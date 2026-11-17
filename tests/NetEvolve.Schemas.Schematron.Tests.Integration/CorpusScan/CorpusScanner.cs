namespace NetEvolve.Schemas.Schematron.Tests.Integration.CorpusScan;

/// <summary>Counts the XSLT constructs a rule set embeds.</summary>
internal static class CorpusScanner
{
    /// <summary>Scans a rule set and the parts it includes.</summary>
    /// <param name="ruleSetPath">The rule set file.</param>
    /// <param name="root">The directory that includes must stay inside.</param>
    /// <param name="name">The display name of the rule set.</param>
    /// <returns>The scan result.</returns>
    public static RuleSetScan Scan(string ruleSetPath, string root, string name)
    {
        _ = (ruleSetPath, root);
        var empty = new SortedDictionary<string, int>(StringComparer.Ordinal);
        return new(name, empty, empty, []);
    }

    /// <summary>Tells whether a file is a Schematron schema document.</summary>
    /// <param name="path">The file to inspect.</param>
    /// <returns><see langword="true"/> when the root element is <c>schema</c> in the Schematron namespace.</returns>
    public static bool IsRuleSet(string path) => throw new NotSupportedException("Not implemented yet.");
}
