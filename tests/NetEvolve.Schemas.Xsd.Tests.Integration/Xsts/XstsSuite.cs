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

    /// <summary>The fetch and selection run once per process, started when the class is first used.</summary>
    private static readonly Task<XstsSelection> Shared = Task.Run(() => FetchAndSelectAsync(CancellationToken.None));

    /// <summary>Fetches the pinned suite (from the cache when present) and selects the cases of XSD 1.0 and 1.1.</summary>
    /// <param name="cancellationToken">A token to cancel the wait.</param>
    /// <returns>The selection of both versions; the work is done once per process.</returns>
    public static Task<XstsSelection> LoadSelectionAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Shared.WaitAsync(cancellationToken);
    }

    private static async Task<XstsSelection> FetchAndSelectAsync(CancellationToken cancellationToken)
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
        return new XstsSelection([.. v10.Selected, .. v11.Selected], [.. v10.Excluded, .. v11.Excluded]);
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
