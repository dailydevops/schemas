namespace NetEvolve.Schemas.Schematron.Tests.Integration.Conformance.Xspec;

/// <summary>Reads the declarative subset of the XSpec files for Schematron.</summary>
/// <remarks>
/// A file is read when its path below the XSpec root names Schematron (the selection of the issue: <c>test</c> and <c>tutorial</c>). A file
/// whose path is listed in <see cref="ExcludedPaths"/> tests XSpec itself and is excluded as a whole, with its reason.
/// </remarks>
internal static class XspecReader
{
    private static readonly string[] Roots = ["test", "tutorial"];

    /// <summary>The path prefixes, relative to the XSpec root, of files that test XSpec itself, each with the reason.</summary>
    internal static readonly IReadOnlyList<(string Prefix, string Reason)> ExcludedPaths =
    [
        (
            "test/schematron/bad-location/",
            "These files test the error XSpec raises for a location that does not select exactly one node; they expect a failed XSpec run."
        ),
        (
            "test/end-to-end/cases/focus-vs-pending_schematron.xspec",
            "The file tests the focus and pending handling of XSpec, which makes the other expectations pending; the reader does not implement focus."
        ),
        (
            "test/end-to-end/cases/pending_schematron.xspec",
            "The file tests the pending handling of XSpec: its expectations check a schema that does nothing and are reported as pending."
        ),
        (
            "test/end-to-end/cases/schematron-023.xspec",
            "The file is an input of the end-to-end tests of XSpec: its expectations fail on purpose to test the XSpec report."
        ),
        (
            "test/some-failures/schematron.xspec",
            "The file is an input of the tests of XSpec: its expectations fail on purpose to test the XSpec report."
        ),
    ];

    /// <summary>Reads every XSpec file below <c>test</c> and <c>tutorial</c> whose path names Schematron.</summary>
    /// <param name="suiteRoot">The root directory of the XSpec project.</param>
    /// <returns>The expectations to run and the parts that are left out.</returns>
    public static XspecSelection Read(string suiteRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(suiteRoot);
        var selected = new List<XspecCase>();
        var excluded = new List<XspecExclusion>();
        var files = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var root in Roots)
        {
            var directory = Path.Combine(suiteRoot, root);
            if (!Directory.Exists(directory))
            {
                continue;
            }

            foreach (var file in Directory.EnumerateFiles(directory, "*.xspec", SearchOption.AllDirectories))
            {
                var relative = Path.GetRelativePath(suiteRoot, file).Replace('\\', '/');
                if (relative.Contains("schematron", StringComparison.OrdinalIgnoreCase))
                {
                    files.Add(relative, file);
                }
            }
        }

        foreach (var (relative, file) in files)
        {
            var testSet = relative[..^".xspec".Length].Replace('/', '-');
            var reader = new XspecFileReader(testSet, selected, excluded);
            var exclusion = ExcludedPaths.FirstOrDefault(e => relative.StartsWith(e.Prefix, StringComparison.Ordinal));
            if (exclusion is (_, { } reason))
            {
                reader.ExcludeFile(reason);
            }
            else
            {
                reader.ReadFile(file);
            }
        }

        return new XspecSelection(selected, excluded);
    }
}
