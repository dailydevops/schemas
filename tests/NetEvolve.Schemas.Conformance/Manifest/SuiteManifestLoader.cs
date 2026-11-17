namespace NetEvolve.Schemas.Conformance.Manifest;

using System.Text.Json;
using NetEvolve.Schemas.Conformance.Fetch;

/// <summary>Reads suite manifests (<c>tests/conformance/suites/&lt;suite&gt;.json</c>).</summary>
public static class SuiteManifestLoader
{
    private const int Sha256HexLength = 64;

    private static readonly string[] ManifestFields = ["key", "url", "sha256", "archive"];

    /// <summary>
    /// Loads every <c>*.json</c> manifest of a directory, one suite per file. A JSON object that declares none of
    /// <c>key</c>, <c>url</c>, <c>sha256</c> or <c>archive</c> is not a manifest (for example the stage record
    /// <c>stage-sources.json</c>) and is skipped; a file that declares any of them is validated in full.
    /// </summary>
    /// <param name="directory">The directory that holds the manifests.</param>
    /// <returns>The manifests, ordered by file name.</returns>
    /// <exception cref="SuiteManifestException">The directory is missing, a manifest is invalid, or a suite key repeats.</exception>
    public static IReadOnlyList<SuiteManifest> LoadDirectory(string directory)
    {
        ArgumentNullException.ThrowIfNull(directory);
        if (!Directory.Exists(directory))
        {
            throw new SuiteManifestException($"Manifest directory '{directory}' does not exist.");
        }

        var files = Directory.GetFiles(directory, "*.json");
        Array.Sort(files, StringComparer.Ordinal);

        var manifests = new List<SuiteManifest>(files.Length);
        var sources = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var file in files)
        {
            var manifest = Parse(File.ReadAllText(file), file, skipNonManifests: true);
            if (manifest is null)
            {
                continue;
            }
            if (!sources.TryAdd(manifest.Key, file))
            {
                throw new SuiteManifestException(
                    $"Suite key '{manifest.Key}' is defined in both '{sources[manifest.Key]}' and '{file}'."
                );
            }
            manifests.Add(manifest);
        }
        return manifests;
    }

    /// <summary>Parses and validates one manifest.</summary>
    /// <param name="json">The manifest JSON.</param>
    /// <param name="source">A description of the origin (usually the file path), used in error messages.</param>
    /// <returns>The validated manifest.</returns>
    /// <exception cref="SuiteManifestException">The manifest is malformed or incomplete.</exception>
    public static SuiteManifest Parse(string json, string source) => Parse(json, source, skipNonManifests: false)!;

    private static SuiteManifest? Parse(string json, string source, bool skipNonManifests)
    {
        ArgumentNullException.ThrowIfNull(json);
        try
        {
            using var document = JsonDocument.Parse(json);
            return skipNonManifests && IsNotManifest(document.RootElement) ? null : Read(document.RootElement, source);
        }
        catch (JsonException ex)
        {
            throw new SuiteManifestException($"Manifest '{source}' is not valid JSON: {ex.Message}", ex);
        }
    }

    private static bool IsNotManifest(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object)
        {
            return false;
        }
        foreach (var field in ManifestFields)
        {
            if (root.TryGetProperty(field, out _))
            {
                return false;
            }
        }
        return true;
    }

    private static SuiteManifest Read(JsonElement root, string source)
    {
        if (root.ValueKind != JsonValueKind.Object)
        {
            throw new SuiteManifestException($"Manifest '{source}' must be a JSON object.");
        }

        var key = Required(root, "key", source);
        if (key.AsSpan().IndexOfAny('/', '\\', ':') >= 0 || key is "." or "..")
        {
            throw new SuiteManifestException($"Manifest '{source}': 'key' must be a plain name.");
        }

        if (
            !Uri.TryCreate(Required(root, "url", source), UriKind.Absolute, out var url)
            || (
                !string.Equals(url.Scheme, Uri.UriSchemeHttps, StringComparison.Ordinal)
                && !string.Equals(url.Scheme, Uri.UriSchemeHttp, StringComparison.Ordinal)
            )
        )
        {
            throw new SuiteManifestException($"Manifest '{source}': 'url' must be an absolute http or https URL.");
        }

        var sha256 = Required(root, "sha256", source);
        if (!IsSha256(sha256))
        {
            throw new SuiteManifestException(
                $"Manifest '{source}': 'sha256' must be {Sha256HexLength} hexadecimal characters."
            );
        }

        var archive = Required(root, "archive", source) switch
        {
            "zip" => ArchiveType.Zip,
            "tar.gz" => ArchiveType.TarGz,
            _ => throw new SuiteManifestException($"Manifest '{source}': 'archive' must be 'zip' or 'tar.gz'."),
        };

        string? subPath = null;
        if (root.TryGetProperty("subPath", out _))
        {
            subPath = Required(root, "subPath", source);
            if (Path.IsPathRooted(subPath) || ContainsParentSegment(subPath))
            {
                throw new SuiteManifestException($"Manifest '{source}': 'subPath' must stay inside the archive.");
            }
        }

        return new SuiteManifest(key, url, HexEncoding.ToLower(Convert.FromHexString(sha256)), archive, subPath);
    }

    private static string Required(JsonElement root, string name, string source) =>
        root.TryGetProperty(name, out var value)
        && value.ValueKind == JsonValueKind.String
        && !string.IsNullOrWhiteSpace(value.GetString())
            ? value.GetString()!
            : throw new SuiteManifestException($"Manifest '{source}': '{name}' is missing or not a non-empty string.");

    private static bool IsSha256(string value)
    {
        if (value.Length != Sha256HexLength)
        {
            return false;
        }
        foreach (var c in value)
        {
            if (!char.IsAsciiHexDigit(c))
            {
                return false;
            }
        }
        return true;
    }

    private static bool ContainsParentSegment(string path)
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
