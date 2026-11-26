namespace NetEvolve.Schemas.Schematron.Tests.Integration.Goldens.OrderX;

using System.Text;
using NetEvolve.Schemas.Conformance.Baseline;
using NetEvolve.Schemas.Conformance.Reporting;
using NetEvolve.Schemas.Conformance.Results;

/// <summary>
/// Acceptance of the Schematron subset on the Order-X 1.0 rules (BASIC and COMFORT) with the 24 official examples. The archive is
/// fetched by its pinned checksum and never committed.
/// </summary>
/// <remarks>
/// Applicable cases: every XML example of a profile, so the pass rate is 100 % of all examples and no case is excluded. The examples
/// are positive only, so the expected result of each is "valid"; there are no negative cases and no EXTENDED examples. A build
/// blocker of a rule set is a known failure of every example of its profile, linked to its issue in
/// <see cref="OrderXRun.BuildBlockers"/>.
/// </remarks>
[Explicit("Fetches the pinned Order-X archive; run through the conformance category.")]
[Category(ConformanceCategory.Name)]
public class OrderXGoldenTests
{
    [Test]
    [Arguments("basic")]
    [Arguments("comfort")]
    public async Task Profile_EveryExample_IsValidOrAKnownBuildBlockedFailure(
        string profile,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(profile);
        var results = (await OrderXRun.GetResultsAsync(cancellationToken))[profile];
        var known = results
            .Where(r =>
                r.Outcome == CaseOutcome.Fail
                && r.Reason!.StartsWith(OrderXRun.BuildBlockedPrefix, StringComparison.Ordinal)
            )
            .Select(r => r.CaseId);

        var report = ConformanceReport.Compare(
            results,
            new Dictionary<string, KnownFailureBaseline>(StringComparer.Ordinal)
            {
                [profile] = KnownFailureBaseline.Parse(KnownFailureBaseline.Format(known)),
            }
        );
        ConformanceRateReporter.Collect(
            OrderXRun.RatePackage,
            TargetFrameworkName.Of(typeof(OrderXGoldenTests).Assembly),
            report
        );

        await Assert.That(results).IsNotEmpty();
        await Assert.That(report.TestSets).HasSingleItem();
        await Assert.That(report.IsSuccess).IsTrue().Because(Format(report.TestSets[0], results));
    }

    [Test]
    [Arguments("basic")]
    [Arguments("comfort")]
    public async Task BuildBlockers_EveryCode_IsLinkedToAnIssue(string profile, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(profile);
        var root = await OrderXRun.GetRootAsync(cancellationToken);

        var codes = OrderXRun.BuildErrorCodes(OrderXRun.SchemaPath(root, profile));

        foreach (var code in codes)
        {
            await Assert.That(OrderXRun.BuildBlockers.Keys).Contains(code);
        }
    }

    [Test]
    [Arguments("basic", 12)]
    [Arguments("comfort", 12)]
    public async Task Corpus_EveryExampleOfTheProfile_IsOneCase(
        string profile,
        int expected,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(profile);
        var root = await OrderXRun.GetRootAsync(cancellationToken);
        var instances = OrderXRun.Instances(root, profile);

        var results = (await OrderXRun.GetResultsAsync(cancellationToken))[profile];

        await Assert.That(instances.Count).IsEqualTo(expected);
        await Assert.That(results.Count).IsEqualTo(instances.Count);
        await Assert
            .That(results.Select(r => r.CaseId).Distinct(StringComparer.Ordinal).Count())
            .IsEqualTo(results.Count);
    }

    [Test]
    [Arguments("basic")]
    [Arguments("comfort")]
    public async Task Schema_IsPartOfTheArchive(string profile, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(profile);
        var root = await OrderXRun.GetRootAsync(cancellationToken);

        await Assert.That(File.Exists(OrderXRun.SchemaPath(root, profile))).IsTrue();
    }

    [Test]
    public async Task Manifest_PinsACommitArchiveByChecksum()
    {
        var manifest = OrderXRun.LoadManifest();

        await Assert.That(manifest.Key).IsEqualTo(OrderXRun.SuiteKey);
        await Assert.That(manifest.SourceUrl.Scheme).IsEqualTo(Uri.UriSchemeHttps);
        await Assert.That(manifest.SourceUrl.AbsolutePath).Contains("/archive/");
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
