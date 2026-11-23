namespace NetEvolve.Schemas.Xsd.Tests.Integration.Xsts;

using NetEvolve.Schemas.Conformance.Fetch;
using NetEvolve.Schemas.Conformance.Manifest;

/// <summary>The pinned W3C XML Schema Test Suite: manifest, fetch, and baseline location.</summary>
internal static class XstsSuite
{
    private static readonly HttpClient Client = new() { Timeout = TimeSpan.FromMinutes(10) };

    public static string RepositoryRoot { get; } = FindRepositoryRoot();

    public static string BaselineDirectory { get; } =
        Path.Combine(RepositoryRoot, "tests", "conformance", "baselines", "xsts");

    /// <summary>The fetch and the selection run once per process, started when the class is first used.</summary>
    private static readonly Task<(string Root, XstsSelection Selection)> Shared = Task.Run(() =>
        FetchAndSelectAsync(CancellationToken.None)
    );

    /// <summary>Fetches the pinned suite (from the cache when present) and selects the cases of XSD 1.0 and 1.1.</summary>
    /// <param name="cancellationToken">A token to cancel the wait.</param>
    /// <returns>The selection of both versions; the work is done once per process.</returns>
    public static async Task<XstsSelection> LoadSelectionAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return (await Shared.WaitAsync(cancellationToken)).Selection;
    }

    /// <summary>Fetches the pinned suite (from the cache when present) and gives the directory that holds it.</summary>
    /// <param name="cancellationToken">A token to cancel the wait.</param>
    /// <returns>The suite root; the paths of the schema and instance documents of a case are relative to it.</returns>
    public static async Task<string> LoadRootAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return (await Shared.WaitAsync(cancellationToken)).Root;
    }

    private static async Task<(string Root, XstsSelection Selection)> FetchAndSelectAsync(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var path = Path.Combine(RepositoryRoot, "tests", "conformance", "suites", "xsts.json");
        var manifest = SuiteManifestLoader.Parse(await File.ReadAllTextAsync(path, cancellationToken), path);
        var root = await new SuiteFetcher(new HttpSuiteTransport(Client), ConformanceCache.ResolveRoot()).FetchAsync(
            manifest,
            cancellationToken
        );
        var cases = XstsReader.Read(root);
        var v10 = XstsSelector.Select(cases, new XstsFilter(XstsVersions.V10, XstsFilter.AcceptedStatuses));
        var v11 = XstsSelector.Select(cases, new XstsFilter(XstsVersions.V11, XstsFilter.AcceptedStatuses));
        return (root, new XstsSelection([.. v10.Selected, .. v11.Selected], [.. v10.Excluded, .. v11.Excluded]));
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
