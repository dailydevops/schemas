namespace NetEvolve.Schemas.Xml.Tests.Integration.XmlConf;

using NetEvolve.Schemas.Conformance.Baseline;
using NetEvolve.Schemas.Conformance.Fetch;
using NetEvolve.Schemas.Conformance.Manifest;

/// <summary>The pinned W3C XML Conformance Test Suite: manifest, fetch, selection, and baseline location.</summary>
internal static class XmlConfSuite
{
    private static readonly HttpClient Client = new() { Timeout = TimeSpan.FromMinutes(10) };

    private static readonly SemaphoreSlim Gate = new(1, 1);
    private static XmlConfSelection? s_selection;
    private static string? s_root;

    /// <summary>Gets the directory of the fetched suite; set once <see cref="LoadSelectionAsync"/> has run.</summary>
    public static string SuiteRoot => s_root ?? throw new InvalidOperationException("The suite is not loaded.");

    public static string RepositoryRoot { get; } = FindRepositoryRoot();

    public static string BaselineDirectory { get; } =
        Path.Combine(RepositoryRoot, "tests", "conformance", "baselines", "xmlconf");

    /// <summary>Fetches the pinned suite (from the cache when present) and applies the filter; loaded once per process.</summary>
    /// <param name="cancellationToken">A token to cancel the fetch.</param>
    /// <returns>The selection.</returns>
    public static async Task<XmlConfSelection> LoadSelectionAsync(CancellationToken cancellationToken)
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

    /// <summary>Loads the baselines of the selected test sets.</summary>
    /// <param name="selection">The selection.</param>
    /// <returns>The baseline of each selected test set by test set name.</returns>
    public static Dictionary<string, KnownFailureBaseline> LoadBaselines(XmlConfSelection selection)
    {
        var baselines = new Dictionary<string, KnownFailureBaseline>(StringComparer.Ordinal);
        foreach (var testSet in selection.Selected.Select(c => c.TestSet).Distinct(StringComparer.Ordinal))
        {
            baselines[testSet] = BaselineFiles.Load(BaselineDirectory, testSet);
        }

        return baselines;
    }

    private static async Task<XmlConfSelection> LoadAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var path = Path.Combine(RepositoryRoot, "tests", "conformance", "suites", "xmlconf.json");
        var manifest = SuiteManifestLoader.Parse(await File.ReadAllTextAsync(path, cancellationToken), path);
        var root = await new SuiteFetcher(new HttpSuiteTransport(Client), ConformanceCache.ResolveRoot()).FetchAsync(
            manifest,
            cancellationToken
        );
        s_root = root;
        return XmlConfFilter.Select(XmlConfCatalogReader.Read(root));
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
