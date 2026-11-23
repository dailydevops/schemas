namespace NetEvolve.Schemas.Xsd.Tests.Integration.SchemaTests;

using System.Collections.Concurrent;
using NetEvolve.Schemas.Conformance.Baseline;
using NetEvolve.Schemas.Conformance.Results;
using NetEvolve.Schemas.Xsd.Tests.Integration.Conformance;
using NetEvolve.Schemas.Xsd.Tests.Integration.InstanceTests;
using NetEvolve.Schemas.Xsd.Tests.Integration.Xsts;

/// <summary>The known-failure baselines of the XSTS schema tests: one file per test set that has a failing case.</summary>
public class SchemaBaselineTests
{
    /// <summary>
    /// Regenerates the baseline files from the failures of the pipeline and the manifest of the test set sizes; run it with the explicit filter after a deliberate change.
    /// Every case that is neither a schema test nor an instance test of XSD 1.0 is not observed and stays a failure, so the files keep them.
    /// </summary>
    [Test]
    [Explicit]
    public async Task Baseline_Regenerate(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var selection = await XstsSuite.LoadSelectionAsync(cancellationToken);
        var validator = new InstanceXstsValidator(await XstsSuite.LoadRootAsync(cancellationToken));
        var failures = new ConcurrentDictionary<string, ConcurrentBag<string>>(StringComparer.Ordinal);

        // The cases run in chunks of 100 like the suite run does, so the large test sets use every core.
        var chunks = selection
            .Selected.GroupBy(c => c.TestSet, StringComparer.Ordinal)
            .SelectMany(group => group.OrderBy(c => c.Id, StringComparer.Ordinal).Chunk(100));
        await ParallelPolyfills.ForEachAsync(
            chunks,
            new ParallelOptions { CancellationToken = cancellationToken },
            async (chunk, token) =>
            {
                var results = await XstsRunner.RunAsync(new XstsSelection(chunk, []), validator, token);
                foreach (var result in results.Where(r => r.Outcome == CaseOutcome.Fail))
                {
                    failures.GetOrAdd(result.TestSet, static _ => []).Add(result.CaseId);
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
