namespace NetEvolve.Schemas.Schematron.Tests.Integration.Goldens.Stage4Globals;

using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using NetEvolve.Schemas.Conformance.Baseline;
using NetEvolve.Schemas.Conformance.Manifest;
using NetEvolve.Schemas.Conformance.Reporting;
using NetEvolve.Schemas.Conformance.Results;
using NetEvolve.Schemas.Schematron.Tests.Integration.Conformance.Runner;

/// <summary>
/// The acceptance of XSLT stage 4 (global variables and parameters, temporary trees, let with element content): the pinned
/// conformance suite cases and the own test schemas must give the expected result, every case that does not yet is a documented known
/// failure with an issue and a reason, and the pass rate is reported per test set.
/// </summary>
/// <remarks>
/// Unlike the conformance suites, the class runs on every leg: the acceptance holds for the library build of each test leg, and the suite
/// files come from the checksum-pinned cache of the suite manifest. The rates go to the job summary under their own package name
/// (<see cref="Stage4Run.RatePackage"/>), so the four suite cases are not counted twice in the row of the conformance suite.
/// </remarks>
public class Stage4GlobalsTests
{
    private const string PlainSchema =
        """<sch:schema xmlns:sch="http://purl.oclc.org/dsdl/schematron"><sch:pattern><sch:rule id="a-rule" context="a"><sch:report test="true()"/></sch:rule></sch:pattern></sch:schema>""";

    private static readonly XNamespace Xsl = "http://www.w3.org/1999/XSL/Transform";

    public static IEnumerable<Func<string>> TestSets() =>
        [
            () => Stage4Run.ConformanceTestSet,
            .. Stage4Run
                .LoadManifest()
                .OwnSchemas.Select(s => (Func<string>)(() => "stage4-" + Path.GetFileNameWithoutExtension(s.Path))),
        ];

    [Test]
    [MethodDataSource(nameof(TestSets))]
    public async Task TestSet_AgainstKnownFailures_HasNoRegressionAndNoUnexpectedPass(
        string testSet,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var results = (await Stage4Run.GetResultsAsync(cancellationToken))
            .Where(r => string.Equals(r.TestSet, testSet, StringComparison.Ordinal))
            .ToList();
        var known = Stage4Run
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
            Stage4Run.RatePackage,
            TargetFrameworkName.Of(typeof(Stage4GlobalsTests).Assembly),
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
        var ids = (await Stage4Run.GetResultsAsync(cancellationToken)).Select(r => r.CaseId).ToList();

        foreach (var link in Stage4Run.LoadManifest().KnownFailures)
        {
            await Assert.That(ids).Contains(link.CaseId);
            await Assert.That(link.Issue).IsGreaterThan(0);
            await Assert.That(string.IsNullOrWhiteSpace(link.Reason)).IsFalse();
        }
    }

    [Test]
    public async Task Set_UsesAGlobalVariableAGlobalParamAndATemporaryTree()
    {
        var declarations = Stage4Run
            .LoadManifest()
            .OwnSchemas.SelectMany(s =>
                XDocument.Load(Path.Combine(SchematronSuite.RepositoryRoot, s.Path)).Root!.Elements()
            )
            .ToList();
        var variables = declarations.Where(e => e.Name == Xsl + "variable").ToList();
        var parameters = declarations.Where(e => e.Name == Xsl + "param").ToList();

        await Assert.That(variables.Any(e => e.Attribute("select") is not null)).IsTrue();
        await Assert.That(parameters.Any(e => e.Attribute("select") is not null)).IsTrue();

        // A temporary tree is a declaration without select whose content is a sequence constructor with element content.
        await Assert.That(variables.Concat(parameters).Any(IsTemporaryTree)).IsTrue();
    }

    [Test]
    public async Task OwnSchemas_AreCommitted_AndEachHasAValidAndAnInvalidDocument()
    {
        var manifest = Stage4Run.LoadManifest();

        await Assert.That(manifest.OwnSchemas).IsNotEmpty();
        foreach (var schema in manifest.OwnSchemas)
        {
            await Assert.That(File.Exists(Path.Combine(SchematronSuite.RepositoryRoot, schema.Path))).IsTrue();
            await Assert.That(schema.Documents.Any(d => d.ExpectedValid)).IsTrue();
            await Assert.That(schema.Documents.Any(d => !d.ExpectedValid)).IsTrue();
            foreach (var document in schema.Documents)
            {
                await Assert
                    .That(File.Exists(Path.Combine(Stage4Run.StageDirectory, "documents", document.Path)))
                    .IsTrue();
            }
        }
    }

    [Test]
    public async Task Record_NamesTheCasesAndSchemasOfTheStageSources(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var manifest = Stage4Run.LoadManifest();
        using var sources = JsonDocument.Parse(
            await File.ReadAllTextAsync(
                Path.Combine(Stage4Run.SuitesDirectory, "stage-sources.json"),
                cancellationToken
            )
        );
        var stage = sources.RootElement.GetProperty("stage4");

        await Assert.That(manifest.Suite).IsEqualTo(stage.GetProperty("suite").GetString());
        await Assert
            .That(manifest.Cases)
            .IsEquivalentTo(stage.GetProperty("cases").EnumerateArray().Select(e => e.GetString()!));
        await Assert
            .That(manifest.OwnSchemas.Select(s => s.Path))
            .IsEquivalentTo(stage.GetProperty("ownSchemas").EnumerateArray().Select(e => e.GetString()!));
    }

    [Test]
    public async Task Record_IsNoSuiteManifest_AndItsSuiteIsPinnedByChecksum()
    {
        var manifest = Stage4Run.LoadManifest();
        var suites = SuiteManifestLoader.LoadDirectory(Stage4Run.SuitesDirectory);

        var suite = suites.Single(s => string.Equals(s.Key, manifest.Suite, StringComparison.Ordinal));
        await Assert.That(suite.Sha256).Length().IsEqualTo(64);
        await Assert.That(suites.Select(s => s.Key)).DoesNotContain("schematron-stage4");
    }

    [Test]
    public async Task RatePackage_IsNotThePackageRowOfTheConformanceSuite() =>
        await Assert.That(Stage4Run.RatePackage).IsNotEqualTo("NetEvolve.Schemas.Schematron");

    [Test]
    public async Task Parse_Null_ThrowsArgumentNullException() =>
        await Assert.That(() => Stage4Manifest.Parse(null!)).Throws<ArgumentNullException>();

    [Test]
    [Arguments("{")]
    [Arguments("{}")]
    [Arguments("""{"suite":"s","cases":[],"ownSchemas":[],"knownFailures":[{"id":"a","issue":"x","reason":"r"}]}""")]
    [Arguments(
        """{"suite":"s","cases":[],"ownSchemas":[{"schema":"a.sch","documents":[{"document":"d.xml","rules":[]}]}],"knownFailures":[]}"""
    )]
    public async Task Parse_MalformedRecord_ThrowsInvalidDataException(string json) =>
        await Assert.That(() => Stage4Manifest.Parse(json)).Throws<InvalidDataException>();

    [Test]
    public async Task Run_ExpectedRulesDiffer_FailsNamingTheRuleAndTheValidity(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var directory = new StageDirectory();
        directory.Write("rules.sch", PlainSchema);
        directory.Write("documents/d/two.xml", "<root><a/></root>");
        var manifest = Stage4Manifest.Parse(
            """{"suite":"s","cases":[],"knownFailures":[],"ownSchemas":[{"schema":"rules.sch","documents":[{"document":"d/two.xml","valid":true,"rules":["missing-rule"]}]}]}"""
        );

        var results = Stage4OwnSchemaRunner.Run(manifest, directory.Root, directory.Root, cancellationToken);

        await Assert.That(results).HasSingleItem();
        await Assert.That(results[0].Outcome).IsEqualTo(CaseOutcome.Fail);
        await Assert.That(results[0].CaseId).IsEqualTo("rules/two.xml");
        await Assert.That(results[0].TestSet).IsEqualTo("stage4-rules");
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
        var manifest = Stage4Manifest.Parse(
            """{"suite":"s","cases":[],"knownFailures":[],"ownSchemas":[{"schema":"rules.sch","documents":[{"document":"d/one.xml","valid":false,"rules":["a-rule"]}]}]}"""
        );

        var results = Stage4OwnSchemaRunner.Run(manifest, directory.Root, directory.Root, cancellationToken);

        await Assert.That(results.Select(r => r.Outcome)).IsEquivalentTo([CaseOutcome.Pass]);
    }

    private static bool IsTemporaryTree(XElement declaration) =>
        declaration.Attribute("select") is null && declaration.Elements().Any(e => e.Name.Namespace != Xsl);

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
                .CreateDirectory(Path.Combine(Path.GetTempPath(), "stage4-globals-" + Guid.NewGuid().ToString("N")))
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
