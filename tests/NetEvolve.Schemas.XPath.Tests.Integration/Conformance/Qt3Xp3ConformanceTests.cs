namespace NetEvolve.Schemas.XPath.Tests.Integration.Conformance;

using NetEvolve.Schemas.Conformance.Baseline;
using NetEvolve.Schemas.Conformance.Reporting;
using NetEvolve.Schemas.Conformance.Results;
using NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Engine;
using NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Runner;
using NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Selections;

/// <summary>
/// Runs the cases the XPath 3.0 and 3.1 selections add to the XPath 2.0 selection, one test per test set and tier, against the
/// baselines of <c>tests/conformance/baselines/qt3-xp30</c> and <c>qt3-xp31</c>. The class lives in this namespace because the
/// conformance workflow selects tests by it. Limitation: the engine compiles every query as XPath 2.0 (the later versions are not
/// enabled), so a case that needs 3.x syntax or a 3.x function is a known failure until the package that adds it removes the case
/// from the baseline. The rates of the two selections are reported by XP-73 and not collected here.
/// </summary>
public class Qt3Xp3ConformanceTests
{
    public static IEnumerable<Func<string>> TestSetsXp30() => TestSets(Qt3SpecLevel.XPath30);

    public static IEnumerable<Func<string>> TestSetsXp31() => TestSets(Qt3SpecLevel.XPath31);

    [Test]
    [Explicit]
    [Category(ConformanceCategory.Name)]
    [MethodDataSource(nameof(TestSetsXp30))]
    public async Task Xp30_TestSet_MatchesBaseline(string testSet, CancellationToken cancellationToken) =>
        await AssertTestSetAsync(Qt3SpecLevel.XPath30, testSet, cancellationToken);

    [Test]
    [Explicit]
    [Category(ConformanceCategory.Name)]
    [MethodDataSource(nameof(TestSetsXp31))]
    public async Task Xp31_TestSet_MatchesBaseline(string testSet, CancellationToken cancellationToken) =>
        await AssertTestSetAsync(Qt3SpecLevel.XPath31, testSet, cancellationToken);

    [Test]
    [Explicit]
    [Category(ConformanceCategory.Name)]
    [Arguments(Qt3SpecLevel.XPath30)]
    [Arguments(Qt3SpecLevel.XPath31)]
    public async Task Tier_EveryNewCase_IsPassingOrABaselineEntry(
        Qt3SpecLevel level,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var newCases = await Qt3Xp3Selections.GetNewCasesAsync(level, cancellationToken);
        var engine = await Qt3Engine.GetSharedAsync(cancellationToken);
        var results = Qt3Runner.Run(newCases.Selected, engine);
        var baselines = Qt3Xp3Selections.LoadBaselines(level);
        var listed = 0;
        foreach (var baseline in baselines.Values)
        {
            listed += baseline.Count;
        }

        var report = ConformanceReport.Compare(results, baselines);

        await Assert.That(report.IsSuccess).IsTrue();
        await Assert.That(report.Counts.Failed + report.Counts.Skipped).IsEqualTo(listed);
        await Assert
            .That(report.Counts.Passed + report.Counts.Failed + report.Counts.Skipped)
            .IsEqualTo(newCases.Selected.Count);
    }

    /// <summary>Rewrites the baseline files of one tier; run it with the explicit filter after a deliberate change.</summary>
    [Test]
    [Explicit]
    [Arguments(Qt3SpecLevel.XPath30)]
    [Arguments(Qt3SpecLevel.XPath31)]
    public async Task Baseline_Regenerate(Qt3SpecLevel level, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var newCases = await Qt3Xp3Selections.GetNewCasesAsync(level, cancellationToken);

        Qt3Xp3Selections.RegenerateBaselines(level, newCases, await Qt3Engine.GetSharedAsync(cancellationToken));

        await Assert.That(Qt3Xp3Selections.LoadBaselines(level).Count).IsEqualTo(newCases.SelectedTestSets.Count);
    }

    private static IEnumerable<Func<string>> TestSets(Qt3SpecLevel level)
    {
        foreach (var name in Qt3Xp3Selections.LoadBaselines(level).Keys.OrderBy(name => name, StringComparer.Ordinal))
        {
            yield return () => name;
        }
    }

    private static async Task AssertTestSetAsync(
        Qt3SpecLevel level,
        string testSet,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var newCases = await Qt3Xp3Selections.GetNewCasesAsync(level, cancellationToken);
        var engine = await Qt3Engine.GetSharedAsync(cancellationToken);
        var results = Qt3Runner.Run(newCases.SelectedIn(testSet), engine);
        var baselines = new Dictionary<string, KnownFailureBaseline>(StringComparer.Ordinal)
        {
            [testSet] = Qt3Xp3Selections.LoadBaselines(level)[testSet],
        };

        var report = ConformanceReport.Compare(results, baselines);

        await Assert
            .That(report.IsSuccess)
            .IsTrue()
            .Because(report.IsSuccess ? string.Empty : Qt3FailureMessage.Format(report.TestSets[0]));
    }
}
