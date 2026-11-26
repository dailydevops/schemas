namespace NetEvolve.Schemas.Schematron.Tests.Integration.Goldens.Xliff;

using System.Text;
using NetEvolve.Schemas.Conformance.Baseline;
using NetEvolve.Schemas.Conformance.Reporting;
using NetEvolve.Schemas.Conformance.Results;

/// <summary>
/// Acceptance of the XSD and Schematron steps on the OASIS XLIFF 2.1 advanced validation rules with the TC test suite. The archive is
/// fetched by its pinned checksum and never committed.
/// </summary>
/// <remarks>
/// Applicable cases: every <c>.xlf</c> file of the <c>valid</c> and <c>invalid</c> folders of <c>test-suite/core</c> and
/// <c>test-suite/modules</c>. The <c>in-out</c> folder and the non-<c>.xlf</c> files are not cases. The expected verdict is the folder; the
/// observed verdict is the XSD step and the Schematron step together. Every known failure is linked to its issue in
/// <see cref="XliffRun.KnownFailures"/>.
/// </remarks>
[Explicit("Fetches the pinned XLIFF archive; run through the conformance category.")]
[Category(ConformanceCategory.Name)]
public class XliffGoldenTests
{
    [Test]
    [Arguments("core")]
    [Arguments("modules")]
    public async Task Set_EveryInstance_HasTheVerdictOfItsFolder_OrIsALinkedKnownFailure(
        string set,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(set);
        var results = (await XliffRun.GetResultsAsync(cancellationToken))[set];
        var known = XliffRun.KnownFailures.Keys.Where(id => id.Contains("/" + set + "/", StringComparison.Ordinal));

        var report = ConformanceReport.Compare(
            results,
            new Dictionary<string, KnownFailureBaseline>(StringComparer.Ordinal)
            {
                [set] = KnownFailureBaseline.Parse(
                    KnownFailureBaseline.Format(known.OrderBy(i => i, StringComparer.Ordinal))
                ),
            }
        );
        ConformanceRateReporter.Collect(
            XliffRun.RatePackage,
            TargetFrameworkName.Of(typeof(XliffGoldenTests).Assembly),
            report
        );

        await Assert.That(results).IsNotEmpty();
        await Assert.That(report.TestSets).HasSingleItem();
        await Assert.That(report.IsSuccess).IsTrue().Because(Format(report.TestSets[0], results));
    }

    [Test]
    [Arguments("core", "valid", 25)]
    [Arguments("core", "invalid", 119)]
    [Arguments("modules", "valid", 34)]
    [Arguments("modules", "invalid", 43)]
    public async Task Corpus_EveryXlfFileOfAFolder_IsOneCase(
        string set,
        string folder,
        int expected,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(set);
        ArgumentNullException.ThrowIfNull(folder);
        var root = await XliffRun.GetRootAsync(cancellationToken);

        var instances = XliffRun.Instances(root, set, folder);
        var results = (await XliffRun.GetResultsAsync(cancellationToken))[set];

        await Assert.That(instances.Count).IsEqualTo(expected);
        await Assert
            .That(results.Count(r => r.CaseId.Contains($"/{set}/{folder}/", StringComparison.Ordinal)))
            .IsEqualTo(expected);
    }

    [Test]
    public async Task KnownFailures_EveryEntry_IsLinkedToAnIssueAndNamesAnInstance(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var root = await XliffRun.GetRootAsync(cancellationToken);

        foreach (var (id, issue) in XliffRun.KnownFailures)
        {
            await Assert.That(issue).IsGreaterThan(0);
            await Assert.That(File.Exists(Path.Combine(root, id))).IsTrue().Because(id);
        }
    }

    [Test]
    public async Task Manifest_PinsACommitArchiveByChecksum()
    {
        var manifest = XliffRun.LoadManifest();

        await Assert.That(manifest.Key).IsEqualTo(XliffRun.SuiteKey);
        await Assert.That(manifest.SourceUrl.Scheme).IsEqualTo(Uri.UriSchemeHttps);
        await Assert.That(manifest.SourceUrl.AbsolutePath).Contains("/archive/");
        await Assert.That(manifest.Sha256.Length).IsEqualTo(64);
    }

    [Test]
    public async Task Schemas_EveryRuleSetAndSchemaFile_IsPartOfTheArchive(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var root = await XliffRun.GetRootAsync(cancellationToken);

        foreach (var file in XliffRun.RuleSets.Concat(XliffRun.SchemaFiles))
        {
            await Assert.That(File.Exists(Path.Combine(root, "schemas", file))).IsTrue().Because(file);
        }
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
