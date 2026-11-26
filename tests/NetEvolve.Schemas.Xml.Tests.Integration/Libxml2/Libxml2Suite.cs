namespace NetEvolve.Schemas.Xml.Tests.Integration.Libxml2;

using NetEvolve.Schemas.Conformance.Fetch;
using NetEvolve.Schemas.Conformance.Manifest;

/// <summary>The pinned libxml2 source archive: fetch and the baseline locations.</summary>
internal static class Libxml2Suite
{
    private static readonly HttpClient Client = new() { Timeout = TimeSpan.FromMinutes(10) };

    public static string RepositoryRoot { get; } = FindRepositoryRoot();

    /// <summary>Gets the baseline directory of a sub-tree, for example <c>libxml2-regexp</c>.</summary>
    /// <param name="name">The directory name below <c>tests/conformance/baselines</c>.</param>
    /// <returns>The directory.</returns>
    public static string BaselineDirectory(string name) =>
        Path.Combine(RepositoryRoot, "tests", "conformance", "baselines", name);

    /// <summary>Fetches the pinned archive, from the cache when present.</summary>
    /// <param name="cancellationToken">A token to cancel the fetch.</param>
    /// <returns>The root of the libxml2 source tree.</returns>
    public static async Task<string> FetchAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var path = Path.Combine(RepositoryRoot, "tests", "conformance", "suites", "libxml2.json");
        var manifest = SuiteManifestLoader.Parse(await File.ReadAllTextAsync(path, cancellationToken), path);
        return await new SuiteFetcher(new HttpSuiteTransport(Client), ConformanceCache.ResolveRoot()).FetchAsync(
            manifest,
            cancellationToken
        );
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
