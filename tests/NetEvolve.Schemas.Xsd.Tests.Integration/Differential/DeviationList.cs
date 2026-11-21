namespace NetEvolve.Schemas.Xsd.Tests.Integration.Differential;

using System.Text;

/// <summary>
/// The accepted differences between this library and the baseline. The text form starts with the header line
/// <c>baseline-runtime: &lt;major.minor&gt;</c>; every other non-blank line is a comment (<c>#</c>) or one entry with five
/// fields separated by <c> | </c>: case id, difference kind, observed difference, specification reference, reason.
/// </summary>
internal sealed class DeviationList
{
    private const string HeaderPrefix = "baseline-runtime:";
    private const string FieldSeparator = " | ";

    private readonly Dictionary<(string, DifferenceKind), DeviationEntry> _byKey = [];

    public DeviationList(string baselineRuntime, IReadOnlyList<DeviationEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(baselineRuntime);
        ArgumentNullException.ThrowIfNull(entries);
        foreach (var entry in entries)
        {
            if (!_byKey.TryAdd((entry.CaseId, entry.Kind), entry))
            {
                throw new DeviationFormatException($"Two entries for case '{entry.CaseId}' and kind {entry.Kind}.");
            }
        }

        BaselineRuntime = baselineRuntime;
        Entries = entries;
    }

    /// <summary>The major and minor version of the runtime that produced the baseline verdicts.</summary>
    public string BaselineRuntime { get; }

    public IReadOnlyList<DeviationEntry> Entries { get; }

    /// <summary>Parses the text form.</summary>
    /// <param name="text">The text; <c>\n</c> and <c>\r\n</c> line endings are accepted.</param>
    /// <returns>The list.</returns>
    /// <exception cref="DeviationFormatException">The header is missing or malformed, or an entry is malformed or a duplicate.</exception>
    public static DeviationList Parse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        string? runtime = null;
        var entries = new List<DeviationEntry>();
        var lineNumber = 0;
        foreach (var raw in text.Split('\n'))
        {
            lineNumber++;
            var line = raw.Trim();
            if (line.Length == 0 || line[0] == '#')
            {
                continue;
            }

            if (runtime is null)
            {
                runtime = ParseHeader(line, lineNumber);
            }
            else
            {
                entries.Add(ParseEntry(line, lineNumber));
            }
        }

        return runtime is null
            ? throw new DeviationFormatException($"The deviation list has no '{HeaderPrefix}' header line.")
            : new DeviationList(runtime, entries);
    }

    /// <summary>Reads and parses a deviation list file.</summary>
    /// <param name="path">The path of the file.</param>
    /// <returns>The list.</returns>
    public static DeviationList Load(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return Parse(File.ReadAllText(path, Encoding.UTF8));
    }

    /// <summary>Finds the entry for a case and kind; the observed text is not compared here.</summary>
    /// <param name="caseId">The case id.</param>
    /// <param name="kind">The kind of difference.</param>
    /// <returns>The entry, or <see langword="null"/> when there is none.</returns>
    public DeviationEntry? Find(string caseId, DifferenceKind kind) =>
        _byKey.TryGetValue((caseId, kind), out var entry) ? entry : null;

    private static string ParseHeader(string line, int lineNumber)
    {
        if (!line.StartsWith(HeaderPrefix, StringComparison.Ordinal))
        {
            throw new DeviationFormatException(
                $"Line {lineNumber}: the first line must be '{HeaderPrefix} <major.minor>'."
            );
        }

        var version = line[HeaderPrefix.Length..].Trim();
        var parts = version.Split('.');
        return parts.Length == 2 && IsNumber(parts[0]) && IsNumber(parts[1])
            ? version
            : throw new DeviationFormatException(
                $"Line {lineNumber}: '{version}' is not a major.minor runtime version."
            );
    }

    private static bool IsNumber(string value)
    {
        if (value.Length == 0)
        {
            return false;
        }

        foreach (var c in value)
        {
            if (c is < '0' or > '9')
            {
                return false;
            }
        }

        return true;
    }

    private static DeviationEntry ParseEntry(string line, int lineNumber)
    {
        var fields = line.Split(FieldSeparator);
        if (fields.Length != 5)
        {
            throw new DeviationFormatException(
                $"Line {lineNumber}: expected 5 fields separated by '{FieldSeparator}', found {fields.Length}."
            );
        }

        for (var i = 0; i < fields.Length; i++)
        {
            fields[i] = fields[i].Trim();
            if (fields[i].Length == 0)
            {
                throw new DeviationFormatException($"Line {lineNumber}: field {i + 1} is empty.");
            }
        }

        if (
            !Enum.TryParse<DifferenceKind>(fields[1], ignoreCase: false, out var kind)
            || !string.Equals(Enum.GetName(kind), fields[1], StringComparison.Ordinal)
        )
        {
            throw new DeviationFormatException($"Line {lineNumber}: '{fields[1]}' is not a difference kind.");
        }

        return new DeviationEntry(fields[0], kind, fields[2], fields[3], fields[4]);
    }
}
