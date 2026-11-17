namespace NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Runner;

using NetEvolve.Schemas.Conformance.Baseline;
using NetEvolve.Schemas.Conformance.Results;

/// <summary>One test per QT3 test set; it fails on any regression or unexpected pass against the set's baseline.</summary>
public class Qt3ConformanceTests
{
    // The XPath engine is bound in XP-14; until then every active case fails and the baseline lists it.
    private static readonly IQt3Engine Engine = new UnboundEngine();

    public static IEnumerable<Func<string>> TestSets()
    {
        foreach (var name in Qt3Suite.Selection.SelectedTestSets)
        {
            yield return () => name;
        }
    }

    [Test]
    [MethodDataSource(nameof(TestSets))]
    public async Task TestSet_MatchesBaseline(string testSet)
    {
        var results = Qt3Runner.Run(Qt3Suite.Selection.SelectedIn(testSet), Engine);
        var baselines = new Dictionary<string, KnownFailureBaseline>(StringComparer.Ordinal)
        {
            [testSet] = Qt3Suite.Baselines.GetValueOrDefault(testSet) ?? KnownFailureBaseline.Empty,
        };

        var report = ConformanceReport.Compare(results, baselines);

        await Assert
            .That(report.IsSuccess)
            .IsTrue()
            .Because(report.IsSuccess ? string.Empty : Qt3FailureMessage.Format(report.TestSets[0]));
    }
}
