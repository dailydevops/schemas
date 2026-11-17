namespace NetEvolve.Schemas.Schematron.Tests.Integration.CorpusScan;

using NetEvolve.Schemas.Conformance.Fetch;
using NetEvolve.Schemas.Conformance.Manifest;

/// <summary>Scans the rule sets of fetched suites and publishes the report.</summary>
internal static class CorpusScanRun
{
    /// <summary>Scans every rule set below a suite root.</summary>
    /// <param name="key">The suite key; prefixes the rule set names.</param>
    /// <param name="root">The suite root directory.</param>
    /// <returns>One scan per rule set, ordered by path.</returns>
    public static IReadOnlyList<RuleSetScan> ScanSuite(string key, string root) =>
        throw new NotSupportedException("Not implemented yet.");

    /// <summary>Loads the rule set pins of the manifest directory.</summary>
    /// <param name="directory">The manifest directory.</param>
    /// <returns>The pinned rule set suites.</returns>
    public static IReadOnlyList<SuiteManifest> LoadPins(string directory) =>
        throw new NotSupportedException("Not implemented yet.");

    /// <summary>Fetches every pinned suite by checksum and scans its rule sets.</summary>
    /// <param name="manifests">The pinned suites.</param>
    /// <param name="fetcher">The fetcher.</param>
    /// <param name="cancellationToken">A token to cancel the run.</param>
    /// <returns>One scan per rule set.</returns>
    public static Task<IReadOnlyList<RuleSetScan>> ScanAsync(
        IReadOnlyList<SuiteManifest> manifests,
        SuiteFetcher fetcher,
        CancellationToken cancellationToken
    ) => throw new NotSupportedException("Not implemented yet.");

    /// <summary>Appends the report to the CI job summary, or writes it to the artifacts folder outside CI.</summary>
    /// <param name="report">The Markdown report.</param>
    /// <param name="repositoryRoot">The repository root.</param>
    /// <returns>The path written to.</returns>
    public static string WriteSummary(string report, string repositoryRoot) =>
        throw new NotSupportedException("Not implemented yet.");
}
