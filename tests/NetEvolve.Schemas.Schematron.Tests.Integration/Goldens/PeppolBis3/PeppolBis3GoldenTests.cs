namespace NetEvolve.Schemas.Schematron.Tests.Integration.Goldens.PeppolBis3;

using System.Text;
using System.Xml.Linq;
using NetEvolve.Schemas.Conformance.Baseline;
using NetEvolve.Schemas.Conformance.Reporting;
using NetEvolve.Schemas.Conformance.Results;
using NetEvolve.Schemas.Schematron.Tests.Integration.Conformance.Runner;
using NetEvolve.Schemas.Schematron.Tests.Integration.Goldens.En16931;

/// <summary>
/// The acceptance of XSLT stage 1 on the OpenPEPPOL BIS Billing 3 rules: the rule unit tests of the Peppol rule sets for UBL and CII.
/// The archive is fetched by its pinned checksum and never committed.
/// </summary>
/// <remarks>
/// Applicable cases: every <c>test</c> of every file in <c>unit-UBL-PEPPOL</c> and <c>unit-CII-PEPPOL</c>, which test the Peppol rule
/// sets <c>PEPPOL-EN16931-UBL.sch</c> and <c>PEPPOL-EN16931-CII.sch</c>. No case is excluded. Out of scope: the national unit-test
/// folders, the envelope rules, the example invoices, and the CEN base rules (tested in the EN 16931 goldens).
/// </remarks>
[Explicit]
[Category(ConformanceCategory.Name)]
public class PeppolBis3GoldenTests
{
    private const string Invoice = """
        <Invoice xmlns:cac="urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2" xmlns:cbc="urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2" xmlns="urn:oasis:names:specification:ubl:schema:xsd:Invoice-2">
          <cac:AccountingCustomerParty><cac:Party><cbc:EndpointID schemeID="SCHEME">VALUE</cbc:EndpointID></cac:Party></cac:AccountingCustomerParty>
        </Invoice>
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
        var results = (await PeppolBis3Run.GetResultsAsync(cancellationToken))[syntax];

        var report = ConformanceReport.Compare(
            results,
            new Dictionary<string, KnownFailureBaseline>(StringComparer.Ordinal)
        );
        ConformanceRateReporter.Collect(
            PeppolBis3Run.RatePackage,
            TargetFrameworkName.Of(typeof(PeppolBis3GoldenTests).Assembly),
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
        var root = await PeppolBis3Run.GetRootAsync(cancellationToken);
        var tests = 0;
        foreach (var file in PeppolBis3Run.UnitFiles(root, syntax))
        {
            tests += XDocument
                .Parse(await File.ReadAllTextAsync(file, cancellationToken))
                .Descendants()
                .Count(e => e.Name.LocalName == "test");
        }

        var cases = PeppolBis3Run.LoadCases(root, syntax);

        await Assert.That(tests).IsGreaterThan(0);
        await Assert.That(cases.Count).IsEqualTo(tests);
        await Assert.That(cases.Select(c => c.Id).Distinct(StringComparer.Ordinal).Count()).IsEqualTo(cases.Count);
        await Assert.That(cases.Any(c => c.ExpectedRuleIds.Count > 0)).IsTrue();
        await Assert.That(cases.Any(c => c.ExpectedRuleIds.Count == 0)).IsTrue();
    }

    [Test]
    [Arguments("ubl")]
    [Arguments("cii")]
    public async Task RuleSet_LoadsWithoutUnsupportedXsltConstruct(string syntax, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(syntax);
        var root = await PeppolBis3Run.GetRootAsync(cancellationToken);

        string[] problems;
        try
        {
            _ = En16931Runner.Build(PeppolBis3Run.SchemaPath(root, syntax));
            problems = [];
        }
        catch (SchemaCompilationException exception)
        {
            problems =
            [
                .. exception.Issues.Select(i => $"{i.Code} {i.Location}: {i.Message}").Distinct(StringComparer.Ordinal),
            ];
        }

        await Assert.That(problems).IsEmpty().Because(string.Join(" | ", problems));
    }

    [Test]
    [Arguments("0088", "9429041098400", false)]
    [Arguments("0088", "9429041098401", true)]
    [Arguments("0192", "991825827", false)]
    [Arguments("0192", "991825828", true)]
    public async Task Functions_GlnAndMod11_ReturnTheExpectedResult(
        string scheme,
        string value,
        bool fires,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(scheme);
        ArgumentNullException.ThrowIfNull(value);
        var root = await PeppolBis3Run.GetRootAsync(cancellationToken);
        var ruleId = scheme == "0088" ? "PEPPOL-COMMON-R040" : "PEPPOL-COMMON-R041";
        var xml = Invoice
            .Replace("SCHEME", scheme, StringComparison.Ordinal)
            .Replace("VALUE", value, StringComparison.Ordinal);
        var testCase = new En16931Case(
            "ubl",
            $"inline-{scheme}-{value}.xml",
            Encoding.UTF8.GetBytes(xml),
            [ruleId],
            fires ? [ruleId] : [],
            isExample: false
        );

        var result = En16931Runner.Run(PeppolBis3Run.SchemaPath(root, "ubl"), [testCase], cancellationToken).Single();

        await Assert.That(result.Outcome).IsEqualTo(CaseOutcome.Pass).Because(result.Reason);
    }

    [Test]
    public async Task Manifest_PinsAReleaseTagArchiveByChecksum()
    {
        var manifest = PeppolBis3Run.LoadManifest();

        await Assert.That(manifest.Key).IsEqualTo("openpeppol-bis3");
        await Assert.That(manifest.SourceUrl.Scheme).IsEqualTo(Uri.UriSchemeHttps);
        await Assert.That(manifest.SourceUrl.AbsolutePath).Contains("/archive/refs/tags/");
        await Assert.That(manifest.Sha256.Length).IsEqualTo(64);
    }

    [Test]
    public async Task TrackedTree_HoldsNoOpenPeppolArtifact(CancellationToken cancellationToken)
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
                if (Path.GetExtension(file) is not (".sch" or ".xslt") || new FileInfo(file).Length > 20 * 1024 * 1024)
                {
                    continue;
                }

                var text = await File.ReadAllTextAsync(file, cancellationToken);
                if (text.Contains("Rules for Peppol BIS 3.0 Billing", StringComparison.Ordinal))
                {
                    offenders.Add(file);
                }
            }
        }

        await Assert.That(offenders).IsEmpty().Because(string.Join(", ", offenders));
    }

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
