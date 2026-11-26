namespace NetEvolve.Schemas.Xsd.Tests.Integration.EInvoicing;

using NetEvolve.Schemas.Conformance.Results;
using NetEvolve.Schemas.Xsd.Tests.Integration.Differential;

/// <summary>The runner on a tiny schema set in a temporary directory; no archive is fetched.</summary>
public class EInvoicingRunnerTests
{
    private const string Schema =
        "<xs:schema xmlns:xs='http://www.w3.org/2001/XMLSchema'><xs:element name='a' type='xs:int'/></xs:schema>";

    private static EInvoicingManifest Manifest(
        string schemaFile,
        string expectation,
        params EInvoicingDeviation[] deviations
    ) =>
        new(
            [
                new EInvoicingSet(
                    "set",
                    "suite",
                    [schemaFile],
                    [new EInvoicingDocumentGroup("suite", "docs", expectation)]
                ),
            ],
            [new EInvoicingExclusion("set/docs/skipped.xml", "not applicable")],
            deviations,
            []
        );

    private static TempDirectory Suite()
    {
        var suite = new TempDirectory();
        _ = suite.Write("a.xsd", Schema);
        _ = suite.Write("docs/good.xml", "<a>1</a>");
        _ = suite.Write("docs/bad.XML", "<a>x</a>");
        _ = suite.Write("docs/skipped.xml", "<a>x</a>");
        _ = suite.Write("docs/readme.txt", "<a>x</a>");
        return suite;
    }

    [Test]
    public async Task ListDocuments_MixedCaseExtensions_AreFoundInOrdinalOrderWithForwardSlashes()
    {
        using var suite = Suite();

        var documents = EInvoicingRunner.ListDocuments(
            suite.Path,
            new EInvoicingDocumentGroup("suite", "docs", EInvoicingRunner.Valid)
        );

        await Assert.That(documents).IsEquivalentTo(["docs/bad.XML", "docs/good.xml", "docs/skipped.xml"]);
    }

    [Test]
    public async Task RunAsync_ValidExpectation_FailsOnlyTheRejectedDocumentAndSkipsTheExcludedOne(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var suite = Suite();

        var results = await EInvoicingRunner.RunAsync(
            Manifest("a.xsd", EInvoicingRunner.Valid),
            _ => suite.Path,
            cancellationToken
        );

        await Assert
            .That(results.Select(r => r.CaseId))
            .IsEquivalentTo(["set/schemas", "set/docs/bad.XML", "set/docs/good.xml"]);
        await Assert.That(results.Single(r => r.CaseId == "set/schemas").Outcome).IsEqualTo(CaseOutcome.Pass);
        await Assert.That(results.Single(r => r.CaseId == "set/docs/good.xml").Outcome).IsEqualTo(CaseOutcome.Pass);
        var bad = results.Single(r => r.CaseId == "set/docs/bad.XML");
        await Assert.That(bad.Outcome).IsEqualTo(CaseOutcome.Fail);
        await Assert.That(bad.Reason).Contains("an official example is rejected at");
    }

    [Test]
    public async Task RunAsync_DifferentialExpectation_PassesWhenBothEnginesAgree(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var suite = Suite();

        var results = await EInvoicingRunner.RunAsync(
            Manifest("a.xsd", EInvoicingRunner.Differential),
            _ => suite.Path,
            cancellationToken
        );

        await Assert.That(results.All(r => r.Outcome == CaseOutcome.Pass)).IsTrue();
    }

    [Test]
    public async Task RunAsync_DeviationThatNoLongerDiffers_FailsWithTheReason(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var suite = Suite();

        var results = await EInvoicingRunner.RunAsync(
            Manifest("a.xsd", EInvoicingRunner.Differential, new EInvoicingDeviation("set/docs/good.xml", "because")),
            _ => suite.Path,
            cancellationToken
        );

        var good = results.Single(r => r.CaseId == "set/docs/good.xml");
        await Assert.That(good.Outcome).IsEqualTo(CaseOutcome.Fail);
        await Assert.That(good.Reason).Contains("no longer applies");
    }

    [Test]
    public async Task RunAsync_SchemaSetThatIsRejected_FailsTheBuildAndEveryDocument(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var suite = Suite();
        _ = suite.Write(
            "broken.xsd",
            "<xs:schema xmlns:xs='http://www.w3.org/2001/XMLSchema'><xs:element name='a' type='b'/></xs:schema>"
        );

        var results = await EInvoicingRunner.RunAsync(
            Manifest("broken.xsd", EInvoicingRunner.Valid),
            _ => suite.Path,
            cancellationToken
        );

        await Assert.That(results.All(r => r.Outcome == CaseOutcome.Fail)).IsTrue();
        await Assert.That(results.Single(r => r.CaseId == "set/schemas").Reason).Contains("the schema set is rejected");
    }
}
