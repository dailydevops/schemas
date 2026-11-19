namespace NetEvolve.Schemas.XPath.Tests.Unit.Functions.Strings;

using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using NetEvolve.Schemas.Conformance.Fetch;
using NetEvolve.Schemas.Conformance.Manifest;

/// <summary>The pinned Unicode Character Database of <c>tests/conformance/suites/ucd.json</c>: fetched by checksum and cached, as the table tests of the Xml package do.</summary>
internal sealed class StringsUcd
{
    private static readonly SemaphoreSlim Gate = new(1, 1);
    private static StringsUcd? _cached;

    private StringsUcd(SuiteManifest manifest, string root)
    {
        Manifest = manifest;
        Root = root;
    }

    /// <summary>Gets the manifest of the pinned suite.</summary>
    internal SuiteManifest Manifest { get; }

    /// <summary>Gets the folder that holds the extracted files.</summary>
    internal string Root { get; }

    /// <summary>Gets the version of the pinned release, for example <c>18.0.0</c>.</summary>
    internal string Version => Manifest.SourceUrl.Segments[^3].TrimEnd('/');

    internal static async Task<StringsUcd> GetAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await Gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return _cached ??= await LoadAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _ = Gate.Release();
        }
    }

    internal static string RepositoryRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Schemas.slnx")))
            {
                return dir.FullName;
            }
        }

        throw new DirectoryNotFoundException("Repository root not found.");
    }

    /// <summary>Reads the lines of a file of the database.</summary>
    internal Task<string[]> ReadLinesAsync(string file, CancellationToken cancellationToken) =>
        File.ReadAllLinesAsync(Path.Combine(Root, file), cancellationToken);

    private static async Task<StringsUcd> LoadAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var path = Path.Combine(RepositoryRoot(), "tests", "conformance", "suites", "ucd.json");
        var manifest = SuiteManifestLoader.Parse(
            await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false),
            path
        );
        using var http = new HttpClient();
        var fetcher = new SuiteFetcher(new HttpSuiteTransport(http), ConformanceCache.ResolveRoot());
        var root = await fetcher.FetchAsync(manifest, cancellationToken).ConfigureAwait(false);

        return new StringsUcd(manifest, root);
    }
}
