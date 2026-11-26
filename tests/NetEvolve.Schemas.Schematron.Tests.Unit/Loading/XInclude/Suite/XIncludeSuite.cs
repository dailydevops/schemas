namespace NetEvolve.Schemas.Schematron.Tests.Unit.Loading.XInclude.Suite;

using NetEvolve.Schemas.Conformance.Fetch;
using NetEvolve.Schemas.Schematron.Tests.Unit.SuitePins;

/// <summary>The pinned W3C XInclude test suite: fetch and the baseline location.</summary>
internal static class XIncludeSuite
{
    private static readonly HttpClient Client = new() { Timeout = TimeSpan.FromMinutes(10) };

    public static string BaselineDirectory { get; } =
        Path.Combine(SuitePinFiles.RepositoryRoot, "tests", "conformance", "baselines", "xinclude");

    /// <summary>Fetches the pinned suite, from the cache when present.</summary>
    /// <param name="cancellationToken">A token to cancel the fetch.</param>
    /// <returns>The suite root.</returns>
    public static Task<string> FetchAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return new SuiteFetcher(new HttpSuiteTransport(Client), ConformanceCache.ResolveRoot()).FetchAsync(
            SuitePinFiles.Load("xinclude.json"),
            cancellationToken
        );
    }
}
