namespace NetEvolve.Schemas.Schematron.Tests.Integration.CorpusScan;

using NetEvolve.Schemas.Conformance.Fetch;
using NetEvolve.Schemas.Conformance.Manifest;

/// <summary>Scans the rule sets of fetched suites and publishes the report.</summary>
internal static class CorpusScanRun
{
    private const string StageSourcesFile = "stage-sources.json";
    private const string JobSummaryVariable = "GITHUB_STEP_SUMMARY";

    /// <summary>Scans every rule set below a suite root.</summary>
    /// <param name="key">The suite key; prefixes the rule set names.</param>
    /// <param name="root">The suite root directory.</param>
    /// <returns>One scan per rule set, ordered by path.</returns>
    public static IReadOnlyList<RuleSetScan> ScanSuite(string key, string root)
    {
        var files = Directory.GetFiles(root, "*.sch", SearchOption.AllDirectories);
        Array.Sort(files, StringComparer.Ordinal);

        var scans = new List<RuleSetScan>();
        foreach (var file in files)
        {
            if (CorpusScanner.IsRuleSet(file))
            {
                var relative = Path.GetRelativePath(root, file).Replace(Path.DirectorySeparatorChar, '/');
                scans.Add(CorpusScanner.Scan(file, root, key + "/" + relative));
            }
        }
        return scans;
    }

    /// <summary>Loads the rule set pins of the manifest directory.</summary>
    /// <param name="directory">The manifest directory.</param>
    /// <returns>The pinned rule set suites (keys starting with <c>stage</c>); <c>stage-sources.json</c> pins no archive and other pins such as the Unicode Character Database carry no rule sets, so both are skipped.</returns>
    public static IReadOnlyList<SuiteManifest> LoadPins(string directory)
    {
        var files = Directory.GetFiles(directory, "*.json");
        Array.Sort(files, StringComparer.Ordinal);

        var pins = new List<SuiteManifest>();
        foreach (var file in files)
        {
            if (!string.Equals(Path.GetFileName(file), StageSourcesFile, StringComparison.Ordinal))
            {
                var manifest = SuiteManifestLoader.Parse(File.ReadAllText(file), file);
                if (manifest.Key.StartsWith("stage", StringComparison.Ordinal))
                {
                    pins.Add(manifest);
                }
            }
        }
        return pins;
    }

    /// <summary>Fetches every pinned suite by checksum and scans its rule sets.</summary>
    /// <param name="manifests">The pinned suites.</param>
    /// <param name="fetcher">The fetcher.</param>
    /// <param name="cancellationToken">A token to cancel the run.</param>
    /// <returns>One scan per rule set.</returns>
    public static async Task<IReadOnlyList<RuleSetScan>> ScanAsync(
        IReadOnlyList<SuiteManifest> manifests,
        SuiteFetcher fetcher,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var scans = new List<RuleSetScan>();
        foreach (var manifest in manifests)
        {
            var root = await fetcher.FetchAsync(manifest, cancellationToken).ConfigureAwait(false);
            scans.AddRange(ScanSuite(manifest.Key, root));
        }
        return scans;
    }

    /// <summary>Appends the report to the CI job summary, or writes it to the artifacts folder outside CI.</summary>
    /// <param name="report">The Markdown report.</param>
    /// <param name="repositoryRoot">The repository root.</param>
    /// <returns>The path written to.</returns>
    public static string WriteSummary(string report, string repositoryRoot)
    {
        var summary = Environment.GetEnvironmentVariable(JobSummaryVariable);
        if (!string.IsNullOrWhiteSpace(summary))
        {
            File.AppendAllText(summary, report + "\n");
            return summary;
        }

        var directory = Directory.CreateDirectory(Path.Combine(repositoryRoot, "artifacts", "corpus-scan")).FullName;
        var path = Path.Combine(directory, "report.md");
        File.WriteAllText(path, report);
        return path;
    }
}
