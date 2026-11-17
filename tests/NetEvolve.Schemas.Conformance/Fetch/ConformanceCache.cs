namespace NetEvolve.Schemas.Conformance.Fetch;

/// <summary>Resolves the folder that caches extracted suites.</summary>
public static class ConformanceCache
{
    /// <summary>The environment variable that overrides the cache folder.</summary>
    public const string EnvironmentVariable = "SCHEMAS_CONFORMANCE_CACHE";

    /// <summary>Resolves the cache root from <see cref="EnvironmentVariable"/>, else the repository default.</summary>
    /// <returns>The cache root path.</returns>
    public static string ResolveRoot() => ResolveRoot(Environment.GetEnvironmentVariable(EnvironmentVariable));

    /// <summary>Resolves the cache root.</summary>
    /// <param name="overrideValue">The override path, or <see langword="null"/> or empty for the repository default.</param>
    /// <returns>
    /// <paramref name="overrideValue"/> when set, otherwise <c>artifacts/conformance-cache</c> in the repository
    /// (the git-ignored folder next to <c>Schemas.slnx</c>).
    /// </returns>
    /// <exception cref="DirectoryNotFoundException">No override is set and the repository root cannot be found.</exception>
    public static string ResolveRoot(string? overrideValue)
    {
        if (!string.IsNullOrWhiteSpace(overrideValue))
        {
            return overrideValue;
        }

        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Schemas.slnx")))
            {
                return Path.Combine(dir.FullName, "artifacts", "conformance-cache");
            }
        }
        throw new DirectoryNotFoundException(
            $"Repository root not found; set {EnvironmentVariable} to choose a cache folder."
        );
    }
}
