namespace NetEvolve.Schemas.Schematron.Tests.Integration.Goldens.En16931;

using System.Text;
using System.Text.RegularExpressions;
using NetEvolve.Schemas.Conformance.Baseline;
using NetEvolve.Schemas.Conformance.Reporting;
using NetEvolve.Schemas.Conformance.Results;
using NetEvolve.Schemas.Schematron.Tests.Integration.Conformance.Runner;

/// <summary>
/// The first acceptance of the Schematron core on a real rule set: the CEN EN 16931 unit-test corpus and the official example invoices
/// for UBL and CII. The archive is fetched by its pinned checksum and never committed.
/// </summary>
/// <remarks>
/// Applicable cases: every <c>test</c> of every test-set file of the unit-test corpus and every official example of the syntax, so the
/// pass rate is 100 % of all cases and no case is excluded. The complete invoices of <c>test/testfiles</c> and the EDIFACT examples are separate test sets of <see cref="En16931ExtendedGoldenTests"/>. A document of the corpus is a fragment, so a case compares only the rules in its scope:
/// the scope elements of its test-set file and the ids its asserts name.
/// </remarks>
public class En16931GoldenTests
{
    private const string SampleTestSet = """
        <testSet xmlns="http://difi.no/xsd/vefa/validator/1.0" configuration="tc434-ubl">
          <assert><scope>BR-01</scope></assert>
          <test>
            <assert><description>d</description><success>BR-01</success></assert>
            <Invoice xmlns="urn:x"><a/></Invoice>
          </test>
          <test>
            <assert><description>d</description><error>BR-01</error><warning>BR-02</warning></assert>
            <Invoice xmlns="urn:x"/>
          </test>
          <test>
            <assert><success>BR-CO-03</success></assert>
            <Invoice xmlns="urn:x"/>
          </test>
        </testSet>
        """;

    [Test]
    [Arguments("ubl")]
    [Arguments("cii")]
    public async Task Syntax_FiresExactlyTheExpectedRules_AtOneHundredPercent(
        string syntax,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(syntax);
        var results = (await En16931Run.GetResultsAsync(cancellationToken))[syntax];

        var report = ConformanceReport.Compare(
            results,
            new Dictionary<string, KnownFailureBaseline>(StringComparer.Ordinal)
        );
        ConformanceRateReporter.Collect(
            En16931Run.RatePackage,
            TargetFrameworkName.Of(typeof(En16931GoldenTests).Assembly),
            report
        );

        await Assert.That(results).IsNotEmpty();
        await Assert.That(report.TestSets).HasSingleItem();
        await Assert.That(report.IsSuccess).IsTrue().Because(Format(report.TestSets[0], results));
        await Assert.That(results.All(r => r.Outcome == CaseOutcome.Pass)).IsTrue();
    }

    [Test]
    [Arguments("ubl")]
    [Arguments("cii")]
    public async Task Corpus_EveryTestOfTheArchive_IsOneCase(string syntax, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(syntax);
        var root = await En16931Run.GetRootAsync(cancellationToken);
        var directories = syntax == "ubl" ? new[] { "Invoice-unit-UBL", "CreditNote-unit-UBL" } : ["cii"];
        var tests = 0;
        foreach (var directory in directories)
        {
            foreach (var file in Directory.EnumerateFiles(Path.Combine(root, "test", directory), "*.xml"))
            {
                tests += Regex.Count(await File.ReadAllTextAsync(file, cancellationToken), @"<test(\s[^>]*)?>");
            }
        }

        var cases = En16931Run.LoadCases(root, syntax);

        await Assert.That(tests).IsGreaterThan(0);
        await Assert.That(cases.Count(c => !c.IsExample)).IsEqualTo(tests);
        await Assert.That(cases.Select(c => c.Id).Distinct(StringComparer.Ordinal).Count()).IsEqualTo(cases.Count);
    }

    [Test]
    [Arguments("ubl")]
    [Arguments("cii")]
    public async Task Corpus_FiresRealRules_NotJustValidDocuments(string syntax, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(syntax);
        var root = await En16931Run.GetRootAsync(cancellationToken);

        var cases = En16931Run.LoadCases(root, syntax);

        await Assert.That(cases.Any(c => !c.IsExample && c.ExpectedRuleIds.Count > 0)).IsTrue();
        await Assert.That(cases.Any(c => !c.IsExample && c.ExpectedRuleIds.Count == 0)).IsTrue();
    }

    [Test]
    [Arguments("ubl")]
    [Arguments("cii")]
    public async Task Examples_AreValid(string syntax, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(syntax);
        var examples = (await En16931Run.GetResultsAsync(cancellationToken))[syntax]
            .Where(r => r.CaseId.StartsWith("examples/", StringComparison.Ordinal))
            .ToList();

        await Assert.That(examples).IsNotEmpty();
        foreach (var example in examples)
        {
            await Assert
                .That(example.Outcome)
                .IsEqualTo(CaseOutcome.Pass)
                .Because(example.CaseId + ": " + example.Reason);
        }
    }

    [Test]
    [Arguments("ubl")]
    [Arguments("cii")]
    public async Task Schema_AbstractPatternAndBindingFiles_ExpandWithoutError(
        string syntax,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(syntax);
        var root = await En16931Run.GetRootAsync(cancellationToken);

        var validator = En16931Runner.Build(En16931Run.SchemaPath(root, syntax));

        await Assert.That(validator.Schema.Patterns).IsNotEmpty();
        await Assert.That(validator.Schema.Patterns.Any(p => p.ExpandedFrom is not null)).IsTrue();
        await Assert.That(validator.Schema.Patterns.Any(p => p.Rules.Count > 0)).IsTrue();
    }

    [Test]
    public async Task Manifest_PinsAReleaseTagArchiveByChecksum()
    {
        var manifest = En16931Run.LoadManifest();

        await Assert.That(manifest.Key).IsEqualTo("cen-en16931");
        await Assert.That(manifest.SourceUrl.Scheme).IsEqualTo(Uri.UriSchemeHttps);
        await Assert.That(manifest.SourceUrl.AbsolutePath).Contains("/archive/refs/tags/");
        await Assert.That(manifest.Sha256.Length).IsEqualTo(64);
    }

    [Test]
    public async Task TrackedTree_HoldsNoCenArtifact(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var offenders = new List<string>();
        foreach (var directory in new[] { "src", "tests", "docs", "specs", "templates" })
        {
            var path = Path.Combine(SchematronSuite.RepositoryRoot, directory);
            if (!Directory.Exists(path))
            {
                continue;
            }

            foreach (var file in Walk(path))
            {
                var extension = Path.GetExtension(file);
                if (extension is not (".sch" or ".xslt" or ".xml") || new FileInfo(file).Length > 20 * 1024 * 1024)
                {
                    continue;
                }

                var text = await File.ReadAllTextAsync(file, cancellationToken);
                var isCenRules =
                    extension is ".sch" or ".xslt"
                    && text.Contains("EN16931", StringComparison.Ordinal)
                    && text.Contains("CEN/TC 434", StringComparison.Ordinal);
                if (isCenRules || text.Contains("difi.no/xsd/vefa/validator", StringComparison.Ordinal))
                {
                    offenders.Add(file);
                }
            }
        }

        await Assert.That(offenders).IsEmpty().Because(string.Join(", ", offenders));
    }

    [Test]
    public async Task Parse_Sample_ReadsScopeAndExpectedRules()
    {
        var cases = En16931TestSet.Parse("ubl", "BR-01.xml", SampleTestSet);

        await Assert.That(cases.Select(c => c.Id)).IsEquivalentTo(["BR-01.xml#1", "BR-01.xml#2", "BR-01.xml#3"]);
        await Assert.That(cases[0].ExpectedRuleIds).IsEmpty();
        await Assert.That(cases[0].Scope).IsEquivalentTo(["BR-01"]);
        await Assert.That(cases[1].ExpectedRuleIds).IsEquivalentTo(["BR-01", "BR-02"]);
        await Assert.That(cases[1].Scope).IsEquivalentTo(["BR-01", "BR-02"]);
        await Assert.That(cases[2].Scope).IsEquivalentTo(["BR-01", "BR-CO-03"]);
        await Assert
            .That(Encoding.UTF8.GetString(cases[0].Document))
            .IsEqualTo("""<Invoice xmlns="urn:x"><a /></Invoice>""");
    }

    [Test]
    [Arguments("<other/>")]
    [Arguments(
        """<testSet xmlns="http://difi.no/xsd/vefa/validator/1.0"><test><assert><success>A</success></assert></test></testSet>"""
    )]
    [Arguments("""<testSet xmlns="http://difi.no/xsd/vefa/validator/1.0"><test><Invoice/></test></testSet>""")]
    public async Task Parse_MalformedTestSet_ThrowsInvalidDataException(string xml) =>
        await Assert.That(() => En16931TestSet.Parse("ubl", "x.xml", xml)).Throws<InvalidDataException>();

    [Test]
    public async Task Parse_Null_ThrowsArgumentNullException() =>
        await Assert.That(() => En16931TestSet.Parse("ubl", "x.xml", null!)).Throws<ArgumentNullException>();

    private static IEnumerable<string> Walk(string directory)
    {
        foreach (var file in Directory.EnumerateFiles(directory))
        {
            yield return file;
        }

        foreach (var child in Directory.EnumerateDirectories(directory))
        {
            var name = Path.GetFileName(child);
            if (name is "bin" or "obj" or "artifacts" or "node_modules" or ".git")
            {
                continue;
            }

            foreach (var file in Walk(child))
            {
                yield return file;
            }
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
