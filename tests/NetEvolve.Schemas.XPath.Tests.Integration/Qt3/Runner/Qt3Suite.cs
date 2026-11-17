namespace NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Runner;

using NetEvolve.Schemas.Conformance.Baseline;
using NetEvolve.Schemas.Conformance.Fetch;
using NetEvolve.Schemas.Conformance.Manifest;
using NetEvolve.Schemas.Conformance.Results;

/// <summary>The pinned QT3 suite: manifest, fetched catalog, selection, and baselines.</summary>
internal static class Qt3Suite
{
    private static readonly SemaphoreSlim Gate = new(1, 1);
    private static Qt3Selection? s_selection;

    public static string RepositoryRoot { get; } = FindRepositoryRoot();

    public static string BaselineDirectory { get; } =
        Path.Combine(RepositoryRoot, "tests", "conformance", "baselines", "qt3");

    public static SuiteManifest Manifest { get; } = LoadManifest();

    /// <summary>Gets the fetched suite's selection; the archive is downloaded once and cached by checksum.</summary>
    /// <param name="cancellationToken">A token to cancel the fetch.</param>
    /// <returns>The selection.</returns>
    public static async Task<Qt3Selection> GetSelectionAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await Gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return s_selection ??= await LoadSelectionAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _ = Gate.Release();
        }
    }

    /// <summary>Loads the baseline of every test set that has a file, keyed by test set name.</summary>
    /// <returns>The baselines.</returns>
    public static Dictionary<string, KnownFailureBaseline> LoadBaselines()
    {
        var baselines = new Dictionary<string, KnownFailureBaseline>(StringComparer.Ordinal);
        if (Directory.Exists(BaselineDirectory))
        {
            foreach (var file in Directory.GetFiles(BaselineDirectory, "*.txt"))
            {
                var name = Path.GetFileNameWithoutExtension(file);
                baselines.Add(name, BaselineFiles.Load(BaselineDirectory, name));
            }
        }
        return baselines;
    }

    /// <summary>
    /// Rewrites the baseline files: one file per test set with selected cases, none for any other set, each listing
    /// the cases that do not pass with the engine.
    /// </summary>
    /// <param name="selection">The selection to record.</param>
    /// <param name="engine">The engine that decides which cases pass.</param>
    public static void RegenerateBaselines(Qt3Selection selection, IQt3Engine engine)
    {
        if (Directory.Exists(BaselineDirectory))
        {
            foreach (var file in Directory.GetFiles(BaselineDirectory, "*.txt"))
            {
                File.Delete(file);
            }
        }
        foreach (var testSet in selection.SelectedTestSets)
        {
            var ids = new List<string>();
            foreach (var testCase in selection.SelectedIn(testSet))
            {
                if (Qt3Runner.Run(testCase, engine).Outcome != CaseOutcome.Pass)
                {
                    ids.Add(testCase.Id);
                }
            }
            BaselineFiles.Save(BaselineDirectory, testSet, ids);
        }
    }

    private static SuiteManifest LoadManifest()
    {
        var path = Path.Combine(RepositoryRoot, "tests", "conformance", "suites", "qt3.json");
        return SuiteManifestLoader.Parse(File.ReadAllText(path), path);
    }

    private static async Task<Qt3Selection> LoadSelectionAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var client = new HttpClient();
        var fetcher = new SuiteFetcher(new HttpSuiteTransport(client), ConformanceCache.ResolveRoot());
        var root = await fetcher.FetchAsync(Manifest, cancellationToken).ConfigureAwait(false);
        return Qt3Selector.Select(Qt3CatalogReader.Read(root));
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
