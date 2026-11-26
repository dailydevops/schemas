namespace NetEvolve.Schemas.Schematron.Tests.Integration.Goldens.Stage2Keys;

using System.Text;
using System.Xml.Linq;
using NetEvolve.Schemas.Conformance.Baseline;
using NetEvolve.Schemas.Conformance.Manifest;
using NetEvolve.Schemas.Conformance.Reporting;
using NetEvolve.Schemas.Conformance.Results;
using NetEvolve.Schemas.Schematron.Tests.Integration.Goldens.En16931;

/// <summary>
/// The acceptance of XSLT stage 2 (<c>xsl:key</c> and <c>key()</c> on real rules): the key-based EHF rule sets load and fire their key
/// rule as expected, and the official examples and bad examples of the Peppol reporting rule sets give their expected rules. The
/// archives are fetched by their pinned checksums and never committed.
/// </summary>
/// <remarks>
/// Excluded, with the reasons in <c>schematron-stage2.json</c>: the EHF archive ships no test documents, and the reporting rule sets
/// declare no key. Every case that does not pass yet is a known failure of that record with an issue and a reason.
/// </remarks>
[Explicit]
[Category(ConformanceCategory.Name)]
public class Stage2KeysGoldenTests
{
    private static readonly string[] CorpusFolders = ["example", "bad-examples"];
    private static readonly XNamespace Xsl = "http://www.w3.org/1999/XSL/Transform";

    public static IEnumerable<Func<string>> TestSets() =>
        [
            () => Stage2Run.EhfTestSet,
            () => Stage2Run.EhfBuildTestSet,
            () => Stage2Run.EusrTestSet,
            () => Stage2Run.TsrTestSet,
        ];

    public static IEnumerable<Func<(string Suite, string Path, string Rule)>> KeyRuleSets() =>
        Stage2Run.LoadRecord().KeyRuleSets.Select(r => (Func<(string, string, string)>)(() => r));

    [Test]
    [MethodDataSource(nameof(TestSets))]
    public async Task TestSet_AgainstKnownFailures_HasNoRegressionAndNoUnexpectedPass(
        string testSet,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var results = (await Stage2Run.GetResultsAsync(cancellationToken))
            .Where(r => string.Equals(r.TestSet, testSet, StringComparison.Ordinal))
            .ToList();
        var known = Stage2Run
            .LoadRecord()
            .KnownFailures.Select(l => l.CaseId)
            .Where(id => results.Any(r => string.Equals(r.CaseId, id, StringComparison.Ordinal)));

        var report = ConformanceReport.Compare(
            results,
            new Dictionary<string, KnownFailureBaseline>(StringComparer.Ordinal)
            {
                [testSet] = KnownFailureBaseline.Parse(KnownFailureBaseline.Format(known)),
            }
        );
        ConformanceRateReporter.Collect(
            Stage2Run.RatePackage,
            TargetFrameworkName.Of(typeof(Stage2KeysGoldenTests).Assembly),
            report
        );

        await Assert.That(results).IsNotEmpty();
        await Assert.That(report.TestSets).HasSingleItem();
        await Assert.That(report.IsSuccess).IsTrue().Because(Format(report.TestSets[0], results));
    }

    [Test]
    public async Task EhfBuildTestSet_HoldsOneCasePerRuleSetOfThePinnedArchive(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var root = await Stage2Run.GetRootAsync("stage2-ehf", cancellationToken);
        var ruleSets = Directory.GetFiles(root, "*.sch", SearchOption.AllDirectories).Length;

        var ids = (await Stage2Run.GetResultsAsync(cancellationToken))
            .Where(r => string.Equals(r.TestSet, Stage2Run.EhfBuildTestSet, StringComparison.Ordinal))
            .Select(r => r.CaseId)
            .ToList();

        await Assert.That(ruleSets).IsEqualTo(18);
        await Assert.That(ids.Count).IsEqualTo(ruleSets);
        await Assert.That(ids.Distinct(StringComparer.Ordinal).Count()).IsEqualTo(ruleSets);
    }

    [Test]
    public async Task KnownFailures_EveryEntry_NamesARunCaseAndHasAnIssueAndAReason(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var ids = (await Stage2Run.GetResultsAsync(cancellationToken)).Select(r => r.CaseId).ToList();

        foreach (var link in Stage2Run.LoadRecord().KnownFailures)
        {
            await Assert.That(ids).Contains(link.CaseId);
            await Assert.That(link.Issue).IsGreaterThan(0);
            await Assert.That(string.IsNullOrWhiteSpace(link.Reason)).IsFalse();
        }
    }

    [Test]
    [MethodDataSource(nameof(KeyRuleSets))]
    public async Task KeyRuleSet_DeclaresAKeyAndUsesIt(
        (string Suite, string Path, string Rule) ruleSet,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var path = Path.Combine(await Stage2Run.GetRootAsync(ruleSet.Suite, cancellationToken), ruleSet.Path);
        var text = await File.ReadAllTextAsync(path, cancellationToken);
        var document = XDocument.Parse(text);

        await Assert.That(document.Root!.Elements(Xsl + "key").Any()).IsTrue();
        await Assert.That(text).Contains("key('");
        await Assert.That(text).Contains($"id=\"{ruleSet.Rule}\"");
    }

    [Test]
    [Arguments(Stage2Run.EusrTestSet)]
    [Arguments(Stage2Run.TsrTestSet)]
    public async Task ReportingRuleSet_LoadsWithoutUnsupportedXsltConstruct(
        string testSet,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var root = await Stage2Run.GetRootAsync("stage2-peppol-reporting", cancellationToken);

        await Assert.That(() => En16931Runner.Build(Stage2Run.ReportingSchema(root, testSet))).ThrowsNothing();
    }

    [Test]
    [Arguments(Stage2Run.EusrTestSet)]
    [Arguments(Stage2Run.TsrTestSet)]
    public async Task Corpus_EveryOfficialDocument_IsOneCase(string testSet, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var root = await Stage2Run.GetRootAsync("stage2-peppol-reporting", cancellationToken);
        var folder = Stage2Run.ReportingDirectory(root, testSet);
        var files = CorpusFolders.Sum(d => Directory.GetFiles(Path.Combine(folder, d), "*.xml").Length);

        var cases = Stage2Run.LoadReportingCases(root, testSet);

        // The unit-test folder adds one case per schunit test (TSR only).
        await Assert.That(cases.Count).IsGreaterThanOrEqualTo(files);
        await Assert.That(cases.Select(c => c.Id).Distinct(StringComparer.Ordinal).Count()).IsEqualTo(cases.Count);
        await Assert.That(cases.Any(c => c.IsExample)).IsTrue();
        await Assert.That(cases.Any(c => c.ExpectedRuleIds.Count > 0)).IsTrue();
    }

    [Test]
    public async Task ExcludedCases_EachNamesAnExistingCaseAndHasAReason(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var root = await Stage2Run.GetRootAsync("stage2-peppol-reporting", cancellationToken);

        foreach (var (testSet, caseId, reason) in Stage2Run.LoadExcludedCases())
        {
            await Assert.That(Stage2Run.LoadReportingCases(root, testSet).Select(c => c.Id)).Contains(caseId);
            await Assert.That(string.IsNullOrWhiteSpace(reason)).IsFalse();
        }
    }

    [Test]
    public async Task Ehf_PinnedArchiveHoldsNoTestDocuments_OnlyRuleSetsAndReadme(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var root = await Stage2Run.GetRootAsync("stage2-ehf", cancellationToken);

        var others = Directory
            .EnumerateFiles(root, "*", SearchOption.AllDirectories)
            .Where(f => !f.EndsWith(".sch", StringComparison.Ordinal) && Path.GetFileName(f) != "README")
            .ToList();

        await Assert.That(others).IsEmpty().Because(string.Join(", ", others));
    }

    [Test]
    public async Task Record_NamesPinnedSuites_ByChecksum_AndIsNoSuiteManifest()
    {
        var record = Stage2Run.LoadRecord();
        var suites = SuiteManifestLoader.LoadDirectory(Stage2Run.SuitesDirectory);

        await Assert.That(record.Suites).IsNotEmpty();
        foreach (var key in record.Suites)
        {
            await Assert
                .That(suites.Single(s => string.Equals(s.Key, key, StringComparison.Ordinal)).Sha256)
                .Length()
                .IsEqualTo(64);
        }

        await Assert
            .That(record.KeyRuleSets.Select(r => r.Suite).Distinct(StringComparer.Ordinal))
            .IsEquivalentTo(["stage2-ehf"]);
        await Assert.That(suites.Select(s => s.Key)).DoesNotContain("schematron-stage2");
    }

    private static string Format(TestSetReport report, IReadOnlyList<CaseResult> results)
    {
        var builder = new StringBuilder();
        foreach (var id in report.Regressions)
        {
            var reason = results.First(r => string.Equals(r.CaseId, id, StringComparison.Ordinal)).Reason;
            _ = builder
                .Append("regression (fails, not a known failure): ")
                .Append(id)
                .Append(": ")
                .Append(reason)
                .Append('\n');
        }

        foreach (var id in report.UnexpectedPasses)
        {
            _ = builder.Append("unexpected pass (passes, still a known failure; remove it): ").Append(id).Append('\n');
        }

        return builder.ToString();
    }
}
