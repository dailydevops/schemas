namespace NetEvolve.Schemas.Schematron.Tests.Integration.CorpusScan;

/// <summary>The XSLT usage of one rule set, including everything it pulls in through includes.</summary>
/// <param name="Name">The display name of the rule set.</param>
/// <param name="Elements">XSLT element counts by expanded name, shown as <c>xsl:local</c>; exact.</param>
/// <param name="Functions">Function name counts from a lexical scan of expression attributes; approximate.</param>
/// <param name="Unresolved">Includes that could not be followed, each as <c>href: reason</c>.</param>
internal sealed record RuleSetScan(
    string Name,
    IReadOnlyDictionary<string, int> Elements,
    IReadOnlyDictionary<string, int> Functions,
    IReadOnlyList<string> Unresolved
);
