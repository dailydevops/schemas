namespace NetEvolve.Schemas.Schematron.Tests.Integration.Goldens.Stage3Documents;

using System.Text;
using NetEvolve.Schemas.Conformance.Baseline;
using NetEvolve.Schemas.Conformance.Manifest;
using NetEvolve.Schemas.Conformance.Reporting;
using NetEvolve.Schemas.Conformance.Results;
using NetEvolve.Schemas.Resources;
using NetEvolve.Schemas.Resources.FileSystem;
using NetEvolve.Schemas.Schematron.Tests.Integration.Conformance.Runner;

/// <summary>
/// The acceptance of XSLT stage 3 (code-database lookups through <c>document()</c>, XSLT 2.0 section 16.1): the sample invoices of the
/// pinned Factur-X rule sets must give their expected rules under a sandboxed file resolver, and the default resolver must deny the
/// lookup. The suite is fetched by its pinned checksum and never committed.
/// </summary>
/// <remarks>
/// Applicable cases: every CII sample invoice of the Factur-X profiles under <c>Z.example/TEST/FX</c>, validated with the rule set of its
/// profile. The record lists every excluded case with its reason. The rate goes to the job summary under its own package name
/// (<see cref="Stage3Run.RatePackage"/>).
/// </remarks>
[Explicit]
[Category(ConformanceCategory.Name)]
public class Stage3DocumentsTests
{
    public static IEnumerable<Func<string>> TestSets() =>
        [
            .. Stage3Run.LoadManifest().RuleSets.Select(r => (Func<string>)(() => "stage3-" + r.Name)),
            () => Stage3Runner.DefaultResolverTestSet,
        ];

    [Test]
    [MethodDataSource(nameof(TestSets))]
    public async Task TestSet_AgainstKnownFailures_HasNoRegressionAndNoUnexpectedPass(
        string testSet,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var results = (await Stage3Run.GetResultsAsync(cancellationToken))
            .Where(r => string.Equals(r.TestSet, testSet, StringComparison.Ordinal))
            .ToList();
        var known = Stage3Run
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
            Stage3Run.RatePackage,
            TargetFrameworkName.Of(typeof(Stage3DocumentsTests).Assembly),
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
        var ids = (await Stage3Run.GetResultsAsync(cancellationToken)).Select(r => r.CaseId).ToList();

        foreach (var link in Stage3Run.LoadManifest().KnownFailures)
        {
            await Assert.That(ids).Contains(link.Id);
            await Assert.That(link.Issue).IsGreaterThan(0);
            await Assert.That(string.IsNullOrWhiteSpace(link.Reason)).IsFalse();
        }
    }

    [Test]
    public async Task RuleSets_EveryRuleSet_LooksUpACodeDatabaseThroughDocument(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var root = await Stage3Run.GetRootAsync(cancellationToken);

        foreach (var ruleSet in Stage3Run.LoadManifest().RuleSets)
        {
            var text = await File.ReadAllTextAsync(Path.Combine(root, ruleSet.Schema), cancellationToken);
            await Assert.That(text).Contains("document('");
            await Assert.That(ruleSet.Documents).IsNotEmpty();
            foreach (var document in ruleSet.Documents)
            {
                await Assert.That(File.Exists(Path.Combine(root, document.Document))).IsTrue();
            }
        }
    }

    [Test]
    public async Task Corpus_EverySampleInvoiceOfTheFacturXProfiles_IsACase(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var root = await Stage3Run.GetRootAsync(cancellationToken);
        var manifest = Stage3Run.LoadManifest();
        var listed = manifest.RuleSets.SelectMany(r => r.Documents).Select(d => d.Document).ToList();
        var invoices = Directory
            .EnumerateFiles(Path.Combine(root, "Z.example", "TEST", "FX"), "*.xml", SearchOption.AllDirectories)
            .Select(f => Path.GetRelativePath(root, f).Replace(Path.DirectorySeparatorChar, '/'));

        await Assert.That(listed).IsEquivalentTo(invoices);
        await Assert.That(listed.Distinct(StringComparer.Ordinal).Count()).IsEqualTo(listed.Count);
        foreach (var exclusion in manifest.Excluded)
        {
            await Assert.That(string.IsNullOrWhiteSpace(exclusion.Subject)).IsFalse();
            await Assert.That(string.IsNullOrWhiteSpace(exclusion.Reason)).IsFalse();
        }
    }

    [Test]
    public async Task SandboxedResolver_PathOutsideTheRoot_IsDenied(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var root = await Stage3Run.GetRootAsync(cancellationToken);
        var directory = Path.GetDirectoryName(Path.Combine(root, Stage3Run.LoadManifest().RuleSets[0].Schema))!;
        var resolver = new SandboxedFileSystemResourceResolver(directory);
        var outside = new Uri(Path.Combine(root, "Z.example", "TEST", "FX", "x.xml")).AbsoluteUri;

        using var result = resolver.Resolve(
            new ResourceRequest(
                ResourceKind.XPathDocument,
                "../../../Z.example/TEST/FX/x.xml",
                new Uri(directory + Path.DirectorySeparatorChar).AbsoluteUri,
                outside
            ),
            cancellationToken
        );

        await Assert.That(result.Status).IsEqualTo(ResourceStatus.Denied);
    }

    [Test]
    public async Task Record_IsNoSuiteManifest_AndItsSuiteIsPinnedByChecksum()
    {
        var manifest = Stage3Run.LoadManifest();
        var suites = SuiteManifestLoader.LoadDirectory(Stage3Run.SuitesDirectory);

        var suite = suites.Single(s => string.Equals(s.Key, manifest.Suite, StringComparison.Ordinal));
        await Assert.That(suite.Sha256).Length().IsEqualTo(64);
        await Assert.That(suites.Select(s => s.Key)).DoesNotContain("schematron-stage3");
    }

    [Test]
    public async Task RatePackage_IsNotThePackageRowOfTheConformanceSuite() =>
        await Assert.That(Stage3Run.RatePackage).IsNotEqualTo("NetEvolve.Schemas.Schematron");

    [Test]
    public async Task Parse_Null_ThrowsArgumentNullException() =>
        await Assert.That(() => Stage3Manifest.Parse(null!)).Throws<ArgumentNullException>();

    [Test]
    [Arguments("{")]
    [Arguments("{}")]
    [Arguments("""{"suite":"s","ruleSets":[]}""")]
    public async Task Parse_MalformedRecord_ThrowsInvalidDataException(string json) =>
        await Assert.That(() => Stage3Manifest.Parse(json)).Throws<InvalidDataException>();

    [Test]
    public async Task Run_ExpectedRulesDiffer_FailsNamingTheRuleAndTheValidity(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var directory = new StageDirectory();
        directory.Write("rules.sch", PlainSchema);
        directory.Write("d/one.xml", "<root><a/></root>");
        var manifest = Stage3Manifest.Parse(
            """{"stage":3,"suite":"s","excluded":[],"knownFailures":[],"ruleSets":[{"name":"r","schema":"rules.sch","documents":[{"document":"d/one.xml","valid":true,"rules":["missing-rule"]}]}]}"""
        );

        var results = Stage3Runner.Run(manifest, directory.Root, resolverFactory: null, cancellationToken);

        await Assert.That(results).HasSingleItem();
        await Assert.That(results[0].Outcome).IsEqualTo(CaseOutcome.Fail);
        await Assert.That(results[0].CaseId).IsEqualTo("r/one.xml");
        await Assert.That(results[0].TestSet).IsEqualTo("stage3-r");
        await Assert.That(results[0].Reason!).Contains("Expected valid but observed invalid.");
        await Assert.That(results[0].Reason!).Contains("Missing expected rule 'missing-rule'.");
        await Assert.That(results[0].Reason!).Contains("Unexpected fired rule 'a-rule'.");
    }

    [Test]
    public async Task Run_ExpectedRulesEqualFiredRules_Passes(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var directory = new StageDirectory();
        directory.Write("rules.sch", PlainSchema);
        directory.Write("d/one.xml", "<root><a/></root>");
        var manifest = Stage3Manifest.Parse(
            """{"stage":3,"suite":"s","excluded":[],"knownFailures":[],"ruleSets":[{"name":"r","schema":"rules.sch","documents":[{"document":"d/one.xml","valid":false,"rules":["a-rule"]}]}]}"""
        );

        var results = Stage3Runner.Run(
            manifest,
            directory.Root,
            d => new SandboxedFileSystemResourceResolver(d),
            cancellationToken
        );

        await Assert.That(results.Select(r => r.Outcome)).IsEquivalentTo([CaseOutcome.Pass]);
    }

    [Test]
    public async Task RunDefaultResolver_ValidationWithoutDenial_FailsTheCase(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var directory = new StageDirectory();
        directory.Write("rules.sch", PlainSchema);
        directory.Write("d/one.xml", "<root><a/></root>");
        var manifest = Stage3Manifest.Parse(
            """{"stage":3,"suite":"s","excluded":[],"knownFailures":[],"ruleSets":[{"name":"r","schema":"rules.sch","documents":[{"document":"d/one.xml","valid":false,"rules":["a-rule"]}]}]}"""
        );

        var results = Stage3Runner.RunDefaultResolver(manifest, directory.Root, cancellationToken);

        await Assert.That(results).HasSingleItem();
        await Assert.That(results[0].CaseId).IsEqualTo("stage3-default-resolver/r");
        await Assert.That(results[0].Outcome).IsEqualTo(CaseOutcome.Fail);
        await Assert.That(results[0].Reason!).Contains("The validation succeeded.");
    }

    private const string PlainSchema =
        """<sch:schema xmlns:sch="http://purl.oclc.org/dsdl/schematron"><sch:pattern><sch:rule context="a"><sch:assert id="a-rule" test="false()"/></sch:rule></sch:pattern></sch:schema>""";

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

    private sealed class StageDirectory : IDisposable
    {
        public string Root { get; } =
            Directory
                .CreateDirectory(Path.Combine(Path.GetTempPath(), "stage3-documents-" + Guid.NewGuid().ToString("N")))
                .FullName;

        public void Write(string relativePath, string content)
        {
            var path = Path.Combine(Root, relativePath);
            _ = Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, content, new UTF8Encoding(false));
        }

        public void Dispose() => Directory.Delete(Root, recursive: true);
    }
}
