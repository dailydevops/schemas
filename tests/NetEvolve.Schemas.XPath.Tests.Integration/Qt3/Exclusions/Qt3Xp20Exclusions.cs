namespace NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Exclusions;

using System.Text;
using NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Runner;

/// <summary>
/// The committed exclusion files of the XPath 2.0 selection: one tab-separated line per excluded case,
/// <c>case id, dependencies, reason, DEF IDs</c>. The files are the record of what is excluded and why; a test keeps them equal to
/// the cases the dependency filter of <see cref="Qt3Selector"/> removes.
/// </summary>
/// <remarks>
/// Limitation: no case of the pinned suite is excluded for DEF-43, DEF-49, or DEF-75 today, because the dependency filter does not
/// remove them; the guard accepts these IDs so an entry can name them once the filter does. No excluded case is a disputed suite case.
/// </remarks>
internal static class Qt3Xp20Exclusions
{
    /// <summary>The deferred features an entry may name.</summary>
    public static readonly string[] AllowedDefIds = ["DEF-40", "DEF-41", "DEF-42", "DEF-43", "DEF-49", "DEF-75"];

    public static string DirectoryPath { get; } =
        Path.Combine(
            Qt3Suite.RepositoryRoot,
            "tests",
            "NetEvolve.Schemas.XPath.Tests.Integration",
            "Qt3",
            "Exclusions",
            "Xp20"
        );

    /// <summary>Classifies one unmet dependency.</summary>
    /// <param name="dependency">The dependency, for example <c>feature schemaImport</c>.</param>
    /// <returns>The file name, the reason, and the DEF ID or <see langword="null"/>.</returns>
    public static (string File, string Reason, string? DefId) Classify(string dependency) =>
        dependency switch
        {
            "feature schemaImport" or "feature schemaValidation" => (
                "def-40-schema-aware.txt",
                "schema-aware processing",
                "DEF-40"
            ),
            "feature staticTyping" => ("def-41-static-typing.txt", "static typing", "DEF-41"),
            "feature namespace-axis" => ("def-42-namespace-axis.txt", "the namespace axis", "DEF-42"),
            "feature non_unicode_codepoint_collation" => (
                "host-collation.txt",
                "a host collation of the test catalog other than the codepoint collation",
                null
            ),
            "xml-version 1.1" => ("xml-1.1.txt", "an XML 1.1 case; the documents of the runner are XML 1.0", null),
            "xsd-version 1.1" => ("xsd-1.1.txt", "an XSD 1.1 case; the library targets XSD 1.0", null),
            _ when dependency.StartsWith("spec ", StringComparison.Ordinal) => (
                "spec-not-applicable.txt",
                "a case for another language or version than XPath 2.0",
                null
            ),
            _ => throw new InvalidOperationException("Unclassified dependency: " + dependency),
        };

    // A case of another language or version is not applicable whatever else it needs, so its spec dependency is the only reason.
    private static (string File, string Reason, string? DefId)[] Primary(string[] dependencies)
    {
        var classes = Array.ConvertAll(dependencies, Classify);
        var spec = Array.FindAll(classes, static c => c.File == "spec-not-applicable.txt");
        return spec.Length > 0 ? spec : classes;
    }

    /// <summary>Builds the file contents from the excluded cases; a case goes to the file that sorts first among its reasons.</summary>
    /// <param name="excluded">The excluded cases.</param>
    /// <returns>The text per file name, with <c>\n</c> line endings.</returns>
    public static SortedDictionary<string, string> Format(IEnumerable<Qt3Exclusion> excluded)
    {
        var files = new SortedDictionary<string, StringBuilder>(StringComparer.Ordinal);
        foreach (var exclusion in excluded)
        {
            var classes = Primary(exclusion.Reason.Split("; "));
            var file = classes.Select(static c => c.File).Order(StringComparer.Ordinal).First();
            var reasons = string.Join("; ", classes.Select(static c => c.Reason).Distinct(StringComparer.Ordinal));
            var defs = string.Join(
                ",",
                classes
                    .Where(static c => c.DefId is not null)
                    .Select(static c => c.DefId!)
                    .Distinct(StringComparer.Ordinal)
            );
            if (!files.TryGetValue(file, out var text))
            {
                files.Add(file, text = new StringBuilder());
            }
            _ = text.Append(exclusion.CaseId)
                .Append('\t')
                .Append(exclusion.Reason)
                .Append('\t')
                .Append(reasons)
                .Append('\t')
                .Append(defs)
                .Append('\n');
        }
        var result = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var pair in files)
        {
            result.Add(pair.Key, pair.Value.ToString());
        }
        return result;
    }

    /// <summary>Loads every entry of every <c>.txt</c> file in the directory.</summary>
    /// <returns>The entries, file by file in name order.</returns>
    public static List<Qt3Xp20ExclusionEntry> Load()
    {
        var entries = new List<Qt3Xp20ExclusionEntry>();
        if (!Directory.Exists(DirectoryPath))
        {
            return entries;
        }
        var files = Directory.GetFiles(DirectoryPath, "*.txt");
        Array.Sort(files, StringComparer.Ordinal);
        foreach (var file in files)
        {
            foreach (var line in File.ReadAllLines(file))
            {
                var columns = line.Split('\t');
                if (columns.Length != 4)
                {
                    throw new FormatException(Path.GetFileName(file) + ": expected four columns: " + line);
                }
                entries.Add(new Qt3Xp20ExclusionEntry(columns[0], columns[1], columns[2], columns[3]));
            }
        }
        return entries;
    }

    /// <summary>Finds the entries that break the rules: a reason, a known DEF ID, and the DEF ID of every deferred feature.</summary>
    /// <param name="entries">The entries.</param>
    /// <returns>One message per violation; empty when the entries are valid.</returns>
    public static List<string> Validate(IEnumerable<Qt3Xp20ExclusionEntry> entries)
    {
        var problems = new List<string>();
        foreach (var entry in entries)
        {
            if (string.IsNullOrWhiteSpace(entry.Reason))
            {
                problems.Add(entry.CaseId + ": no reason");
            }
            var defs = entry.DefIds.Split([','], StringSplitOptions.RemoveEmptyEntries);
            foreach (var def in defs)
            {
                if (Array.IndexOf(AllowedDefIds, def) < 0)
                {
                    problems.Add(entry.CaseId + ": unknown deferred feature " + def);
                }
            }
            foreach (var dependency in Primary(entry.Dependencies.Split(["; "], StringSplitOptions.RemoveEmptyEntries)))
            {
                var expected = dependency.DefId;
                if (expected is not null && Array.IndexOf(defs, expected) < 0)
                {
                    problems.Add(entry.CaseId + ": " + dependency.Reason + " needs " + expected);
                }
            }
        }
        return problems;
    }
}
