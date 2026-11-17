namespace NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Runner;

using NetEvolve.Schemas.Conformance.Baseline;
using NetEvolve.Schemas.Conformance.Results;

public class Qt3BaselineTests
{
    [Test]
    public async Task Baseline_EverySelectedCase_InExactlyOneFile()
    {
        var owners = new Dictionary<string, int>(StringComparer.Ordinal);
        var foreign = new List<string>();
        foreach (var file in Directory.GetFiles(Qt3Suite.BaselineDirectory, "*.txt"))
        {
            var testSet = Path.GetFileNameWithoutExtension(file);
            foreach (var id in BaselineFiles.Load(Qt3Suite.BaselineDirectory, testSet).Ids)
            {
                owners[id] = owners.GetValueOrDefault(id) + 1;
                if (!id.StartsWith(testSet + "/", StringComparison.Ordinal))
                {
                    foreign.Add($"{testSet}.txt: {id}");
                }
            }
        }

        var missingOrRepeated = new List<string>();
        foreach (var testCase in Qt3Suite.Selection.Selected)
        {
            if (owners.GetValueOrDefault(testCase.Id) != 1)
            {
                missingOrRepeated.Add(testCase.Id);
            }
        }

        await Assert.That(missingOrRepeated).IsEmpty();
        await Assert.That(foreign).IsEmpty();
        await Assert.That(owners.Count).IsEqualTo(Qt3Suite.Selection.Selected.Count);
    }

    [Test]
    public async Task Baseline_MatchesUnboundEngineRun()
    {
        var results = Qt3Runner.Run(Qt3Suite.Selection.Selected, new UnboundEngine());

        var report = ConformanceReport.Compare(results, Qt3Suite.Baselines);

        await Assert.That(report.IsSuccess).IsTrue();
        await Assert.That(report.Counts.Passed).IsEqualTo(0);
        await Assert.That(report.Counts.Failed + report.Counts.Skipped).IsEqualTo(Qt3Suite.Selection.Selected.Count);
    }
}
