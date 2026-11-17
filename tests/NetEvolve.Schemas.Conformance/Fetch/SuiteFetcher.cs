namespace NetEvolve.Schemas.Conformance.Fetch;

using System.Security.Cryptography;
using NetEvolve.Schemas.Conformance.Manifest;

/// <summary>Downloads, verifies, extracts, and caches pinned suite archives; the cache is keyed by checksum.</summary>
public sealed class SuiteFetcher
{
    private const string TempPrefix = ".tmp-";

    private readonly ISuiteTransport _transport;
    private readonly string _cacheRoot;

    /// <summary>Initializes a new instance of the <see cref="SuiteFetcher"/> class.</summary>
    /// <param name="transport">The download transport.</param>
    /// <param name="cacheRoot">The cache folder; see <see cref="ConformanceCache"/>.</param>
    public SuiteFetcher(ISuiteTransport transport, string cacheRoot)
    {
        ArgumentNullException.ThrowIfNull(transport);
        ArgumentException.ThrowIfNullOrWhiteSpace(cacheRoot);
        _transport = transport;
        _cacheRoot = Path.GetFullPath(cacheRoot);
    }

    /// <summary>Returns the extracted suite directory, downloading it only when the cache misses.</summary>
    /// <param name="manifest">The pinned suite.</param>
    /// <param name="cancellationToken">A token to cancel the fetch.</param>
    /// <returns>The suite root directory (the manifest sub-path applied).</returns>
    /// <exception cref="SuiteFetchException">The checksum differs, the archive is unsafe, or the sub-path is missing.</exception>
    public async Task<string> FetchAsync(SuiteManifest manifest, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        cancellationToken.ThrowIfCancellationRequested();

        var entry = Path.Combine(_cacheRoot, manifest.Sha256);
        if (!Directory.Exists(entry))
        {
            await DownloadAsync(manifest, entry, cancellationToken).ConfigureAwait(false);
        }
        return ApplySubPath(manifest, entry);
    }

    private async Task DownloadAsync(SuiteManifest manifest, string entry, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Directory.CreateDirectory(_cacheRoot);
        var temp = Path.Combine(_cacheRoot, TempPrefix + Guid.NewGuid().ToString("N"));
        var archivePath = temp + ".archive";
        try
        {
            using (var source = await _transport.OpenAsync(manifest.SourceUrl, cancellationToken).ConfigureAwait(false))
            using (var file = File.Create(archivePath))
            {
                await source.CopyToAsync(file, cancellationToken).ConfigureAwait(false);
            }

            using (var file = File.OpenRead(archivePath))
            {
                var actual = Convert.ToHexStringLower(
                    await SHA256.HashDataAsync(file, cancellationToken).ConfigureAwait(false)
                );
                if (!string.Equals(actual, manifest.Sha256, StringComparison.Ordinal))
                {
                    throw new SuiteFetchException(
                        $"Suite '{manifest.Key}': checksum mismatch, expected {manifest.Sha256} but downloaded {actual}."
                    );
                }
            }

            try
            {
                using var file = File.OpenRead(archivePath);
                ArchiveExtractor.Extract(file, manifest.ArchiveType, temp);
            }
            catch (InvalidDataException ex)
            {
                throw new SuiteFetchException($"Suite '{manifest.Key}': unsafe or invalid archive: {ex.Message}", ex);
            }

            try
            {
                Directory.Move(temp, entry);
            }
            catch (IOException) when (Directory.Exists(entry))
            {
                // Another process filled the same checksum entry first; its content is identical.
            }
        }
        finally
        {
            File.Delete(archivePath);
            if (Directory.Exists(temp))
            {
                Directory.Delete(temp, recursive: true);
            }
        }
    }

    private static string ApplySubPath(SuiteManifest manifest, string entry)
    {
        if (manifest.SubPath is null)
        {
            return entry;
        }

        var path = Path.GetFullPath(Path.Combine(entry, manifest.SubPath));
        if (!Directory.Exists(path))
        {
            throw new SuiteFetchException(
                $"Suite '{manifest.Key}': sub-path '{manifest.SubPath}' not found in archive."
            );
        }
        return path;
    }
}
