namespace NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Runner;

using NetEvolve.Schemas.Conformance.Baseline;
using NetEvolve.Schemas.Conformance.Results;

/// <summary>One test per QT3 test set; it fails on any regression or unexpected pass against the set's baseline.</summary>
public class Qt3ConformanceTests
{
    // The XPath engine is bound in XP-14; until then every active case fails and the baseline lists it.
    private static readonly UnboundEngine Engine = new();

    /// <summary>The test sets are the baseline files, so discovery needs no download; a missing file fails the baseline test.</summary>
    public static IEnumerable<Func<string>> TestSets()
    {
        foreach (var name in Qt3Suite.LoadBaselines().Keys.Order(StringComparer.Ordinal))
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
        var results = Qt3Runner.Run(selection.SelectedIn(testSet), Engine);
        var baselines = new Dictionary<string, KnownFailureBaseline>(StringComparer.Ordinal)
        {
            [testSet] = Qt3Suite.LoadBaselines()[testSet],
        };

        var report = ConformanceReport.Compare(results, baselines);

        await Assert
            .That(report.IsSuccess)
            .IsTrue()
            .Because(report.IsSuccess ? string.Empty : Qt3FailureMessage.Format(report.TestSets[0]));
    }
}
