namespace NetEvolve.Schemas.Schematron.Tests.Integration.Conformance.Engine;

using System.Globalization;

/// <summary>
/// Reads the file that links every baseline entry to its issue and checks it against the baseline. The baseline files hold case ids only
/// and are shared by all conformance suites, so the link of an entry to its bug issue and its reason lives in this file: one line per
/// case, <c>case id</c>, <c>#issue</c>, and <c>reason</c> separated by tab characters; blank lines and lines starting with <c>#</c> are
/// ignored.
/// </summary>
internal static class KnownFailureLinks
{
    /// <summary>Reads the links.</summary>
    /// <param name="text">The text of the file.</param>
    /// <returns>The links in file order.</returns>
    /// <exception cref="InvalidDataException">A line has no case id, no valid issue number, or no reason, or a case is listed twice.</exception>
    public static IReadOnlyList<KnownFailureLink> Parse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var links = new List<KnownFailureLink>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var lineNumber = 0;
        foreach (var raw in text.Split('\n'))
        {
            lineNumber++;
            var line = raw.TrimEnd('\r');
            if (line.Length == 0 || line[0] == '#')
            {
                continue;
            }

            var link = ParseLine(line, lineNumber);
            if (!seen.Add(link.CaseId))
            {
                throw new InvalidDataException($"Line {lineNumber}: '{link.CaseId}' is listed twice.");
            }

            links.Add(link);
        }

        return links;
    }

    /// <summary>Checks the links against the baseline.</summary>
    /// <param name="baselineIds">The case ids of all baseline files.</param>
    /// <param name="links">The links.</param>
    /// <returns>One problem per baseline entry without a link and per link without a baseline entry; empty when both agree.</returns>
    public static IReadOnlyList<string> Verify(
        IReadOnlyCollection<string> baselineIds,
        IReadOnlyList<KnownFailureLink> links
    )
    {
        ArgumentNullException.ThrowIfNull(baselineIds);
        ArgumentNullException.ThrowIfNull(links);
        var linked = new HashSet<string>(StringComparer.Ordinal);
        foreach (var link in links)
        {
            _ = linked.Add(link.CaseId);
        }

        var problems = new List<string>();
        foreach (var id in baselineIds)
        {
            if (!linked.Contains(id))
            {
                problems.Add($"The baseline entry '{id}' has no linked bug issue.");
            }
        }

        var baseline = new HashSet<string>(baselineIds, StringComparer.Ordinal);
        foreach (var link in links)
        {
            if (!baseline.Contains(link.CaseId))
            {
                problems.Add($"The link of '{link.CaseId}' to #{link.Issue} has no baseline entry; remove it.");
            }
        }

        return problems;
    }

    private static KnownFailureLink ParseLine(string line, int lineNumber)
    {
        var fields = line.Split('\t');
        if (fields.Length != 3)
        {
            throw new InvalidDataException(
                $"Line {lineNumber}: expected 'case id', '#issue' and 'reason' separated by tabs."
            );
        }

        var issue = fields[1];
        if (
            fields[0].Length == 0
            || issue.Length < 2
            || issue[0] != '#'
            || !int.TryParse(issue.AsSpan(1), NumberStyles.None, CultureInfo.InvariantCulture, out var number)
            || number < 1
        )
        {
            throw new InvalidDataException($"Line {lineNumber}: '{issue}' is no issue number of the form #123.");
        }

        return string.IsNullOrWhiteSpace(fields[2])
            ? throw new InvalidDataException($"Line {lineNumber}: the reason is empty.")
            : new KnownFailureLink(fields[0], number, fields[2]);
    }
}
