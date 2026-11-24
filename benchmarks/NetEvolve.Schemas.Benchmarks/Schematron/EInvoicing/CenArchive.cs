namespace NetEvolve.Schemas.Benchmarks.Schematron.EInvoicing;

using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;

/// <summary>
/// Fetches the pinned CEN EN 16931 validation archive for the benchmarks. The pin (URL, SHA-256, sub-path) comes from
/// <c>tests/conformance/suites/cen-en16931.json</c>; the archive is verified by its checksum and extracted into the
/// git-ignored conformance cache (the same folder and layout the golden tests use), so no CEN artifact is ever tracked.
/// </summary>
internal static class CenArchive
{
    private const string Manifest = "tests/conformance/suites/cen-en16931.json";
    private const string CacheVariable = "SCHEMAS_CONFORMANCE_CACHE";

    /// <summary>Fetches the archive, or reuses the cache.</summary>
    /// <returns>The directory that holds the <c>ubl</c> and <c>cii</c> folders.</returns>
    public static string Fetch()
    {
        var repository = RepositoryRoot();
        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(repository, Manifest)));
        var url = manifest.RootElement.GetProperty("url").GetString()!;
        var sha256 = manifest.RootElement.GetProperty("sha256").GetString()!;
        var subPath = manifest.RootElement.GetProperty("subPath").GetString()!;
        var cacheVariable = Environment.GetEnvironmentVariable(CacheVariable);
        var cache = string.IsNullOrWhiteSpace(cacheVariable)
            ? Path.Combine(repository, "artifacts", "conformance-cache")
            : cacheVariable;
        var entry = Path.Combine(cache, sha256);
        if (!Directory.Exists(entry))
        {
            _ = Directory.CreateDirectory(cache);
            var temp = Path.Combine(cache, ".tmp-" + Guid.NewGuid().ToString("N"));
            var archive = temp + ".archive";
            try
            {
                using (var client = new HttpClient { Timeout = TimeSpan.FromMinutes(10) })
                using (var request = new HttpRequestMessage(HttpMethod.Get, new Uri(url)))
                using (var response = client.Send(request))
                using (var source = response.EnsureSuccessStatusCode().Content.ReadAsStream())
                using (var file = File.Create(archive))
                {
                    source.CopyTo(file);
                }

                using (var file = File.OpenRead(archive))
                {
                    var actual = Convert.ToHexStringLower(SHA256.HashData(file));
                    if (!string.Equals(actual, sha256, StringComparison.Ordinal))
                    {
                        throw new InvalidDataException(
                            $"CEN archive checksum mismatch: expected {sha256}, got {actual}."
                        );
                    }
                }

                ZipFile.ExtractToDirectory(archive, temp);
                if (!Directory.Exists(entry))
                {
                    Directory.Move(temp, entry);
                }
            }
            finally
            {
                File.Delete(archive);
                if (Directory.Exists(temp))
                {
                    Directory.Delete(temp, recursive: true);
                }
            }
        }

        return Path.Combine(entry, subPath);
    }

    private static string RepositoryRoot()
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
