namespace NetEvolve.Schemas.Schematron.Tests.Integration.Goldens.FrenchCtc;

using System.Text;
using NetEvolve.Schemas.Conformance.Baseline;
using NetEvolve.Schemas.Conformance.Reporting;
using NetEvolve.Schemas.Conformance.Results;
using NetEvolve.Schemas.Schematron.Tests.Integration.Goldens.Stage3Documents;

/// <summary>
/// The acceptance of the French CTC, CDAR and Flux10 rule sets of the pinned archive: every rule set with a sample folder validates its
/// documents with the expected result, and every rule set without documents builds. The suite is fetched by its pinned checksum and
/// never committed.
/// </summary>
[Explicit]
[Category(ConformanceCategory.Name)]
public class FrenchCtcGoldenTests
{
    public static IEnumerable<Func<string>> TestSets() =>
        FrenchCtcRun.LoadManifest().RuleSets.Select(r => (Func<string>)(() => "stage3-" + r.Name));

    [Test]
    [MethodDataSource(nameof(TestSets))]
    public async Task TestSet_AgainstKnownFailures_HasNoRegressionAndNoUnexpectedPass(
        string testSet,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var results = (await FrenchCtcRun.GetResultsAsync(cancellationToken))
            .Where(r => string.Equals(r.TestSet, testSet, StringComparison.Ordinal))
            .ToList();
        var known = FrenchCtcRun
            .LoadManifest()
            .KnownFailures.Select(l => l.Id)
            .Where(id => results.Any(r => string.Equals(r.CaseId, id, StringComparison.Ordinal)));

        var report = ConformanceReport.Compare(
            results,
            new Dictionary<string, KnownFailureBaseline>(StringComparer.Ordinal)
            {
                [testSet] = KnownFailureBaseline.Parse(KnownFailureBaseline.Format(known)),
            }
        );
        ConformanceRateReporter.Collect(
            FrenchCtcRun.RatePackage,
            TargetFrameworkName.Of(typeof(FrenchCtcGoldenTests).Assembly),
            report
        );

        await Assert.That(results).IsNotEmpty();
        await Assert.That(report.TestSets).HasSingleItem();
        await Assert.That(report.IsSuccess).IsTrue().Because(Format(report.TestSets[0], results));
    }

    [Test]
    public async Task KnownFailures_EveryEntry_NamesARunCaseAndHasAnIssueAndAReason(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var ids = (await FrenchCtcRun.GetResultsAsync(cancellationToken)).Select(r => r.CaseId).ToList();

        foreach (var link in FrenchCtcRun.LoadManifest().KnownFailures)
        {
            await Assert.That(ids).Contains(link.Id);
            await Assert.That(link.Issue).IsGreaterThan(0);
            await Assert.That(string.IsNullOrWhiteSpace(link.Reason)).IsFalse();
        }
    }

    [Test]
    public async Task Corpus_EveryRuleSetAndSampleDocumentOutsideStage3_IsACase(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var root = await Stage3Run.GetRootAsync(cancellationToken);
        var manifest = FrenchCtcRun.LoadManifest();

        var schemas = manifest.RuleSets.Concat(Stage3Run.LoadManifest().RuleSets).Select(r => r.Schema).ToList();
        var onDisk = Directory
            .EnumerateFiles(root, "*.sch", SearchOption.AllDirectories)
            .Select(f => Path.GetRelativePath(root, f).Replace(Path.DirectorySeparatorChar, '/'));
        await Assert.That(schemas).IsEquivalentTo(onDisk);

        var listed = manifest.RuleSets.SelectMany(r => r.Documents).Select(d => d.Document).Distinct().ToList();
        var samples = SampleFolders
            .SelectMany(folder =>
                Directory.EnumerateFiles(
                    Path.Combine(root, "Z.example", "TEST", folder),
                    "*.xml",
                    SearchOption.AllDirectories
                )
            )
            .Select(f => Path.GetRelativePath(root, f).Replace(Path.DirectorySeparatorChar, '/'));
        await Assert.That(listed).IsEquivalentTo(samples);
        foreach (var exclusion in manifest.Excluded)
        {
            await Assert.That(string.IsNullOrWhiteSpace(exclusion.Subject)).IsFalse();
            await Assert.That(string.IsNullOrWhiteSpace(exclusion.Reason)).IsFalse();
        }
    }

    [Test]
    public async Task RunBuildOnly_RuleSetWithoutDocuments_PassesWhenTheSchemaBuilds(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var directory = new TempDirectory();
        directory.Write("ok.sch", Schema);
        directory.Write("bad.sch", "<not-a-schema/>");
        var manifest = Stage3Manifest.Parse(
            """{"stage":3,"suite":"s","excluded":[],"knownFailures":[],"ruleSets":[{"name":"ok","schema":"ok.sch","documents":[]},{"name":"bad","schema":"bad.sch","documents":[]},{"name":"docs","schema":"ok.sch","documents":[{"document":"d.xml","valid":true,"rules":[]}]}]}"""
        );

        var results = FrenchCtcRunner.RunBuildOnly(manifest, directory.Root, cancellationToken);

        await Assert.That(results.Select(r => r.CaseId)).IsEquivalentTo(["ok/build", "bad/build"]);
        await Assert.That(results[0].Outcome).IsEqualTo(CaseOutcome.Pass);
        await Assert.That(results[1].Outcome).IsEqualTo(CaseOutcome.Fail);
        await Assert.That(results[1].TestSet).IsEqualTo("stage3-bad");
    }

    [Test]
    public async Task RunBuildOnly_NullManifest_ThrowsArgumentNullException(CancellationToken cancellationToken) =>
        await Assert
            .That(() => FrenchCtcRunner.RunBuildOnly(null!, "root", cancellationToken))
            .Throws<ArgumentNullException>();

    [Test]
    public async Task RatePackage_IsNotThePackageRowOfTheConformanceSuite() =>
        await Assert.That(FrenchCtcRun.RatePackage).IsNotEqualTo("NetEvolve.Schemas.Schematron");

    private static readonly string[] SampleFolders = ["CII", "UBL", "CDAR"];

    private const string Schema =
        """<sch:schema xmlns:sch="http://purl.oclc.org/dsdl/schematron"><sch:pattern><sch:rule context="a"><sch:assert id="a-rule" test="true()"/></sch:rule></sch:pattern></sch:schema>""";

    private static string Format(TestSetReport report, IReadOnlyList<CaseResult> results)
    {
        var builder = new StringBuilder();
        foreach (var id in report.Regressions)
        {
            var reason = results.First(r => string.Equals(r.CaseId, id, StringComparison.Ordinal)).Reason;
            _ = builder.Append("regression (fails, not a known failure): ").Append(id).Append(": ").Append(reason);
            _ = builder.Append('\n');
        }

        foreach (var id in report.UnexpectedPasses)
        {
            _ = builder.Append("unexpected pass (passes, still a known failure; remove it): ").Append(id).Append('\n');
        }

        return builder.ToString();
    }
}
