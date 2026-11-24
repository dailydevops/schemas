namespace NetEvolve.Schemas.XPath.Tests.Integration.Conformance;

using NetEvolve.Schemas.Conformance.Baseline;
using NetEvolve.Schemas.Conformance.Reporting;
using NetEvolve.Schemas.Conformance.Results;
using NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Engine;
using NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Runner;

/// <summary>One test per QT3 test set; it fails on any regression or unexpected pass against the set's baseline.</summary>
[Explicit]
[Category(ConformanceCategory.Name)]
[ConformanceLeg]
public class Qt3ConformanceTests
{
    /// <summary>The test sets are the baseline files, so discovery needs no download; a missing file fails the baseline test.</summary>
    public static IEnumerable<Func<string>> TestSets()
    {
        foreach (var name in Qt3Suite.LoadBaselines().Keys.OrderBy(name => name, StringComparer.Ordinal))
        {
            yield return () => name;
        }
    }

    [Test]
    [MethodDataSource(nameof(TestSets))]
    public async Task TestSet_MatchesBaseline(string testSet, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var selection = await Qt3Suite.GetSelectionAsync(cancellationToken);
        var engine = await Qt3Engine.GetSharedAsync(cancellationToken);
        var results = Qt3Runner.Run(selection.SelectedIn(testSet), engine);
        var baselines = new Dictionary<string, KnownFailureBaseline>(StringComparer.Ordinal)
        {
            [testSet] = Qt3Suite.LoadBaselines()[testSet],
        };

        var report = ConformanceReport.Compare(results, baselines);
        ConformanceRateReporter.Collect(
            "NetEvolve.Schemas.XPath",
            TargetFrameworkName.Of(typeof(Qt3ConformanceTests).Assembly),
            report
        );

        await Assert
            .That(report.IsSuccess)
            .IsTrue()
            .Because(report.IsSuccess ? string.Empty : Qt3FailureMessage.Format(report.TestSets[0]));
    }
}
