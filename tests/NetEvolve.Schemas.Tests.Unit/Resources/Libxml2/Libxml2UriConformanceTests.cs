namespace NetEvolve.Schemas.Tests.Unit.Resources.Libxml2;

using NetEvolve.Schemas.Conformance.Baseline;
using NetEvolve.Schemas.Conformance.Reporting;
using NetEvolve.Schemas.Conformance.Results;

/// <summary>The pinned libxml2 URI base composition file run on the reference resolver; run on demand with the conformance category.</summary>
public class Libxml2UriConformanceTests
{
    private const int ReferenceCount = 21;

    [Test]
    [Explicit]
    [Category(ConformanceCategory.Name)]
    public async Task Baseline_MatchesTheRun(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var results = await RunAsync(cancellationToken);
        var baselines = new Dictionary<string, KnownFailureBaseline>(StringComparer.Ordinal)
        {
            [Libxml2UriRunner.TestSet] = BaselineFiles.Load(
                Libxml2UriSuite.BaselineDirectory,
                Libxml2UriRunner.TestSet
            ),
        };

        var report = ConformanceReport.Compare(results, baselines);

        await Assert.That(results.Count).IsEqualTo(ReferenceCount);
        await Assert.That(report.IsSuccess).IsTrue().Because(Describe(report));
        await Assert.That(report.Counts.Passed).IsGreaterThan(0);
    }

    [Test]
    [Explicit]
    [Category(ConformanceCategory.Name)]
    public async Task Baseline_Regenerate(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var results = await RunAsync(cancellationToken);
        var failures = results.Where(r => r.Outcome == CaseOutcome.Fail).Select(r => r.CaseId).ToList();

        _ = Directory.CreateDirectory(Libxml2UriSuite.BaselineDirectory);
        if (failures.Count == 0)
        {
            File.Delete(BaselineFiles.PathFor(Libxml2UriSuite.BaselineDirectory, Libxml2UriRunner.TestSet));
        }
        else
        {
            BaselineFiles.Save(Libxml2UriSuite.BaselineDirectory, Libxml2UriRunner.TestSet, failures);
        }

        await Assert.That(results).IsNotEmpty();
    }

    private static async Task<List<CaseResult>> RunAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var root = await Libxml2UriSuite.FetchAsync(cancellationToken);
        return Libxml2UriRunner.Run(
            await File.ReadAllTextAsync(Path.Combine(root, "test", "URI", "uri.data"), cancellationToken),
            await File.ReadAllTextAsync(Path.Combine(root, "result", "URI", "uri.data"), cancellationToken)
        );
    }

    private static string Describe(ConformanceReport report) =>
        string.Join(
            Environment.NewLine,
            report.TestSets.Select(s =>
                $"{s.TestSet}: passed {s.Counts.Passed}, failed {s.Counts.Failed}; regressions [{string.Join(", ", s.Regressions)}]; unexpected passes [{string.Join(", ", s.UnexpectedPasses)}]"
            )
        );
}
