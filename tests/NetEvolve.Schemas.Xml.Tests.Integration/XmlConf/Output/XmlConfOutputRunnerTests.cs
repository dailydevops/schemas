namespace NetEvolve.Schemas.Xml.Tests.Integration.XmlConf.Output;

using NetEvolve.Schemas.Conformance.Baseline;
using NetEvolve.Schemas.Conformance.Results;

public class XmlConfOutputRunnerTests
{
    private static async Task<IReadOnlyList<CaseResult>> RunAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var fixture = new TemporaryDirectory();
        var root = OutputFixtureSuite.Create(fixture.Path);
        var selection = XmlConfFilter.Select(XmlConfCatalogReader.Read(root));
        var outputs = XmlConfOutputReference.Read(root);

        return await XmlConfOutputRunner.RunAsync(selection, outputs, root, cancellationToken);
    }

    private static CaseResult Of(IReadOnlyList<CaseResult> results, string id) =>
        results.Single(r => string.Equals(r.CaseId, id, StringComparison.Ordinal));

    [Test]
    public async Task Run_EqualOutput_Passes(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var results = await RunAsync(cancellationToken);

        await Assert.That(Of(results, "a/equal").Outcome).IsEqualTo(CaseOutcome.Pass);
    }

    [Test]
    public async Task Run_OneByteDifference_FailsWithTheOffsetInTheMessage(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var results = await RunAsync(cancellationToken);
        var failed = Of(results, "a/differs");

        await Assert.That(failed.Outcome).IsEqualTo(CaseOutcome.Fail);
        await Assert.That(failed.Reason).IsEqualTo("The output differs at offset 14: expected 0x33 but was 0x32.");
    }

    [Test]
    public async Task Run_DocumentTypeDeclaration_IsNotComparedAndStaysInTheBaselineWithTheReason(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var results = await RunAsync(cancellationToken);
        var result = Of(results, "a/doctype");

        await Assert.That(result.Outcome).IsEqualTo(CaseOutcome.Fail);
        await Assert.That(result.Reason).IsEqualTo("needs DTD processing");

        var baselines = new Dictionary<string, KnownFailureBaseline>(StringComparer.Ordinal)
        {
            ["a"] = KnownFailureBaseline.Parse("a/differs\na/doctype\n"),
        };
        await Assert.That(ConformanceReport.Compare(results, baselines).IsSuccess).IsTrue();
    }

    [Test]
    public async Task Run_CasesWithoutAnOutputFile_AreNotPartOfTheRun(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var results = await RunAsync(cancellationToken);

        await Assert.That(results.Select(r => r.CaseId)).IsEquivalentTo(["a/equal", "a/differs", "a/doctype"]);
    }

    [Test]
    public async Task Reference_ReadsTheOutputPathOfEveryCaseThatHasOne()
    {
        using var fixture = new TemporaryDirectory();
        var outputs = XmlConfOutputReference.Read(OutputFixtureSuite.Create(fixture.Path));

        await Assert.That(outputs.Count).IsEqualTo(3);
        await Assert.That(outputs["a/equal"]).IsEqualTo("a/out/equal.xml");
        await Assert.That(outputs["a/differs"]).IsEqualTo("a/out/differs.xml");
        await Assert.That(outputs["a/doctype"]).IsEqualTo("a/out/doctype.xml");
    }
}
