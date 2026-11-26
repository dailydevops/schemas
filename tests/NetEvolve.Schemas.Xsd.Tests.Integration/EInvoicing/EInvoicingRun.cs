namespace NetEvolve.Schemas.Xsd.Tests.Integration.EInvoicing;

using NetEvolve.Schemas.Conformance.Fetch;
using NetEvolve.Schemas.Conformance.Manifest;
using NetEvolve.Schemas.Conformance.Results;
using NetEvolve.Schemas.Xsd.Tests.Integration.Xsts;

/// <summary>Fetches the pinned archives and runs the schema sets once per process.</summary>
internal static class EInvoicingRun
{
    /// <summary>The package name of the rate rows in the job summary.</summary>
    public const string RatePackage = "NetEvolve.Schemas.Xsd (e-invoicing schema sets)";

    private const string RecordFile = "xsd-einvoicing.json";

    private static readonly HttpClient Client = new() { Timeout = TimeSpan.FromMinutes(10) };
    private static readonly SemaphoreSlim Gate = new(1, 1);
    private static IReadOnlyDictionary<string, string>? s_roots;
    private static IReadOnlyList<CaseResult>? s_results;

    /// <summary>Gets the directory of the suite manifests.</summary>
    public static string SuitesDirectory { get; } =
        Path.Combine(XstsSuite.RepositoryRoot, "tests", "conformance", "suites");

    /// <summary>Reads the record.</summary>
    /// <returns>The record.</returns>
    public static EInvoicingManifest LoadManifest() =>
        EInvoicingManifest.Parse(File.ReadAllText(Path.Combine(SuitesDirectory, RecordFile)));

    /// <summary>Fetches the pinned archives by their checksums, or reuses the cache.</summary>
    /// <param name="cancellationToken">A token to cancel the fetch.</param>
    /// <returns>The suite roots by suite key.</returns>
    public static async Task<IReadOnlyDictionary<string, string>> GetRootsAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await Gate.WaitAsync(cancellationToken);
        try
        {
            return s_roots ??= await FetchAsync(cancellationToken);
        }
        finally
        {
            _ = Gate.Release();
        }
    }

    /// <summary>Gets the results of every set.</summary>
    /// <param name="cancellationToken">A token to cancel the run.</param>
    /// <returns>One result per set build and per document.</returns>
    public static async Task<IReadOnlyList<CaseResult>> GetResultsAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var roots = await GetRootsAsync(cancellationToken);
        await Gate.WaitAsync(cancellationToken);
        try
        {
            return s_results ??= await EInvoicingRunner.RunAsync(LoadManifest(), key => roots[key], cancellationToken);
        }
        finally
        {
            _ = Gate.Release();
        }
    }

    private static async Task<IReadOnlyDictionary<string, string>> FetchAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var manifest = LoadManifest();
        var keys = manifest
            .Sets.SelectMany(s => s.Documents.Select(d => d.Suite).Append(s.SchemaSuite))
            .Distinct(StringComparer.Ordinal);
        var manifests = SuiteManifestLoader.LoadDirectory(SuitesDirectory);
        var fetcher = new SuiteFetcher(new HttpSuiteTransport(Client), ConformanceCache.ResolveRoot());
        var roots = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var key in keys)
        {
            roots[key] = await fetcher.FetchAsync(
                manifests.Single(m => string.Equals(m.Key, key, StringComparison.Ordinal)),
                cancellationToken
            );
        }

        return roots;
    }
}
