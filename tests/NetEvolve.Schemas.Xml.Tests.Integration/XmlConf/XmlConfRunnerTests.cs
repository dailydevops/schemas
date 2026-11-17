namespace NetEvolve.Schemas.Xml.Tests.Integration.XmlConf;

using NetEvolve.Schemas.Conformance.Baseline;
using NetEvolve.Schemas.Conformance.Results;

public class XmlConfRunnerTests
{
    private static XmlConfSelection Selection()
    {
        using var fixture = new FixtureDirectory();
        return XmlConfFilter.Select(XmlConfCatalogReader.Read(fixture.Path));
    }

    private static async Task<IReadOnlyList<CaseResult>> RunAsync(
        IXmlConfReader reader,
        CancellationToken cancellationToken
    ) => await XmlConfRunner.RunAsync(Selection(), reader, cancellationToken);

    private static CaseResult Of(IReadOnlyList<CaseResult> results, string id) =>
        results.Single(r => string.Equals(r.CaseId, id, StringComparison.Ordinal));

    [Test]
    public async Task Run_NotWellFormedCase_PassesOnlyWhenTheReaderReportsAFatalError(
        CancellationToken cancellationToken
    )
    {
        var rejecting = await RunAsync(new FakeXmlConfReader(_ => XmlConfObservation.FatalError), cancellationToken);
        var accepting = await RunAsync(new FakeXmlConfReader(_ => XmlConfObservation.Accepted), cancellationToken);

        await Assert.That(Of(rejecting, "a/notwf-1").Outcome).IsEqualTo(CaseOutcome.Pass);
        var failed = Of(accepting, "a/notwf-1");
        await Assert.That(failed.Outcome).IsEqualTo(CaseOutcome.Fail);
        await Assert.That(failed.TestSet).IsEqualTo("a");
        await Assert.That(failed.Reason).IsEqualTo("Expected a fatal error but the reader reported none.");
    }

    [Test]
    public async Task Run_ValidCase_PassesOnlyWhenTheReaderReportsNoFatalError(CancellationToken cancellationToken)
    {
        var accepting = await RunAsync(new FakeXmlConfReader(_ => XmlConfObservation.Accepted), cancellationToken);
        var rejecting = await RunAsync(new FakeXmlConfReader(_ => XmlConfObservation.FatalError), cancellationToken);

        await Assert.That(Of(accepting, "a/valid-1").Outcome).IsEqualTo(CaseOutcome.Pass);
        var failed = Of(rejecting, "a/valid-1");
        await Assert.That(failed.Outcome).IsEqualTo(CaseOutcome.Fail);
        await Assert.That(failed.Reason).IsEqualTo("Expected no fatal error but the reader reported one.");
    }

    [Test]
    public async Task Run_InvalidCase_NeedsOnlyWellFormedness(CancellationToken cancellationToken)
    {
        var accepting = await RunAsync(new FakeXmlConfReader(_ => XmlConfObservation.Accepted), cancellationToken);
        var rejecting = await RunAsync(new FakeXmlConfReader(_ => XmlConfObservation.FatalError), cancellationToken);

        await Assert.That(Of(accepting, "a/invalid-1").Outcome).IsEqualTo(CaseOutcome.Pass);
        await Assert.That(Of(rejecting, "a/invalid-1").Outcome).IsEqualTo(CaseOutcome.Fail);
    }

    [Test]
    public async Task Run_ErrorCase_AcceptsAnyObservationOfARealReader(CancellationToken cancellationToken)
    {
        var accepting = await RunAsync(new FakeXmlConfReader(_ => XmlConfObservation.Accepted), cancellationToken);
        var rejecting = await RunAsync(new FakeXmlConfReader(_ => XmlConfObservation.FatalError), cancellationToken);

        await Assert.That(Of(accepting, "a/error-1").Outcome).IsEqualTo(CaseOutcome.Pass);
        await Assert.That(Of(rejecting, "a/error-1").Outcome).IsEqualTo(CaseOutcome.Pass);
    }

    [Test]
    public async Task Run_NoReaderBound_FailsEverySelectedCase(CancellationToken cancellationToken)
    {
        var results = await RunAsync(UnboundXmlConfReader.Instance, cancellationToken);

        await Assert.That(results.Count(r => r.Outcome == CaseOutcome.Fail)).IsEqualTo(8);
        await Assert.That(results.Count(r => r.Outcome == CaseOutcome.Pass)).IsEqualTo(0);
        await Assert.That(Of(results, "a/error-1").Outcome).IsEqualTo(CaseOutcome.Fail);
    }

    [Test]
    public async Task Run_ExcludedCases_AreSkippedWithTheirReasonAndNeverRead(CancellationToken cancellationToken)
    {
        var reader = new FakeXmlConfReader(_ => XmlConfObservation.Accepted);

        var results = await RunAsync(reader, cancellationToken);

        var skipped = results.Where(r => r.Outcome == CaseOutcome.Skipped).ToList();
        await Assert.That(skipped.Count).IsEqualTo(4);
        await Assert.That(skipped.All(r => !string.IsNullOrWhiteSpace(r.Reason))).IsTrue();
        await Assert.That(reader.Calls).DoesNotContain("b/xml11-version");
        await Assert.That(reader.Calls.Count).IsEqualTo(8);
    }

    [Test]
    public async Task Compare_FailureNotInBaseline_IsRegression(CancellationToken cancellationToken)
    {
        var results = await RunAsync(UnboundXmlConfReader.Instance, cancellationToken);

        var report = ConformanceReport.Compare(
            results,
            new Dictionary<string, KnownFailureBaseline>(StringComparer.Ordinal)
            {
                ["a"] = KnownFailureBaseline.Parse("a/default-1\na/error-1\na/invalid-1\na/notwf-1\n"),
            }
        );

        await Assert.That(report.IsSuccess).IsFalse();
        await Assert
            .That(report.TestSets.SelectMany(t => t.Regressions))
            .IsEquivalentTo(["a/valid-1", "b/both-versions", "b/edition-4-5", "b/no-namespaces"]);
    }

    [Test]
    public async Task Compare_PassOfBaselinedCase_IsUnexpectedPass(CancellationToken cancellationToken)
    {
        var results = await RunAsync(new FakeXmlConfReader(_ => XmlConfObservation.Accepted), cancellationToken);

        var report = ConformanceReport.Compare(
            results,
            new Dictionary<string, KnownFailureBaseline>(StringComparer.Ordinal)
            {
                ["a"] = KnownFailureBaseline.Parse("a/valid-1\n"),
            }
        );

        await Assert.That(report.IsSuccess).IsFalse();
        await Assert.That(report.TestSets.SelectMany(t => t.UnexpectedPasses)).IsEquivalentTo(["a/valid-1"]);
    }
}
