namespace NetEvolve.Schemas.Schematron.Tests.Integration.CorpusScan;

/// <summary>Renders scan results as Markdown.</summary>
internal static class CorpusReport
{
    /// <summary>Renders one section per rule set.</summary>
    /// <param name="scans">The scan results.</param>
    /// <returns>The Markdown report.</returns>
    public static string Render(IReadOnlyList<RuleSetScan> scans) =>
        throw new NotSupportedException("Not implemented yet.");
}
