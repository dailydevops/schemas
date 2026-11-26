namespace NetEvolve.Schemas.Schematron.Tests.Integration.Goldens.PeppolBis3;

using NetEvolve.Schemas.Conformance.Fetch;
using NetEvolve.Schemas.Conformance.Manifest;
using NetEvolve.Schemas.Conformance.Results;
using NetEvolve.Schemas.Schematron.Tests.Integration.Conformance.Runner;
using NetEvolve.Schemas.Schematron.Tests.Integration.Goldens.En16931;

/// <summary>Fetches the pinned OpenPEPPOL BIS Billing 3 archive and runs the rule unit tests once per process.</summary>
/// <remarks>
/// The unit-test files use the same test-set format as the CEN corpus, so the cases are read and compared with the EN 16931 golden
/// helpers.
/// </remarks>
internal static class PeppolBis3Run
{
    /// <summary>The key of the suite manifest.</summary>
    public const string SuiteKey = "openpeppol-bis3";

    /// <summary>The package name of the rate rows in the job summary.</summary>
    public const string RatePackage = "NetEvolve.Schemas.Schematron (OpenPEPPOL BIS 3 goldens)";

    /// <summary>The package name of the rate rows of the national and envelope unit tests in the job summary.</summary>
    public const string NationalRatePackage = "NetEvolve.Schemas.Schematron (OpenPEPPOL BIS 3 national goldens)";

    /// <summary>The syntaxes of the Peppol rule sets, which are also the test sets of their results.</summary>
    public static readonly string[] Syntaxes = ["ubl", "cii"];

    private const string EnvelopeFolder = "unit-SBDH";
    private const string EnvelopeSchema = "PeppolBIS-Envelope-Rules.sch";
    private const string EnvelopeTestSet = "sbdh";

    /// <summary>Gets the directory of the known-failure baselines of the national and envelope test sets.</summary>
    public static string BaselineDirectory { get; } =
        Path.Combine(SchematronSuite.RepositoryRoot, "tests", "conformance", "baselines", "peppol-bis3");

    private static readonly HttpClient Client = new() { Timeout = TimeSpan.FromMinutes(10) };
    private static readonly SemaphoreSlim Gate = new(1, 1);
    private static Dictionary<string, IReadOnlyList<CaseResult>>? s_results;
    private static string? s_root;

    /// <summary>Reads the manifest that pins the archive.</summary>
    /// <returns>The manifest.</returns>
    public static SuiteManifest LoadManifest() =>
        SuiteManifestLoader
            .LoadDirectory(Path.Combine(SchematronSuite.RepositoryRoot, "tests", "conformance", "suites"))
            .Single(m => string.Equals(m.Key, SuiteKey, StringComparison.Ordinal));

    /// <summary>Gets the full path of the Peppol rule set of a syntax.</summary>
    /// <param name="root">The suite root.</param>
    /// <param name="syntax">The syntax.</param>
    /// <returns>The path of the schema.</returns>
    public static string SchemaPath(string root, string syntax) =>
        Path.Combine(
            root,
            "sch",
            syntax == EnvelopeTestSet ? EnvelopeSchema : $"PEPPOL-EN16931-{SyntaxOf(syntax).ToUpperInvariant()}.sch"
        );

    /// <summary>Gets the test sets of the archive: the Peppol rules, every national folder, and the envelope.</summary>
    /// <remarks>
    /// The test set of <c>unit-UBL-PEPPOL</c> is <c>ubl</c>, of <c>unit-UBL-DE</c> it is <c>ubl-de</c>, and of <c>unit-SBDH</c> it is
    /// <c>sbdh</c>. The national folders test the Peppol rule sets of their syntax.
    /// </remarks>
    /// <param name="root">The suite root.</param>
    /// <returns>The test set names in a stable order.</returns>
    public static IReadOnlyList<string> TestSets(string root)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(root);
        return
        [
            .. Directory
                .EnumerateDirectories(root, "unit-*")
                .Select(d =>
                    Path.GetFileName(d)["unit-".Length..].Replace("-PEPPOL", string.Empty, StringComparison.Ordinal)
                )
                .Select(n => string.Concat(n.Select(char.ToLowerInvariant)))
                .OrderBy(n => n, StringComparer.Ordinal),
        ];
    }

    /// <summary>Gets the syntax of a test set: <c>ubl</c> or <c>cii</c> for the rule sets, <c>sbdh</c> for the envelope.</summary>
    /// <param name="testSet">The test set name.</param>
    /// <returns>The syntax.</returns>
    public static string SyntaxOf(string testSet)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(testSet);
        var dash = testSet.IndexOf('-', StringComparison.Ordinal);
        return dash < 0 ? testSet : testSet[..dash];
    }

    /// <summary>Gets the unit-test directory of a test set.</summary>
    /// <param name="root">The suite root.</param>
    /// <param name="syntax">The test set name.</param>
    /// <returns>The directory of the Peppol rule unit tests.</returns>
    public static string UnitDirectory(string root, string syntax) =>
        Path.Combine(
            root,
            syntax == EnvelopeTestSet ? EnvelopeFolder
                : syntax.Contains('-', StringComparison.Ordinal) ? $"unit-{syntax.ToUpperInvariant()}"
                : $"unit-{syntax.ToUpperInvariant()}-PEPPOL"
        );

    /// <summary>Gets the test-set files of a syntax; the archive spells one file extension <c>.xm</c>, which is included.</summary>
    /// <param name="root">The suite root.</param>
    /// <param name="syntax">The syntax.</param>
    /// <returns>The files in a stable order.</returns>
    public static IReadOnlyList<string> UnitFiles(string root, string syntax) =>
        [
            .. Directory
                .EnumerateFiles(UnitDirectory(root, syntax))
                .Where(f => Path.GetExtension(f) is ".xml" or ".xm")
                .OrderBy(f => f, StringComparer.Ordinal),
        ];

    /// <summary>Fetches the archive by its pinned checksum, or reuses the cache.</summary>
    /// <param name="cancellationToken">A token to cancel the fetch.</param>
    /// <returns>The suite root.</returns>
    public static async Task<string> GetRootAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await Gate.WaitAsync(cancellationToken);
        try
        {
            return s_root ??= await new SuiteFetcher(
                new HttpSuiteTransport(Client),
                ConformanceCache.ResolveRoot()
            ).FetchAsync(LoadManifest(), cancellationToken);
        }
        finally
        {
            _ = Gate.Release();
        }
    }

    /// <summary>Reads the cases of a syntax: one per <c>test</c> of every unit-test file.</summary>
    /// <param name="root">The suite root.</param>
    /// <param name="syntax">The syntax.</param>
    /// <returns>The cases in a stable order.</returns>
    public static IReadOnlyList<En16931Case> LoadCases(string root, string syntax) =>
        [
            .. UnitFiles(root, syntax)
                .SelectMany(file => En16931TestSet.Parse(syntax, Path.GetFileName(file), File.ReadAllText(file))),
        ];

    /// <summary>Gets the results of every syntax.</summary>
    /// <param name="cancellationToken">A token to cancel the run.</param>
    /// <returns>The results by syntax.</returns>
    public static async Task<IReadOnlyDictionary<string, IReadOnlyList<CaseResult>>> GetResultsAsync(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var root = await GetRootAsync(cancellationToken);
        await Gate.WaitAsync(cancellationToken);
        try
        {
            if (s_results is null)
            {
                var results = new Dictionary<string, IReadOnlyList<CaseResult>>(StringComparer.Ordinal);
                foreach (var syntax in TestSets(root))
                {
                    results[syntax] = En16931Runner.Run(
                        SchemaPath(root, syntax),
                        LoadCases(root, syntax),
                        cancellationToken
                    );
                }

                s_results = results;
            }

            return s_results;
        }
        finally
        {
            _ = Gate.Release();
        }
    }
}
