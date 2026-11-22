namespace NetEvolve.Schemas.Xsd.Tests.Integration.DifferentialData;

using NetEvolve.Schemas.Xsd.Tests.Integration.Differential;

/// <summary>Locates the maintained deviation list of the XSTS 1.0 corpus and its published document.</summary>
internal static class DeviationData
{
    /// <summary>Gets the path of the data file that the differential run over the corpus consumes.</summary>
    public static string ListPath { get; } =
        Path.Combine(
            RepositoryFiles.Root,
            "tests",
            "NetEvolve.Schemas.Xsd.Tests.Integration",
            "DifferentialData",
            "xsts-1.0-deviations.txt"
        );

    /// <summary>Gets the path of the published deviation document.</summary>
    public static string DocumentPath { get; } =
        Path.Combine(RepositoryFiles.Root, "docs", "xsd", "system-xml-schema-deviations.md");

    /// <summary>Lists the case ids that a deviation document names: every non-blank line inside a fenced code block.</summary>
    /// <param name="markdown">The text of the document.</param>
    /// <returns>The distinct case ids in ordinal order.</returns>
    public static IReadOnlyList<string> CaseIdsOf(string markdown)
    {
        ArgumentNullException.ThrowIfNull(markdown);
        var ids = new SortedSet<string>(StringComparer.Ordinal);
        var inside = false;
        foreach (var raw in markdown.Split('\n'))
        {
            var line = raw.Trim();
            if (line.StartsWith("```", StringComparison.Ordinal))
            {
                inside = !inside;
            }
            else if (inside && line.Length > 0)
            {
                _ = ids.Add(line);
            }
        }

        return [.. ids];
    }
}
