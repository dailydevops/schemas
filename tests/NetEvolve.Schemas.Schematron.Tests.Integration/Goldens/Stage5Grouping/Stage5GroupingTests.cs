namespace NetEvolve.Schemas.Schematron.Tests.Integration.Goldens.Stage5Grouping;

using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using NetEvolve.Schemas.Conformance.Baseline;
using NetEvolve.Schemas.Conformance.Manifest;
using NetEvolve.Schemas.Conformance.Reporting;
using NetEvolve.Schemas.Conformance.Results;
using NetEvolve.Schemas.Schematron.Tests.Integration.Conformance.Runner;

/// <summary>
/// The acceptance of XSLT stage 5 (<c>xsl:for-each-group</c> with all four grouping modes and <c>xsl:analyze-string</c> with
/// <c>regex-group</c>, inside <c>xsl:function</c>): the own test schemas must give the expected result, every case that does not yet is a
/// documented known failure with an issue and a reason, and the pass rate is reported per test set.
/// </summary>
/// <remarks>
/// Unlike the conformance suites, the class runs on every leg: the acceptance holds for the library build of each test leg, and it needs no
/// fetched suite files. The rates go to the job summary under their own package name (<see cref="Stage5Run.RatePackage"/>).
/// </remarks>
public class Stage5GroupingTests
{
    private const string PlainSchema =
        """<sch:schema xmlns:sch="http://purl.oclc.org/dsdl/schematron"><sch:pattern><sch:rule id="a-rule" context="a"><sch:report test="true()"/></sch:rule></sch:pattern></sch:schema>""";

    private static readonly XNamespace Xsl = "http://www.w3.org/1999/XSL/Transform";

    public static IEnumerable<Func<string>> TestSets() =>
        Stage5Run
            .LoadManifest()
            .OwnSchemas.Select(s => (Func<string>)(() => "stage5-" + Path.GetFileNameWithoutExtension(s.Path)));

    [Test]
    [MethodDataSource(nameof(TestSets))]
    public async Task TestSet_AgainstKnownFailures_HasNoRegressionAndNoUnexpectedPass(
        string testSet,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var results = (await Stage5Run.GetResultsAsync(cancellationToken))
            .Where(r => string.Equals(r.TestSet, testSet, StringComparison.Ordinal))
            .ToList();
        var known = Stage5Run
            .LoadManifest()
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
            Stage5Run.RatePackage,
            TargetFrameworkName.Of(typeof(Stage5GroupingTests).Assembly),
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
        var ids = (await Stage5Run.GetResultsAsync(cancellationToken)).Select(r => r.CaseId).ToList();

        foreach (var link in Stage5Run.LoadManifest().KnownFailures)
        {
            await Assert.That(ids).Contains(link.CaseId);
            await Assert.That(link.Issue).IsGreaterThan(0);
            await Assert.That(string.IsNullOrWhiteSpace(link.Reason)).IsFalse();
        }
    }

    [Test]
    [Arguments("group-by")]
    [Arguments("group-adjacent")]
    [Arguments("group-starting-with")]
    [Arguments("group-ending-with")]
    public async Task Set_UsesEachGroupingModeInsideAFunction(string mode)
    {
        var groups = Functions().SelectMany(f => f.Descendants(Xsl + "for-each-group")).ToList();

        await Assert.That(groups.Any(g => g.Attribute(mode) is not null)).IsTrue();
    }

    [Test]
    public async Task Set_UsesAnalyzeStringWithRegexGroupInsideAFunction()
    {
        var analyses = Functions().SelectMany(f => f.Descendants(Xsl + "analyze-string")).ToList();

        await Assert.That(analyses).IsNotEmpty();
        await Assert
            .That(
                analyses.Any(a =>
                    a.Element(Xsl + "matching-substring")
                        ?.DescendantsAndSelf()
                        .Attributes()
                        .Any(x => x.Value.Contains("regex-group(", StringComparison.Ordinal)) == true
                )
            )
            .IsTrue();
    }

    [Test]
    public async Task OwnSchemas_AreCommitted_AndEachHasAValidAnInvalidAFiringAndANonFiringDocument()
    {
        var manifest = Stage5Run.LoadManifest();

        await Assert.That(manifest.OwnSchemas).IsNotEmpty();
        foreach (var schema in manifest.OwnSchemas)
        {
            await Assert.That(File.Exists(Path.Combine(SchematronSuite.RepositoryRoot, schema.Path))).IsTrue();
            await Assert.That(schema.Documents.Any(d => d.ExpectedValid)).IsTrue();
            await Assert.That(schema.Documents.Any(d => !d.ExpectedValid)).IsTrue();
            await Assert.That(schema.Documents.Any(d => d.ExpectedRuleIds.Count > 0)).IsTrue();
            await Assert.That(schema.Documents.Any(d => d.ExpectedRuleIds.Count == 0)).IsTrue();
            foreach (var document in schema.Documents)
            {
                await Assert
                    .That(File.Exists(Path.Combine(Stage5Run.StageDirectory, "documents", document.Path)))
                    .IsTrue();
            }
        }
    }

    [Test]
    public async Task Record_NamesTheSchemasOfTheStageSources(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var manifest = Stage5Run.LoadManifest();
        using var sources = JsonDocument.Parse(
            await File.ReadAllTextAsync(
                Path.Combine(Stage5Run.SuitesDirectory, "stage-sources.json"),
                cancellationToken
            )
        );
        var stage = sources.RootElement.GetProperty("stage5");

        await Assert
            .That(manifest.OwnSchemas.Select(s => s.Path))
            .IsEquivalentTo(stage.GetProperty("ownSchemas").EnumerateArray().Select(e => e.GetString()!));
    }

    [Test]
    public async Task Record_IsNoSuiteManifest()
    {
        var suites = SuiteManifestLoader.LoadDirectory(Stage5Run.SuitesDirectory);

        await Assert.That(suites.Select(s => s.Key)).DoesNotContain("schematron-stage5");
    }

    [Test]
    public async Task RatePackage_IsNotThePackageRowOfTheConformanceSuite() =>
        await Assert.That(Stage5Run.RatePackage).IsNotEqualTo("NetEvolve.Schemas.Schematron");

    [Test]
    public async Task Parse_Null_ThrowsArgumentNullException() =>
        await Assert.That(() => Stage5Manifest.Parse(null!)).Throws<ArgumentNullException>();

    [Test]
    [Arguments("{")]
    [Arguments("{}")]
    [Arguments("""{"ownSchemas":[],"knownFailures":[{"id":"a","issue":"x","reason":"r"}]}""")]
    [Arguments(
        """{"ownSchemas":[{"schema":"a.sch","documents":[{"document":"d.xml","rules":[]}]}],"knownFailures":[]}"""
    )]
    public async Task Parse_MalformedRecord_ThrowsInvalidDataException(string json) =>
        await Assert.That(() => Stage5Manifest.Parse(json)).Throws<InvalidDataException>();

    [Test]
    public async Task Run_ExpectedRulesDiffer_FailsNamingTheRuleAndTheValidity(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var directory = new StageDirectory();
        directory.Write("rules.sch", PlainSchema);
        directory.Write("documents/d/two.xml", "<root><a/></root>");
        var manifest = Stage5Manifest.Parse(
            """{"knownFailures":[],"ownSchemas":[{"schema":"rules.sch","documents":[{"document":"d/two.xml","valid":true,"rules":["missing-rule"]}]}]}"""
        );

        var results = Stage5OwnSchemaRunner.Run(manifest, directory.Root, directory.Root, cancellationToken);

        await Assert.That(results).HasSingleItem();
        await Assert.That(results[0].Outcome).IsEqualTo(CaseOutcome.Fail);
        await Assert.That(results[0].CaseId).IsEqualTo("rules/two.xml");
        await Assert.That(results[0].TestSet).IsEqualTo("stage5-rules");
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
        directory.Write("documents/d/one.xml", "<root><a/><a/></root>");
        var manifest = Stage5Manifest.Parse(
            """{"knownFailures":[],"ownSchemas":[{"schema":"rules.sch","documents":[{"document":"d/one.xml","valid":false,"rules":["a-rule"]}]}]}"""
        );

        var results = Stage5OwnSchemaRunner.Run(manifest, directory.Root, directory.Root, cancellationToken);

        await Assert.That(results.Select(r => r.Outcome)).IsEquivalentTo([CaseOutcome.Pass]);
    }

    private static IEnumerable<XElement> Functions() =>
        Stage5Run
            .LoadManifest()
            .OwnSchemas.SelectMany(s =>
                XDocument.Load(Path.Combine(SchematronSuite.RepositoryRoot, s.Path)).Root!.Elements(Xsl + "function")
            );

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

    private sealed class StageDirectory : IDisposable
    {
        public string Root { get; } =
            Directory
                .CreateDirectory(Path.Combine(Path.GetTempPath(), "stage5-grouping-" + Guid.NewGuid().ToString("N")))
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
