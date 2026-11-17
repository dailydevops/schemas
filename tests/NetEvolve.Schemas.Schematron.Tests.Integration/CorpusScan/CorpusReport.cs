namespace NetEvolve.Schemas.Schematron.Tests.Integration.CorpusScan;

using System.Globalization;

/// <summary>Renders scan results as Markdown.</summary>
internal static class CorpusReport
{
    /// <summary>Renders one section per rule set.</summary>
    /// <param name="scans">The scan results.</param>
    /// <returns>The Markdown report.</returns>
    public static string Render(IReadOnlyList<RuleSetScan> scans)
    {
        var blocks = new List<string>
        {
            "## XSLT usage in pinned rule sets",
            "Element counts are exact: elements are matched by expanded name, never by prefix. Function counts are approximate: names come from a lexical scan of expression attributes, not from parsing.",
        };

        foreach (var scan in scans)
        {
            blocks.Add("### " + scan.Name);
            if (scan.Elements.Count == 0 && scan.Functions.Count == 0)
            {
                blocks.Add("No XSLT constructs found.");
            }
            AddTable(blocks, "Construct", scan.Elements);
            AddTable(blocks, "Function (approximate)", scan.Functions);
            if (scan.Unresolved.Count > 0)
            {
                blocks.Add("Unresolved includes:");
                blocks.Add(string.Join("\n", scan.Unresolved.Select(u => "- `" + u + "`")));
            }
        }

        return string.Join("\n\n", blocks) + "\n";
    }

    private static void AddTable(List<string> blocks, string heading, IReadOnlyDictionary<string, int> counts)
    {
        if (counts.Count == 0)
        {
            return;
        }

        var lines = new List<string> { $"| {heading} | Count |", "| --- | ---: |" };
        foreach (var (name, count) in counts)
        {
            lines.Add($"| {name} | {count.ToString(CultureInfo.InvariantCulture)} |");
        }
        blocks.Add(string.Join("\n", lines));
    }
}
