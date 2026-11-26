namespace NetEvolve.Schemas.Schematron.Tests.Integration.Goldens.En16931;

using NetEvolve.Schemas.Conformance.Fetch;
using NetEvolve.Schemas.Conformance.Manifest;
using NetEvolve.Schemas.Conformance.Results;
using NetEvolve.Schemas.Schematron.Tests.Integration.Conformance.Runner;

/// <summary>Fetches the pinned CEN EN 16931 archive and runs the corpus once per process; every test reads its results from this run.</summary>
internal static class En16931Run
{
    /// <summary>The key of the suite manifest.</summary>
    public const string SuiteKey = "cen-en16931";

    /// <summary>The package name of the rate rows in the job summary.</summary>
    public const string RatePackage = "NetEvolve.Schemas.Schematron (EN 16931 goldens)";

    /// <summary>The syntaxes of the corpus, which are also the test sets of the results.</summary>
    public static readonly string[] Syntaxes = ["ubl", "cii"];

    private static readonly HttpClient Client = new() { Timeout = TimeSpan.FromMinutes(10) };

    // Two UBL examples carry the extension .XML; match the casing the same way on every platform.
    private static readonly EnumerationOptions XmlFiles = new() { MatchCasing = MatchCasing.CaseInsensitive };
    private static readonly SemaphoreSlim Gate = new(1, 1);
    private static Dictionary<string, IReadOnlyList<CaseResult>>? s_results;
    private static string? s_root;

    /// <summary>Gets the directory of the suite manifests.</summary>
    public static string SuitesDirectory { get; } =
        Path.Combine(SchematronSuite.RepositoryRoot, "tests", "conformance", "suites");

    /// <summary>Reads the manifest that pins the archive.</summary>
    /// <returns>The manifest.</returns>
    public static SuiteManifest LoadManifest() =>
        SuiteManifestLoader
            .LoadDirectory(SuitesDirectory)
            .Single(m => string.Equals(m.Key, SuiteKey, StringComparison.Ordinal));

    /// <summary>Gets the full path of the validation schema of a syntax.</summary>
    /// <param name="root">The suite root.</param>
    /// <param name="syntax">The syntax.</param>
    /// <returns>The path of the schema that includes the abstract patterns, the binding files, and the code lists.</returns>
    public static string SchemaPath(string root, string syntax) =>
        Path.Combine(root, syntax, "schematron", $"EN16931-{syntax.ToUpperInvariant()}-validation.sch");

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

    /// <summary>Reads the cases of a syntax: the unit-test corpus files and the official examples.</summary>
    /// <param name="root">The suite root.</param>
    /// <param name="syntax">The syntax.</param>
    /// <returns>The cases in a stable order.</returns>
    public static IReadOnlyList<En16931Case> LoadCases(string root, string syntax)
    {
        var cases = new List<En16931Case>();
        var unitDirectories = syntax == "ubl" ? ["Invoice-unit-UBL", "CreditNote-unit-UBL"] : new[] { "cii" };
        foreach (var directory in unitDirectories)
        {
            var path = Path.Combine(root, "test", directory);
            foreach (
                var file in Directory.EnumerateFiles(path, "*.xml", XmlFiles).OrderBy(f => f, StringComparer.Ordinal)
            )
            {
                cases.AddRange(
                    En16931TestSet.Parse(syntax, $"{directory}/{Path.GetFileName(file)}", File.ReadAllText(file))
                );
            }
        }

        foreach (
            var file in Directory
                .EnumerateFiles(Path.Combine(root, syntax, "examples"), "*.xml", XmlFiles)
                .OrderBy(f => f, StringComparer.Ordinal)
        )
        {
            cases.Add(
                new En16931Case(
                    syntax,
                    $"examples/{Path.GetFileName(file)}",
                    File.ReadAllBytes(file),
                    [],
                    [],
                    isExample: true
                )
            );
        }

        return cases;
    }

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
                foreach (var syntax in Syntaxes)
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

    /// <summary>The test sets beyond the UBL and CII unit-test corpus, each with its own rate row.</summary>
    public static readonly string[] ExtendedTestSets = ["edifact", "ubl-testfiles"];

    /// <summary>Reads the cases of an extended test set.</summary>
    /// <param name="root">The suite root.</param>
    /// <param name="testSet">The test set, one of <see cref="ExtendedTestSets"/>.</param>
    /// <returns>The cases in a stable order.</returns>
    public static IReadOnlyList<En16931Case> LoadExtendedCases(string root, string testSet) =>
        string.IsNullOrEmpty(root + testSet) ? throw new ArgumentException("Unreachable.") : [];

    /// <summary>Gets the results of every extended test set.</summary>
    /// <param name="cancellationToken">A token to cancel the run.</param>
    /// <returns>The results by test set.</returns>
    public static Task<IReadOnlyDictionary<string, IReadOnlyList<CaseResult>>> GetExtendedResultsAsync(
        CancellationToken cancellationToken
    ) => throw new NotImplementedException();
}
