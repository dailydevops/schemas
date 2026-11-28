namespace NetEvolve.Schemas.Conformance.Tests.Unit.Results;

using NetEvolve.Schemas.Conformance.Baseline;
using NetEvolve.Schemas.Conformance.Results;

public class ConformanceReportTests
{
    private static CaseResult Case(string set, string id, CaseOutcome outcome, string? reason = null) =>
        new(id, set, outcome, reason);

    private static Dictionary<string, KnownFailureBaseline> Baselines(string set, params string[] ids) =>
        new(StringComparer.Ordinal) { [set] = KnownFailureBaseline.Parse(string.Join('\n', ids)) };

    [Test]
    public async Task Compare_FailingCaseNotInBaseline_IsRegression()
    {
        var results = new[] { Case("set", "a/1", CaseOutcome.Pass), Case("set", "a/2", CaseOutcome.Fail, "boom") };

        var report = ConformanceReport.Compare(results, Baselines("set"));

        await Assert.That(report.IsSuccess).IsFalse();
        await Assert.That(report.TestSets[0].Regressions.Count).IsEqualTo(1);
        await Assert.That(report.TestSets[0].Regressions[0]).IsEqualTo("a/2");
        await Assert.That(report.TestSets[0].UnexpectedPasses.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Compare_Regression_KeepsItsReasonAndBaselinedFailureDoesNot()
    {
        var results = new[]
        {
            Case("set", "a/1", CaseOutcome.Fail, "known"),
            Case("set", "a/2", CaseOutcome.Fail, "boom"),
            Case("set", "a/3", CaseOutcome.Fail),
        };

        var report = ConformanceReport.Compare(results, Baselines("set", "a/1"));

        await Assert.That(report.TestSets[0].RegressionReasons.Count).IsEqualTo(1);
        await Assert.That(report.TestSets[0].RegressionReasons["a/2"]).IsEqualTo("boom");
    }

    [Test]
    public async Task Compare_PassingCaseInBaseline_IsUnexpectedPass()
    {
        var results = new[] { Case("set", "a/1", CaseOutcome.Pass), Case("set", "a/2", CaseOutcome.Fail) };

        var report = ConformanceReport.Compare(results, Baselines("set", "a/1", "a/2"));

        await Assert.That(report.IsSuccess).IsFalse();
        await Assert.That(report.TestSets[0].UnexpectedPasses.Count).IsEqualTo(1);
        await Assert.That(report.TestSets[0].UnexpectedPasses[0]).IsEqualTo("a/1");
        await Assert.That(report.TestSets[0].Regressions.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Compare_FailuresMatchBaseline_Succeeds()
    {
        var results = new[] { Case("set", "a/1", CaseOutcome.Pass), Case("set", "a/2", CaseOutcome.Fail) };

        var report = ConformanceReport.Compare(results, Baselines("set", "a/2"));

        await Assert.That(report.IsSuccess).IsTrue();
    }

    [Test]
    public async Task Compare_SkippedCases_AreCountedSeparatelyAndLeavePassRateUntouched()
    {
        var results = new[]
        {
            Case("set", "a/1", CaseOutcome.Pass),
            Case("set", "a/2", CaseOutcome.Pass),
            Case("set", "a/3", CaseOutcome.Fail),
            Case("set", "a/4", CaseOutcome.Skipped, "excluded: reason"),
            Case("set", "a/5", CaseOutcome.Skipped, "excluded: reason"),
        };

        var report = ConformanceReport.Compare(results, Baselines("set", "a/3"));

        var counts = report.TestSets[0].Counts;
        await Assert.That(counts.Passed).IsEqualTo(2);
        await Assert.That(counts.Failed).IsEqualTo(1);
        await Assert.That(counts.Skipped).IsEqualTo(2);
        await Assert.That(counts.Total).IsEqualTo(5);
        await Assert.That(counts.Applicable).IsEqualTo(3);
        await Assert.That(counts.PassRate).IsEqualTo(2d / 3d);
        await Assert.That(report.Counts).IsEqualTo(counts);
        await Assert.That(report.IsSuccess).IsTrue();
    }

    [Test]
    public async Task Compare_SkippedCaseInBaseline_IsNeitherRegressionNorUnexpectedPass()
    {
        var results = new[] { Case("set", "a/1", CaseOutcome.Skipped) };

        var report = ConformanceReport.Compare(results, Baselines("set", "a/1"));

        await Assert.That(report.IsSuccess).IsTrue();
    }

    [Test]
    public async Task Compare_SuiteCounts_SumTestSetsAndSetsAreSortedByName()
    {
        var results = new[]
        {
            Case("zeta", "z/1", CaseOutcome.Pass),
            Case("alpha", "a/1", CaseOutcome.Fail),
            Case("alpha", "a/2", CaseOutcome.Skipped),
        };

        var report = ConformanceReport.Compare(results, Baselines("alpha", "a/1"));

        await Assert.That(report.TestSets.Count).IsEqualTo(2);
        await Assert.That(report.TestSets[0].TestSet).IsEqualTo("alpha");
        await Assert.That(report.TestSets[1].TestSet).IsEqualTo("zeta");
        await Assert.That(report.Counts).IsEqualTo(new TestSetCounts(Passed: 1, Failed: 1, Skipped: 1));
    }

    [Test]
    public async Task Compare_NoApplicableCases_HasFullPassRate() =>
        await Assert.That(new TestSetCounts(Passed: 0, Failed: 0, Skipped: 3).PassRate).IsEqualTo(1d);
}
