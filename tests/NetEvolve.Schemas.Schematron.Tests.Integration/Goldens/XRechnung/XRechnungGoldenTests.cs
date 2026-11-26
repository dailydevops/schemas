namespace NetEvolve.Schemas.Schematron.Tests.Integration.Goldens.XRechnung;

using System.Text;
using NetEvolve.Schemas.Conformance.Baseline;
using NetEvolve.Schemas.Conformance.Reporting;
using NetEvolve.Schemas.Conformance.Results;

/// <summary>
/// Acceptance of the Schematron subset on the KoSIT XRechnung rules with the KoSIT XRechnung test suite for UBL and CII. Both
/// archives are fetched by their pinned checksums and never committed.
/// </summary>
/// <remarks>
/// Applicable cases: every instance of the test suite whose name ends in <c>_ubl.xml</c> or <c>_uncefact.xml</c>, so the pass rate is
/// 100 % of all instances and no case is excluded. The suite holds only instances that are valid for XRechnung; the expected result of
/// each is "valid". The rules do not build yet, so every instance is a known failure linked to the issue of each blocker
/// (<see cref="XRechnungRun.BuildBlockers"/>); once the blockers are fixed the instances pass, the unexpected-pass check fails, and
/// the known failures are removed, which leaves the 100 % acceptance.
/// </remarks>
[Explicit("Fetches the pinned KoSIT archives; run through the conformance category.")]
[Category(ConformanceCategory.Name)]
public class XRechnungGoldenTests
{
    [Test]
    [Arguments("ubl")]
    [Arguments("cii")]
    public async Task Syntax_EveryInstance_IsValidOrAKnownBuildBlockedFailure(
        string syntax,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(syntax);
        var results = (await XRechnungRun.GetResultsAsync(cancellationToken))[syntax];
        var known = results
            .Where(r =>
                r.Outcome == CaseOutcome.Fail
                && r.Reason!.StartsWith(XRechnungRun.BuildBlockedPrefix, StringComparison.Ordinal)
            )
            .Select(r => r.CaseId);

        var report = ConformanceReport.Compare(
            results,
            new Dictionary<string, KnownFailureBaseline>(StringComparer.Ordinal)
            {
                [syntax] = KnownFailureBaseline.Parse(KnownFailureBaseline.Format(known)),
            }
        );
        ConformanceRateReporter.Collect(
            XRechnungRun.RatePackage,
            TargetFrameworkName.Of(typeof(XRechnungGoldenTests).Assembly),
            report
        );

        await Assert.That(results).IsNotEmpty();
        await Assert.That(report.TestSets).HasSingleItem();
        await Assert.That(report.IsSuccess).IsTrue().Because(Format(report.TestSets[0], results));
    }

    [Test]
    [Arguments("ubl")]
    [Arguments("cii")]
    public async Task BuildBlockers_EveryCode_IsLinkedToAnIssue(string syntax, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(syntax);
        var (rules, _) = await XRechnungRun.GetRootsAsync(cancellationToken);

        var codes = XRechnungRun.BuildErrorCodes(XRechnungRun.SchemaPath(rules, syntax));

        foreach (var code in codes)
        {
            await Assert.That(XRechnungRun.BuildBlockers.Keys).Contains(code);
        }
    }

    [Test]
    [Arguments("ubl")]
    [Arguments("cii")]
    public async Task Corpus_EveryInstanceOfTheSyntax_IsOneCase(string syntax, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(syntax);
        var (_, suite) = await XRechnungRun.GetRootsAsync(cancellationToken);
        var instances = XRechnungRun.Instances(suite, syntax);

        var results = (await XRechnungRun.GetResultsAsync(cancellationToken))[syntax];

        await Assert.That(instances).IsNotEmpty();
        await Assert.That(results.Count).IsEqualTo(instances.Count);
        await Assert
            .That(results.Select(r => r.CaseId).Distinct(StringComparer.Ordinal).Count())
            .IsEqualTo(results.Count);
    }

    [Test]
    [Arguments("ubl")]
    [Arguments("cii")]
    public async Task Schema_IsPartOfTheRulesArchive(string syntax, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(syntax);
        var (rules, _) = await XRechnungRun.GetRootsAsync(cancellationToken);

        await Assert.That(File.Exists(XRechnungRun.SchemaPath(rules, syntax))).IsTrue();
        await Assert.That(File.Exists(Path.Combine(rules, "schematron", "common.sch"))).IsTrue();
    }

    [Test]
    [Arguments(XRechnungRun.SuiteKey)]
    [Arguments(XRechnungRun.RulesKey)]
    public async Task Manifest_PinsAReleaseArchiveByChecksum(string key)
    {
        var manifest = XRechnungRun.LoadManifest(key);

        await Assert.That(manifest.Key).IsEqualTo(key);
        await Assert.That(manifest.SourceUrl.Scheme).IsEqualTo(Uri.UriSchemeHttps);
        await Assert.That(manifest.SourceUrl.AbsolutePath).Contains("/releases/download/");
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
