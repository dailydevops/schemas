namespace NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Runner;

using NetEvolve.Schemas.Conformance.Reporting;
using NetEvolve.Schemas.Conformance.Results;
using NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Engine;

[ConformanceLeg]
public class Qt3BaselineTests
{
    /// <summary>Regenerates the baseline files from the current selection; run it with the explicit filter after a deliberate change.</summary>
    [Test]
    [Explicit]
    public async Task Baseline_Regenerate(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var selection = await Qt3Suite.GetSelectionAsync(cancellationToken);

        Qt3Suite.RegenerateBaselines(selection, await Qt3Engine.GetSharedAsync(cancellationToken));

        await Assert.That(Qt3Suite.LoadBaselines().Count).IsEqualTo(selection.SelectedTestSets.Count);
    }

    [Test]
    public async Task Baseline_ListsOnlySelectedCases_EachOnceInItsOwnFile(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var selection = await Qt3Suite.GetSelectionAsync(cancellationToken);
        var owners = new Dictionary<string, int>(StringComparer.Ordinal);
        var foreign = new List<string>();
        foreach (var (testSet, baseline) in Qt3Suite.LoadBaselines())
        {
            foreach (var id in baseline.Ids)
            {
                owners[id] = owners.GetValueOrDefault(id) + 1;
                if (!id.StartsWith(testSet + "/", StringComparison.Ordinal))
                {
                    foreign.Add($"{testSet}.txt: {id}");
                }
            }
        }

        var selected = new HashSet<string>(StringComparer.Ordinal);
        foreach (var testCase in selection.Selected)
        {
            _ = selected.Add(testCase.Id);
        }

        var unselectedOrRepeated = new List<string>();
        foreach (var (id, count) in owners)
        {
            if (count != 1 || !selected.Contains(id))
            {
                unselectedOrRepeated.Add(id);
            }
        }

        await Assert.That(unselectedOrRepeated).IsEmpty();
        await Assert.That(foreign).IsEmpty();
        await Assert.That(Qt3Suite.LoadBaselines().Count).IsEqualTo(selection.SelectedTestSets.Count);
    }

    [Test]
    public async Task Baseline_MatchesEngineRun(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var selection = await Qt3Suite.GetSelectionAsync(cancellationToken);
        var results = Qt3Runner.Run(selection.Selected, await Qt3Engine.GetSharedAsync(cancellationToken));
        var baselines = Qt3Suite.LoadBaselines();
        var listed = 0;
        foreach (var baseline in baselines.Values)
        {
            listed += baseline.Count;
        }

        var report = ConformanceReport.Compare(results, baselines);

        await Assert.That(report.IsSuccess).IsTrue();
        await Assert.That(report.Counts.Passed).IsGreaterThan(0);
        await Assert.That(report.Counts.Failed + report.Counts.Skipped).IsEqualTo(listed);
        await Assert
            .That(report.Counts.Passed + report.Counts.Failed + report.Counts.Skipped)
            .IsEqualTo(selection.Selected.Count);
    }
}
