namespace NetEvolve.Schemas.Schematron.Tests.Integration.Conformance.Runner;

using NetEvolve.Schemas.Conformance.Fetch;
using NetEvolve.Schemas.Conformance.Manifest;

/// <summary>The pinned Schematron conformance suite: manifest, fetch, and baseline location.</summary>
internal static class SchematronSuite
{
    private static readonly HttpClient Client = new() { Timeout = TimeSpan.FromMinutes(10) };

    private static readonly SemaphoreSlim Gate = new(1, 1);
    private static CaseSelection? s_selection;

    public static string RepositoryRoot { get; } = FindRepositoryRoot();

    public static string BaselineDirectory { get; } =
        Path.Combine(RepositoryRoot, "tests", "conformance", "baselines", "schematron-conformance");

    /// <summary>Fetches the pinned suite (from the cache when present) and applies the binding selection; loaded once per process.</summary>
    /// <param name="cancellationToken">A token to cancel the fetch.</param>
    /// <returns>The selection.</returns>
    public static async Task<CaseSelection> LoadSelectionAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await Gate.WaitAsync(cancellationToken);
        try
        {
            return s_selection ??= await LoadAsync(cancellationToken);
        }
        finally
        {
            _ = Gate.Release();
        }
    }

    private static async Task<CaseSelection> LoadAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var path = Path.Combine(RepositoryRoot, "tests", "conformance", "suites", "schematron-conformance.json");
        var manifest = SuiteManifestLoader.Parse(await File.ReadAllTextAsync(path, cancellationToken), path);
        var root = await new SuiteFetcher(new HttpSuiteTransport(Client), ConformanceCache.ResolveRoot()).FetchAsync(
            manifest,
            cancellationToken
        );
        return BindingFilter.Select(SchematronCatalogReader.Read(root, SchematronBindings.Iso), SchematronBindings.Iso);
    }

    private static string FindRepositoryRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Schemas.slnx")))
            {
                return dir.FullName;
            }
        }

        throw new DirectoryNotFoundException("Repository root (Schemas.slnx) not found.");
    }
}
