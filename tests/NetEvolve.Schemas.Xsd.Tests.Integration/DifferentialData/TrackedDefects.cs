namespace NetEvolve.Schemas.Xsd.Tests.Integration.DifferentialData;

using NetEvolve.Schemas.Xsd.Tests.Integration.Differential;

/// <summary>
/// The cases that the known-failures lists of the schema tests and the instance tests of XSD 1.0 track with a bug issue. A
/// difference to System.Xml.Schema on such a case is a symptom of that defect and is tracked there, not in the deviation list.
/// </summary>
/// <remarks>
/// Limitation: a case leaves this set when its issue is fixed and the case leaves the known-failures list; every difference
/// that is left on it then needs an entry of its own.
/// </remarks>
internal static class TrackedDefects
{
    private const string NoIssue = "-";

    private static readonly string ProjectDirectory = Path.Combine(
        RepositoryFiles.Root,
        "tests",
        "NetEvolve.Schemas.Xsd.Tests.Integration"
    );

    /// <summary>Gets the paths of the known-failures lists.</summary>
    public static IReadOnlyList<string> ListPaths { get; } =
    [
        Path.Combine(ProjectDirectory, "SchemaTests", "known-failures.txt"),
        Path.Combine(ProjectDirectory, "InstanceTests", "known-failures.txt"),
    ];

    /// <summary>Reads the known-failures lists.</summary>
    /// <returns>The issue reference of every tracked case, keyed by the case id of <see cref="DifferentialCorpus.IdOf"/>.</returns>
    public static IReadOnlyDictionary<string, string> Load()
    {
        var tracked = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var path in ListPaths)
        {
            Read(File.ReadAllText(path), tracked);
        }

        return tracked;
    }

    /// <summary>Reads one list; lines of cases without an issue are left out.</summary>
    /// <param name="text">The list: test set, case id, issue, and reason, separated by tabs; <c>#</c> starts a comment line.</param>
    /// <param name="tracked">Receives the issue reference of every case that has one.</param>
    internal static void Read(string text, Dictionary<string, string> tracked)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(tracked);
        foreach (var raw in text.Split('\n'))
        {
            var line = raw.TrimEnd('\r');
            if (line.Length == 0 || line[0] == '#')
            {
                continue;
            }

            var fields = line.Split('\t');
            if (fields.Length >= 3 && !string.Equals(fields[2], NoIssue, StringComparison.Ordinal))
            {
                tracked[fields[0] + "/" + fields[1]] = fields[2];
            }
        }
    }
}
