namespace NetEvolve.Schemas.Schematron.Tests.Integration.Goldens.Harness;

using NetEvolve.Schemas.Conformance.Fetch;
using NetEvolve.Schemas.Conformance.Manifest;

/// <summary>The only way to reach golden documents: pinned suites fetched through <see cref="SuiteFetcher"/>.</summary>
/// <remarks>
/// A source names a suite key, and the key must have a manifest with a SHA-256 checksum. There is no overload that
/// takes a URL, so an unpinned download cannot be expressed.
/// </remarks>
internal sealed class GoldenCorpus
{
    private const int Sha256HexLength = 64;

    private readonly Dictionary<string, SuiteManifest> _manifests = new(StringComparer.Ordinal);
    private readonly SuiteFetcher _fetcher;

    /// <summary>Initializes a new instance of the <see cref="GoldenCorpus"/> class.</summary>
    /// <param name="manifests">The suite manifests, for example from <see cref="SuiteManifestLoader.LoadDirectory"/>.</param>
    /// <param name="fetcher">The fetcher that downloads, verifies, and caches the pinned archives.</param>
    /// <exception cref="GoldenSourceException">A manifest has no well-formed lowercase SHA-256 checksum, or a suite key repeats.</exception>
    public GoldenCorpus(IReadOnlyList<SuiteManifest> manifests, SuiteFetcher fetcher)
    {
        ArgumentNullException.ThrowIfNull(manifests);
        ArgumentNullException.ThrowIfNull(fetcher);
        foreach (var manifest in manifests)
        {
            ArgumentNullException.ThrowIfNull(manifest);
            ArgumentNullException.ThrowIfNullOrWhiteSpace(manifest.Key);
            if (!IsSha256(manifest.Sha256))
            {
                throw new GoldenSourceException(
                    $"Suite '{manifest.Key}' has no pinned SHA-256 checksum; golden documents are fetched only through a checksum-pinned manifest."
                );
            }

            if (!_manifests.TryAdd(manifest.Key, manifest))
            {
                throw new GoldenSourceException($"Suite '{manifest.Key}' is pinned by more than one manifest.");
            }
        }

        _fetcher = fetcher;
    }

    /// <summary>Checks, without any download, that the source belongs to a pinned suite and names a path inside it.</summary>
    /// <param name="source">The source.</param>
    /// <exception cref="GoldenSourceException">The suite has no manifest, or the path is empty, rooted, or leaves the suite.</exception>
    public void EnsurePinned(GoldenDocumentSource source)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNullOrWhiteSpace(source.SuiteKey);
        if (!_manifests.ContainsKey(source.SuiteKey))
        {
            throw new GoldenSourceException(
                $"Suite '{source.SuiteKey}' has no manifest with a pinned SHA-256 checksum; golden documents are fetched only through the suite manifest."
            );
        }

        if (string.IsNullOrWhiteSpace(source.Path) || Path.IsPathRooted(source.Path) || LeavesSuite(source.Path))
        {
            throw new GoldenSourceException(
                $"Golden document path '{source.Path}' of suite '{source.SuiteKey}' must be a relative path inside the suite."
            );
        }
    }

    /// <summary>Fetches the suite of the source (from the cache when present) and locates the document.</summary>
    /// <param name="source">The source.</param>
    /// <param name="cancellationToken">A token to cancel the fetch.</param>
    /// <returns>The document with its extracted path.</returns>
    /// <exception cref="GoldenSourceException">The source is not pinned or the file does not exist in the suite.</exception>
    /// <exception cref="SuiteFetchException">The checksum differs, the archive is unsafe, or the sub-path is missing.</exception>
    public async Task<GoldenDocument> ResolveAsync(GoldenDocumentSource source, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(source);
        EnsurePinned(source);
        var root = await _fetcher.FetchAsync(_manifests[source.SuiteKey], cancellationToken);
        var path = Path.GetFullPath(Path.Combine(root, source.Path));
        if (!File.Exists(path))
        {
            throw new GoldenSourceException(
                $"Golden document '{source.Path}' does not exist in suite '{source.SuiteKey}'."
            );
        }

        return new GoldenDocument(source.Id, path);
    }

    private static bool IsSha256(string? value)
    {
        if (value is null || value.Length != Sha256HexLength)
        {
            return false;
        }

        foreach (var c in value)
        {
            if (!c.IsAsciiDigit() && !c.IsAsciiHexDigitLower())
            {
                return false;
            }
        }

        return true;
    }

    private static bool LeavesSuite(string path)
    {
        foreach (var segment in path.Split('/', '\\'))
        {
            if (string.Equals(segment, "..", StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
