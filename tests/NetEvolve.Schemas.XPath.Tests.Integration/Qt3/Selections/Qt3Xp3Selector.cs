namespace NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Selections;

using System.Text.RegularExpressions;
using NetEvolve.Arguments;
using NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Runner;

/// <summary>
/// Selects the cases of the QT3 suite that apply to XPath 3.0 or XPath 3.1. Only declared catalog dependencies, the test set names,
/// and the functions and collations of <see cref="Qt3Xp3Exclusions"/> filter; set-level and case-level dependencies are combined with
/// AND. Limitation: the cases that apply to XQuery or XSLT only are excluded with that reason, and no DEF issue covers them.
/// </summary>
internal static class Qt3Xp3Selector
{
    private static readonly char[] Separators = [' ', '\t', '\r', '\n'];

    private static readonly string[] Xp30Tokens = ["XP10+", "XP20+", "XP30", "XP30+"];

    private static readonly string[] Xp31Tokens = ["XP10+", "XP20+", "XP30+", "XP31", "XP31+"];

    private static readonly List<Qt3ExclusionEntry> DependencyEntries = Of(Qt3ExclusionKind.Dependency);

    private static readonly Dictionary<string, Qt3ExclusionEntry> TestSetEntries = ToDictionary(
        Of(Qt3ExclusionKind.TestSet)
    );

    private static readonly List<(Regex Pattern, Qt3ExclusionEntry Entry)> PatternEntries = Patterns();

    public static Qt3Selection Select(IReadOnlyList<Qt3TestSet> testSets, Qt3SpecLevel level)
    {
        Argument.ThrowIfNull(testSets);
        var accepted = level == Qt3SpecLevel.XPath30 ? Xp30Tokens : Xp31Tokens;
        var selected = new List<Qt3Case>();
        var excluded = new List<Qt3Exclusion>();
        foreach (var set in testSets)
        {
            var reasons = new List<string>();
            CollectUnmet(set.Dependencies, accepted, reasons);
            if (TestSetEntries.TryGetValue(set.Name, out var entry))
            {
                reasons.Add("test set " + set.Name + ": " + entry.Describe());
            }

            var setReasons = reasons.Count;
            foreach (var testCase in set.Cases)
            {
                reasons.RemoveRange(setReasons, reasons.Count - setReasons);
                CollectUnmet(testCase.Dependencies, accepted, reasons);
                if (reasons.Count == 0)
                {
                    CollectMentioned(testCase, reasons);
                }

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

    private static void CollectUnmet(IReadOnlyList<Qt3Dependency> dependencies, string[] accepted, List<string> reasons)
    {
        foreach (var dependency in dependencies)
        {
            if (string.Equals(dependency.Type, "spec", StringComparison.Ordinal))
            {
                var tokens = dependency.Value.Split(Separators, StringSplitOptions.RemoveEmptyEntries);
                if (ContainsAny(tokens, accepted) != dependency.Satisfied)
                {
                    reasons.Add(
                        "spec "
                            + dependency.Value
                            + (
                                Array.Exists(tokens, static t => t.StartsWith("XP", StringComparison.Ordinal))
                                    ? ": applies to another XPath version"
                                    : ": applies to XQuery or XSLT only"
                            )
                    );
                }
                continue;
            }

            foreach (var entry in DependencyEntries)
            {
                if (Qt3Xp3Exclusions.Matches(entry, dependency))
                {
                    reasons.Add(entry.Key + ": " + entry.Describe());
                }
            }
        }
    }

    private static void CollectMentioned(Qt3Case testCase, List<string> reasons)
    {
        var text = testCase.Query + "\n" + testCase.Assertion.Value;
        if (testCase.QueryFile is not null && File.Exists(testCase.QueryFile))
        {
            text += "\n" + File.ReadAllText(testCase.QueryFile);
        }

        foreach (var (pattern, entry) in PatternEntries)
        {
            if (pattern.IsMatch(text))
            {
                reasons.Add("mentions " + entry.Key + ": " + entry.Describe());
            }
        }
    }

    private static bool ContainsAny(string[] tokens, string[] wanted)
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

    private static List<Qt3ExclusionEntry> Of(Qt3ExclusionKind kind)
    {
        var entries = new List<Qt3ExclusionEntry>();
        foreach (var entry in Qt3Xp3Exclusions.All)
        {
            if (entry.Kind == kind)
            {
                entries.Add(entry);
            }
        }
        return entries;
    }

    private static Dictionary<string, Qt3ExclusionEntry> ToDictionary(List<Qt3ExclusionEntry> entries)
    {
        var map = new Dictionary<string, Qt3ExclusionEntry>(StringComparer.Ordinal);
        foreach (var entry in entries)
        {
            map.Add(entry.Key, entry);
        }
        return map;
    }

    private static List<(Regex, Qt3ExclusionEntry)> Patterns()
    {
        var patterns = new List<(Regex, Qt3ExclusionEntry)>();
        foreach (var entry in Of(Qt3ExclusionKind.Pattern))
        {
            patterns.Add((new Regex(entry.Key, RegexOptions.CultureInvariant, TimeSpan.FromSeconds(10)), entry));
        }
        return patterns;
    }
}
