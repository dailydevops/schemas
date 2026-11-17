namespace NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Runner;

/// <summary>
/// Selects the XPath 2.0 cases. Only declared catalog dependencies filter; set-level and case-level dependencies
/// are combined with AND. Limitation: XSD 1.1 and XML 1.1 cases and the features no package covers yet are excluded.
/// </summary>
internal static class Qt3Selector
{
    private static readonly char[] Separators = [' ', '\t', '\r', '\n'];

    public static Qt3Selection Select(IReadOnlyList<Qt3TestSet> testSets)
    {
        var selected = new List<Qt3Case>();
        var excluded = new List<Qt3Exclusion>();
        foreach (var set in testSets)
        {
            var reasons = new List<string>();
            CollectUnmet(set.Dependencies, reasons);
            var setReasons = reasons.Count;
            foreach (var testCase in set.Cases)
            {
                reasons.RemoveRange(setReasons, reasons.Count - setReasons);
                CollectUnmet(testCase.Dependencies, reasons);
                if (reasons.Count == 0)
                {
                    selected.Add(testCase);
                }
                else
                {
                    excluded.Add(new Qt3Exclusion(testCase.Id, string.Join("; ", reasons)));
                }
            }
        }
        return new Qt3Selection(selected, excluded);
    }

    private static void CollectUnmet(IReadOnlyList<Qt3Dependency> dependencies, List<string> reasons)
    {
        foreach (var dependency in dependencies)
        {
            if (!IsMet(dependency))
            {
                reasons.Add(dependency.Type + " " + dependency.Value);
            }
        }
    }

    private static bool IsMet(Qt3Dependency dependency)
    {
        var tokens = dependency.Value.Split(Separators, StringSplitOptions.RemoveEmptyEntries);
        switch (dependency.Type)
        {
            case "spec":
                // XP20, XP20+ and XP10+ cover XPath 2.0; XQ* and XT* never match, XP30+ and later do not either.
                return ContainsAny(tokens, "XP20", "XP20+", "XP10+") == dependency.Satisfied;
            case "feature" when dependency.Satisfied:
                return dependency.Value
                    is not (
                        "schemaImport"
                        or "schemaValidation"
                        or "staticTyping"
                        or "namespace-axis"
                        or "non_unicode_codepoint_collation"
                    );
            case "xml-version" when dependency.Satisfied:
                return !Array.TrueForAll(tokens, static t => string.Equals(t, "1.1", StringComparison.Ordinal));
            case "xsd-version" when dependency.Satisfied:
                return ContainsAny(tokens, "1.0");
            default:
                return true;
        }
    }

    private static bool ContainsAny(string[] tokens, params string[] wanted)
    {
        foreach (var token in tokens)
        {
            if (Array.IndexOf(wanted, token) >= 0)
            {
                return true;
            }
        }
        return false;
    }
}
