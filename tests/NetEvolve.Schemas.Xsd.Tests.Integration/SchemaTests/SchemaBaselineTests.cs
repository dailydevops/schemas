namespace NetEvolve.Schemas.Xsd.Tests.Integration.SchemaTests;

using System.Collections.Concurrent;
using NetEvolve.Schemas.Conformance.Baseline;
using NetEvolve.Schemas.Conformance.Results;
using NetEvolve.Schemas.Xsd.Tests.Integration.Conformance;
using NetEvolve.Schemas.Xsd.Tests.Integration.Xsts;

/// <summary>The known-failure baselines of the XSTS schema tests: one file per test set that has a failing case.</summary>
public class SchemaBaselineTests
{
    /// <summary>
    /// Regenerates the baseline files from the failures of the pipeline and the manifest of the test set sizes; run it with the explicit filter after a deliberate change.
    /// Every case that is not a schema test of XSD 1.0 is not observed and stays a failure, so the files keep them.
    /// </summary>
    [Test]
    [Explicit]
    public async Task Baseline_Regenerate(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var selection = await XstsSuite.LoadSelectionAsync(cancellationToken);
        var validator = new SchemaXstsValidator(await XstsSuite.LoadRootAsync(cancellationToken));
        var failures = new ConcurrentDictionary<string, ConcurrentBag<string>>(StringComparer.Ordinal);

        await Parallel.ForEachAsync(
            selection.Selected.GroupBy(c => c.TestSet, StringComparer.Ordinal),
            new ParallelOptions { CancellationToken = cancellationToken },
            async (group, token) =>
            {
                var results = await XstsRunner.RunAsync(new XstsSelection([.. group], []), validator, token);
                foreach (var result in results.Where(r => r.Outcome == CaseOutcome.Fail))
                {
                    failures.GetOrAdd(group.Key, static _ => []).Add(result.CaseId);
                }
            }
        );

        foreach (var file in Directory.GetFiles(XstsSuite.BaselineDirectory, "*.txt"))
        {
            if (!failures.ContainsKey(Path.GetFileNameWithoutExtension(file)))
            {
                File.Delete(file);
            }
        }

        foreach (var (testSet, ids) in failures)
        {
            BaselineFiles.Save(XstsSuite.BaselineDirectory, testSet, ids);
        }

        var sizes = selection
            .Selected.GroupBy(c => c.TestSet, StringComparer.Ordinal)
            .OrderBy(g => g.Key, StringComparer.Ordinal)
            .Select(g => g.Key + "\t" + g.Count().ToString(System.Globalization.CultureInfo.InvariantCulture) + "\n");
        await File.WriteAllTextAsync(TestSetSizes.ManifestPath, string.Concat(sizes), cancellationToken);

        await Assert.That(failures).IsNotEmpty();
    }
}
