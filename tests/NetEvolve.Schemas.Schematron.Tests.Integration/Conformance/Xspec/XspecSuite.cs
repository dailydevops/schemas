namespace NetEvolve.Schemas.Schematron.Tests.Integration.Conformance.Xspec;

using NetEvolve.Schemas.Conformance.Fetch;
using NetEvolve.Schemas.Conformance.Manifest;
using NetEvolve.Schemas.Schematron.Tests.Integration.Conformance.Runner;

/// <summary>The pinned XSpec project: manifest, fetch, selection, and the run of its Schematron scenarios.</summary>
internal static class XspecSuite
{
    private static readonly HttpClient Client = new() { Timeout = TimeSpan.FromMinutes(10) };

    private static readonly SemaphoreSlim Gate = new(1, 1);
    private static XspecRun? s_run;

    public static string BaselineDirectory { get; } =
        Path.Combine(SchematronSuite.RepositoryRoot, "tests", "conformance", "baselines", "xspec");

    public static string LinksPath { get; } =
        Path.Combine(
            SchematronSuite.RepositoryRoot,
            "tests",
            "NetEvolve.Schemas.Schematron.Tests.Integration",
            "Conformance",
            "Xspec",
            "known-failure-links.txt"
        );

    /// <summary>Fetches the pinned project (from the cache when present), reads its Schematron scenarios, and runs them; done once per process.</summary>
    /// <param name="cancellationToken">A token to cancel the fetch and the run.</param>
    /// <returns>The selection and one result per expectation and excluded part.</returns>
    public static async Task<XspecRun> RunAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await Gate.WaitAsync(cancellationToken);
        try
        {
            return s_run ??= await LoadAsync(cancellationToken);
        }
        finally
        {
            _ = Gate.Release();
        }
    }

    private static async Task<XspecRun> LoadAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var path = Path.Combine(SchematronSuite.RepositoryRoot, "tests", "conformance", "suites", "xspec.json");
        var manifest = SuiteManifestLoader.Parse(await File.ReadAllTextAsync(path, cancellationToken), path);
        var root = await new SuiteFetcher(new HttpSuiteTransport(Client), ConformanceCache.ResolveRoot()).FetchAsync(
            manifest,
            cancellationToken
        );
        var selection = XspecReader.Read(root);
        return new XspecRun(root, selection, XspecEngine.Run(selection, root, cancellationToken));
    }
}
