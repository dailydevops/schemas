namespace NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Runner;

using NetEvolve.Schemas.Conformance.Results;

public class Qt3BaselineTests
{
    /// <summary>Regenerates the baseline files from the current selection; run it with the explicit filter after a deliberate change.</summary>
    [Test]
    [Explicit]
    public async Task Baseline_Regenerate(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var selection = await Qt3Suite.GetSelectionAsync(cancellationToken);

        Qt3Suite.RegenerateBaselines(selection);

        await Assert.That(Qt3Suite.LoadBaselines().Count).IsEqualTo(selection.SelectedTestSets.Count);
    }

    [Test]
    public async Task Baseline_EverySelectedCase_InExactlyOneFile(CancellationToken cancellationToken)
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

        var missingOrRepeated = new List<string>();
        foreach (var testCase in selection.Selected)
        {
            if (owners.GetValueOrDefault(testCase.Id) != 1)
            {
                missingOrRepeated.Add(testCase.Id);
            }
        }

        await Assert.That(missingOrRepeated).IsEmpty();
        await Assert.That(foreign).IsEmpty();
        await Assert.That(owners.Count).IsEqualTo(selection.Selected.Count);
    }

    [Test]
    public async Task Baseline_MatchesUnboundEngineRun(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var selection = await Qt3Suite.GetSelectionAsync(cancellationToken);
        var results = Qt3Runner.Run(selection.Selected, new UnboundEngine());

        var report = ConformanceReport.Compare(results, Qt3Suite.LoadBaselines());

        await Assert.That(report.IsSuccess).IsTrue();
        await Assert.That(report.Counts.Passed).IsEqualTo(0);
        await Assert.That(report.Counts.Failed + report.Counts.Skipped).IsEqualTo(selection.Selected.Count);
    }
}
