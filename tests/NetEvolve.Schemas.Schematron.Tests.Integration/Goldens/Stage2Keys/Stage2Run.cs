namespace NetEvolve.Schemas.Schematron.Tests.Integration.Goldens.Stage2Keys;

using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using NetEvolve.Schemas.Conformance.Fetch;
using NetEvolve.Schemas.Conformance.Manifest;
using NetEvolve.Schemas.Conformance.Results;
using NetEvolve.Schemas.Schematron.Tests.Integration.Conformance.Engine;
using NetEvolve.Schemas.Schematron.Tests.Integration.Conformance.Runner;
using NetEvolve.Schemas.Schematron.Tests.Integration.Goldens.En16931;

/// <summary>
/// Fetches the pinned stage 2 archives (key-based EHF rule sets, Peppol reporting rule sets) and runs the goldens once per process.
/// </summary>
internal static class Stage2Run
{
    /// <summary>The test set of the key-based EHF rules.</summary>
    public const string EhfTestSet = "stage2-ehf-keys";

    /// <summary>The test set that builds every EHF rule set, one case per rule set.</summary>
    public const string EhfBuildTestSet = "stage2-ehf-build";

    /// <summary>The test set of the end user statistics report (EUSR).</summary>
    public const string EusrTestSet = "stage2-eusr";

    /// <summary>The test set of the transaction statistics report (TSR).</summary>
    public const string TsrTestSet = "stage2-tsr";

    /// <summary>The package name of the rate rows in the job summary.</summary>
    public const string RatePackage = "NetEvolve.Schemas.Schematron (stage 2 goldens)";

    private const string RecordFile = "schematron-stage2.json";
    private const string ReportingFolder = "edec-reporting-eusr-1.1.0";
    private const string EusrFolder = "src/eusr/trn-end_user_statistics";
    private const string TsrFolder = "src/tsr/trn-transaction_statistics";

    private static readonly HttpClient Client = new() { Timeout = TimeSpan.FromMinutes(10) };
    private static readonly SemaphoreSlim Gate = new(1, 1);
    private static readonly Dictionary<string, string> s_roots = new(StringComparer.Ordinal);
    private static IReadOnlyList<CaseResult>? s_results;

    /// <summary>Gets the directory of the suite manifests.</summary>
    public static string SuitesDirectory { get; } =
        Path.Combine(SchematronSuite.RepositoryRoot, "tests", "conformance", "suites");

    /// <summary>Reads the stage record.</summary>
    /// <returns>The suite keys, the key-based rule sets with their key rule, and the known failures.</returns>
    public static (
        IReadOnlyList<string> Suites,
        IReadOnlyList<(string Suite, string Path, string Rule)> KeyRuleSets,
        IReadOnlyList<KnownFailureLink> KnownFailures
    ) LoadRecord()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(SuitesDirectory, RecordFile)));
        var root = document.RootElement;
        return (
            [.. root.GetProperty("suites").EnumerateArray().Select(e => e.GetString()!)],
            [
                .. root.GetProperty("keyRuleSets")
                    .EnumerateArray()
                    .Select(e =>
                        (
                            e.GetProperty("suite").GetString()!,
                            e.GetProperty("path").GetString()!,
                            e.GetProperty("rule").GetString()!
                        )
                    ),
            ],
            [
                .. root.GetProperty("knownFailures")
                    .EnumerateArray()
                    .Select(e => new KnownFailureLink(
                        e.GetProperty("id").GetString()!,
                        e.GetProperty("issue").GetInt32(),
                        e.GetProperty("reason").GetString()!
                    )),
            ]
        );
    }

    /// <summary>Reads the cases that are excluded from the run, each with a reason in the record.</summary>
    /// <returns>The excluded case ids by test set.</returns>
    public static IReadOnlyList<(string TestSet, string CaseId, string Reason)> LoadExcludedCases()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(SuitesDirectory, RecordFile)));
        return
        [
            .. document
                .RootElement.GetProperty("excluded")
                .EnumerateArray()
                .Where(e => e.TryGetProperty("case", out _))
                .Select(e =>
                    (
                        e.GetProperty("testSet").GetString()!,
                        e.GetProperty("case").GetString()!,
                        e.GetProperty("reason").GetString()!
                    )
                ),
        ];
    }

    /// <summary>Fetches an archive by its pinned checksum, or reuses the cache.</summary>
    /// <param name="key">The suite key.</param>
    /// <param name="cancellationToken">A token to cancel the fetch.</param>
    /// <returns>The suite root.</returns>
    public static async Task<string> GetRootAsync(string key, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await Gate.WaitAsync(cancellationToken);
        try
        {
            return await GetRootCoreAsync(key, cancellationToken);
        }
        finally
        {
            _ = Gate.Release();
        }
    }

    /// <summary>Gets the full path of the EUSR or TSR rule set.</summary>
    /// <param name="reportingRoot">The root of the reporting archive.</param>
    /// <param name="testSet">The test set.</param>
    /// <returns>The path of the schema.</returns>
    public static string ReportingSchema(string reportingRoot, string testSet) =>
        Directory.GetFiles(Path.Combine(ReportingDirectory(reportingRoot, testSet), "schematron"), "*.sch")[0];

    /// <summary>Gets the directory of the EUSR or TSR transaction in the reporting archive.</summary>
    /// <param name="reportingRoot">The root of the reporting archive.</param>
    /// <param name="testSet">The test set.</param>
    /// <returns>The directory.</returns>
    public static string ReportingDirectory(string reportingRoot, string testSet) =>
        Path.Combine(reportingRoot, ReportingFolder, testSet == EusrTestSet ? EusrFolder : TsrFolder);

    /// <summary>Reads the cases of the official documents of the EUSR or TSR: examples (valid) and bad examples (one rule each; the other rules that a bad example fires on the side are not compared, as in the CEN goldens).</summary>
    /// <param name="reportingRoot">The root of the reporting archive.</param>
    /// <param name="testSet">The test set.</param>
    /// <returns>The cases in a stable order.</returns>
    public static IReadOnlyList<En16931Case> LoadReportingCases(string reportingRoot, string testSet)
    {
        var folder = ReportingDirectory(reportingRoot, testSet);
        var prefix = testSet == EusrTestSet ? "EUSR" : "TSR";
        var cases = new List<En16931Case>();
        foreach (var file in SortedXml(Path.Combine(folder, "example")))
        {
            cases.Add(new(testSet, $"example/{Path.GetFileName(file)}", File.ReadAllBytes(file), [], [], true));
        }

        foreach (var file in SortedXml(Path.Combine(folder, "bad-examples")))
        {
            var rule = $"SCH-{prefix}-{Path.GetFileNameWithoutExtension(file).Split('-')[1]}";
            cases.Add(
                new(testSet, $"bad-examples/{Path.GetFileName(file)}", File.ReadAllBytes(file), [rule], [rule], false)
            );
        }

        // The schunit unit tests (TSR only): one Test per document, the Trigger names the rule that must fire.
        foreach (var file in SortedXml(Path.Combine(folder, "schematron", "unit-test", "bad")))
        {
            var position = 0;
            foreach (var test in XDocument.Load(file).Descendants().Where(e => e.Name.LocalName == "Test"))
            {
                position++;
                var rule = test.Elements().First(e => e.Name.LocalName == "Trigger").Value.Trim();
                var document = test.Elements().First(e => e.Name.LocalName != "Trigger");
                cases.Add(
                    new(
                        testSet,
                        $"unit-test/bad/{Path.GetFileName(file)}#{position}",
                        Encoding.UTF8.GetBytes(document.ToString()),
                        [rule],
                        [rule],
                        false
                    )
                );
            }
        }

        return cases;
    }

    /// <summary>Gets the results of every case.</summary>
    /// <param name="cancellationToken">A token to cancel the run.</param>
    /// <returns>One result per case; the test set is in <see cref="CaseResult.TestSet"/>.</returns>
    public static async Task<IReadOnlyList<CaseResult>> GetResultsAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await Gate.WaitAsync(cancellationToken);
        try
        {
            return s_results ??= await RunAsync(cancellationToken);
        }
        finally
        {
            _ = Gate.Release();
        }
    }

    private static async Task<IReadOnlyList<CaseResult>> RunAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var results = new List<CaseResult>();
        var reporting = await GetRootCoreAsync("stage2-peppol-reporting", cancellationToken);
        var excluded = LoadExcludedCases();
        foreach (var testSet in new[] { EusrTestSet, TsrTestSet })
        {
            results.AddRange(
                En16931Runner.Run(
                    ReportingSchema(reporting, testSet),
                    [
                        .. LoadReportingCases(reporting, testSet)
                            .Where(c =>
                                !excluded.Any(x =>
                                    x.TestSet == testSet && string.Equals(x.CaseId, c.Id, StringComparison.Ordinal)
                                )
                            ),
                    ],
                    cancellationToken
                )
            );
        }

        var ehf = await GetRootCoreAsync("stage2-ehf", cancellationToken);
        foreach (var (_, path, rule) in LoadRecord().KeyRuleSets)
        {
            var stem = Path.GetFileNameWithoutExtension(path);
            var cases = new[]
            {
                EhfCase(stem, "unique-line-ids", ["1", "2"], rule, fires: false),
                EhfCase(stem, "duplicate-line-ids", ["1", "1"], rule, fires: true),
            };
            try
            {
                results.AddRange(
                    En16931Runner.Run(Path.Combine(ehf, path), cases, cancellationToken, globalPatternLets: true)
                );
            }
            catch (SchemaCompilationException exception)
            {
                // The rule set does not build: every case of it fails with the issues, which the known failures link.
                var reason =
                    "The rule set does not build: "
                    + string.Join(" | ", exception.Issues.Select(i => i.Code).Distinct().Select(c => c.ToString()));
                results.AddRange(cases.Select(c => new CaseResult(c.Id, c.Syntax, CaseOutcome.Fail, reason)));
            }
        }

        // Build gate: every rule set of the package builds, one case per rule set (also those that use no key).
        foreach (
            var file in Directory
                .GetFiles(ehf, "*.sch", SearchOption.AllDirectories)
                .OrderBy(f => f, StringComparer.Ordinal)
        )
        {
            cancellationToken.ThrowIfCancellationRequested();
            var id = Path.GetRelativePath(ehf, file).Replace(Path.DirectorySeparatorChar, '/');
            try
            {
                _ = En16931Runner.Build(file, globalPatternLets: true);
                results.Add(new CaseResult(id, EhfBuildTestSet, CaseOutcome.Pass));
            }
            catch (SchemaCompilationException exception)
            {
                var reason =
                    "The rule set does not build: "
                    + string.Join(" | ", exception.Issues.Select(i => i.Code).Distinct().Select(c => c.ToString()));
                results.Add(new CaseResult(id, EhfBuildTestSet, CaseOutcome.Fail, reason));
            }
        }

        return results;
    }

    private static En16931Case EhfCase(string stem, string name, string[] ids, string rule, bool fires)
    {
        var lines = string.Concat(
            ids.Select(id => $"<cac:OrderLine><cac:LineItem><cbc:ID>{id}</cbc:ID></cac:LineItem></cac:OrderLine>")
        );
        var xml =
            $"""<Doc xmlns="urn:test" xmlns:cac="urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2" xmlns:cbc="urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2">{lines}</Doc>""";
        return new En16931Case(
            EhfTestSet,
            $"{stem}/{name}.xml",
            Encoding.UTF8.GetBytes(xml),
            [rule],
            fires ? [rule] : [],
            isExample: false
        );
    }

    private static async Task<string> GetRootCoreAsync(string key, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!s_roots.TryGetValue(key, out var root))
        {
            var manifest = SuiteManifestLoader
                .LoadDirectory(SuitesDirectory)
                .Single(m => string.Equals(m.Key, key, StringComparison.Ordinal));
            root = await new SuiteFetcher(new HttpSuiteTransport(Client), ConformanceCache.ResolveRoot()).FetchAsync(
                manifest,
                cancellationToken
            );
            s_roots[key] = root;
        }

        return root;
    }

    private static IEnumerable<string> SortedXml(string directory) =>
        Directory.Exists(directory)
            ? Directory.GetFiles(directory, "*.xml").OrderBy(f => f, StringComparer.Ordinal)
            : [];
}
