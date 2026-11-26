namespace NetEvolve.Schemas.Schematron.Tests.Integration.Goldens.Scapval;

using System.Text;
using NetEvolve.Schemas.Conformance.Baseline;
using NetEvolve.Schemas.Conformance.Reporting;
using NetEvolve.Schemas.Conformance.Results;

/// <summary>
/// Acceptance of the Schematron builder on the NIST SCAPVal rules: the build of all 31 rule sets. The archive is fetched by its pinned
/// checksum and never committed.
/// </summary>
/// <remarks>
/// The unit tests of the archive are decima templates, not instances, and are not run. A rule set that does not build is a known failure of
/// <see cref="ScapvalRun.KnownFailures"/>, linked to its issue.
/// </remarks>
[Explicit("Fetches the pinned SCAPVal archive; run through the conformance category.")]
[Category(ConformanceCategory.Name)]
public class ScapvalGoldenTests
{
    [Test]
    [Arguments(ScapvalRun.BuildSet)]
    public async Task TestSet_EveryCase_PassesOrIsALinkedKnownFailure(
        string testSet,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(testSet);
        var results = (await ScapvalRun.GetResultsAsync(cancellationToken))[testSet];

        var report = ConformanceReport.Compare(
            results,
            new Dictionary<string, KnownFailureBaseline>(StringComparer.Ordinal)
            {
                [testSet] = KnownFailureBaseline.Parse(
                    KnownFailureBaseline.Format(ScapvalRun.KnownFailures[testSet].Keys)
                ),
            }
        );
        ConformanceRateReporter.Collect(
            ScapvalRun.RatePackage,
            TargetFrameworkName.Of(typeof(ScapvalGoldenTests).Assembly),
            report
        );

        await Assert.That(results).IsNotEmpty();
        await Assert.That(report.TestSets).HasSingleItem();
        await Assert.That(report.IsSuccess).IsTrue().Because(Format(report.TestSets[0], results));
    }

    [Test]
    public async Task Corpus_HoldsEveryRuleSet(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var root = await ScapvalRun.GetRootAsync(cancellationToken);

        await Assert.That(ScapvalRun.RuleSets(root).Count).IsEqualTo(ScapvalRun.RuleSetCount);
    }

    [Test]
    public async Task KnownFailures_EveryEntry_NamesAnIssue()
    {
        foreach (var (_, entries) in ScapvalRun.KnownFailures)
        {
            foreach (var (_, issue) in entries)
            {
                await Assert.That(issue).IsGreaterThan(0);
            }
        }
    }

    [Test]
    public async Task Manifest_PinsATagArchiveByChecksum()
    {
        var manifest = ScapvalRun.LoadManifest();

        await Assert.That(manifest.Key).IsEqualTo(ScapvalRun.SuiteKey);
        await Assert.That(manifest.SourceUrl.Scheme).IsEqualTo(Uri.UriSchemeHttps);
        await Assert.That(manifest.SourceUrl.AbsolutePath).Contains("/archive/refs/tags/");
        await Assert.That(manifest.Sha256.Length).IsEqualTo(64);
    }

    private static string Format(TestSetReport report, IReadOnlyList<CaseResult> results)
    {
        var builder = new StringBuilder();
        foreach (var id in report.Regressions)
        {
            var reason = results.First(r => string.Equals(r.CaseId, id, StringComparison.Ordinal)).Reason;
            _ = builder.Append("fails: ").Append(id).Append(": ").Append(reason).Append('\n');
        }

        return builder.ToString();
    }
}
