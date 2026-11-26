namespace NetEvolve.Schemas.Schematron.Tests.Integration.Goldens.Stage3Documents;

using NetEvolve.Schemas.Conformance.Fetch;
using NetEvolve.Schemas.Conformance.Manifest;
using NetEvolve.Schemas.Conformance.Results;
using NetEvolve.Schemas.Resources.FileSystem;
using NetEvolve.Schemas.Schematron.Tests.Integration.Conformance.Runner;

/// <summary>Fetches the pinned stage 3 suite and validates the sample invoices once per process.</summary>
internal static class Stage3Run
{
    /// <summary>The package name of the rate rows in the job summary; it differs from the conformance suite row on purpose.</summary>
    public const string RatePackage = "NetEvolve.Schemas.Schematron (stage 3 goldens)";

    private const string RecordFile = "schematron-stage3.json";

    private static readonly HttpClient Client = new() { Timeout = TimeSpan.FromMinutes(10) };
    private static readonly SemaphoreSlim Gate = new(1, 1);
    private static IReadOnlyList<CaseResult>? s_results;
    private static string? s_root;

    /// <summary>Gets the directory of the suite manifests.</summary>
    public static string SuitesDirectory { get; } =
        Path.Combine(SchematronSuite.RepositoryRoot, "tests", "conformance", "suites");

    /// <summary>Reads the stage record.</summary>
    /// <returns>The record.</returns>
    public static Stage3Manifest LoadManifest() =>
        Stage3Manifest.Parse(File.ReadAllText(Path.Combine(SuitesDirectory, RecordFile)));

    /// <summary>Fetches the pinned suite by its checksum, or reuses the cache.</summary>
    /// <param name="cancellationToken">A token to cancel the fetch.</param>
    /// <returns>The suite root.</returns>
    public static async Task<string> GetRootAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await Gate.WaitAsync(cancellationToken);
        try
        {
            var suite = LoadManifest().Suite;
            return s_root ??= await new SuiteFetcher(
                new HttpSuiteTransport(Client),
                ConformanceCache.ResolveRoot()
            ).FetchAsync(
                SuiteManifestLoader
                    .LoadDirectory(SuitesDirectory)
                    .Single(m => string.Equals(m.Key, suite, StringComparison.Ordinal)),
                cancellationToken
            );
        }
        finally
        {
            _ = Gate.Release();
        }
    }

    /// <summary>Gets the results of the sample invoices under the sandboxed file resolver and of the default resolver cases.</summary>
    /// <param name="cancellationToken">A token to cancel the run.</param>
    /// <returns>One result per invoice.</returns>
    public static async Task<IReadOnlyList<CaseResult>> GetResultsAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var root = await GetRootAsync(cancellationToken);
        await Gate.WaitAsync(cancellationToken);
        try
        {
            if (s_results is null)
            {
                var manifest = LoadManifest();
                s_results =
                [
                    .. Stage3Runner.Run(
                        manifest,
                        root,
                        directory => new SandboxedFileSystemResourceResolver(directory),
                        cancellationToken
                    ),
                    .. Stage3Runner.RunDefaultResolver(manifest, root, cancellationToken),
                ];
            }

            return s_results;
        }
        finally
        {
            _ = Gate.Release();
        }
    }
}
