namespace NetEvolve.Schemas.Conformance.Baseline;

using System.Text;

/// <summary>
/// The known failures of one test set. The text form is one case id per line in strictly ascending
/// ordinal order, with <c>\n</c> line endings (<c>\r\n</c> is accepted when reading).
/// </summary>
public sealed class KnownFailureBaseline
{
    private readonly HashSet<string> _set;

    private KnownFailureBaseline(List<string> ids)
    {
        Ids = ids;
        _set = new HashSet<string>(ids, StringComparer.Ordinal);
    }

    /// <summary>Gets a baseline without any known failure.</summary>
    public static KnownFailureBaseline Empty { get; } = new([]);

    /// <summary>Gets the case ids in ascending ordinal order.</summary>
    public IReadOnlyList<string> Ids { get; }

    /// <summary>Gets the number of known failures.</summary>
    public int Count => Ids.Count;

    /// <summary>Determines whether a case is a known failure.</summary>
    /// <param name="caseId">The case id.</param>
    /// <returns><see langword="true"/> when the id is in the baseline.</returns>
    public bool Contains(string caseId) => _set.Contains(caseId);

    /// <summary>Parses the text form of a baseline.</summary>
    /// <param name="text">The text, one case id per line.</param>
    /// <returns>The baseline.</returns>
    /// <exception cref="BaselineFormatException">A line is blank, unsorted, or a duplicate.</exception>
    public static KnownFailureBaseline Parse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        return Parse(text.AsSpan());
    }

    /// <summary>Parses the text form of a baseline.</summary>
    /// <param name="text">The text, one case id per line.</param>
    /// <returns>The baseline.</returns>
    /// <exception cref="BaselineFormatException">A line is blank, unsorted, or a duplicate.</exception>
    public static KnownFailureBaseline Parse(ReadOnlySpan<char> text)
    {
        var ids = new List<string>();
        var lineNumber = 0;
        while (!text.IsEmpty)
        {
            lineNumber++;
            var end = text.IndexOf('\n');
            var line = end < 0 ? text : text[..end];
            text = end < 0 ? default : text[(end + 1)..];
            if (!line.IsEmpty && line[^1] == '\r')
            {
                line = line[..^1];
            }

            if (line.IsEmpty)
            {
                throw new BaselineFormatException($"Baseline line {lineNumber} is blank.");
            }

            if (ids.Count > 0)
            {
                var order = line.CompareTo(ids[^1].AsSpan(), StringComparison.Ordinal);
                if (order == 0)
                {
                    throw new BaselineFormatException($"Baseline line {lineNumber} duplicates '{line.ToString()}'.");
                }

                if (order < 0)
                {
                    throw new BaselineFormatException(
                        $"Baseline line {lineNumber} is out of order: '{line.ToString()}' sorts before '{ids[^1]}'."
                    );
                }
            }

            ids.Add(line.ToString());
        }

        return new KnownFailureBaseline(ids);
    }

    /// <summary>Formats case ids as the text form: sorted ordinally, duplicates removed, one id per line.</summary>
    /// <param name="caseIds">The case ids in any order.</param>
    /// <returns>The text; identical for the same set of ids.</returns>
    public static string Format(IEnumerable<string> caseIds)
    {
        ArgumentNullException.ThrowIfNull(caseIds);
        var ids = new List<string>(caseIds);
        ids.Sort(StringComparer.Ordinal);

        var builder = new StringBuilder();
        string? previous = null;
        foreach (var id in ids)
        {
            if (string.Equals(id, previous, StringComparison.Ordinal))
            {
                continue;
            }

            _ = builder.Append(id).Append('\n');
            previous = id;
        }

        return builder.ToString();
    }
}
