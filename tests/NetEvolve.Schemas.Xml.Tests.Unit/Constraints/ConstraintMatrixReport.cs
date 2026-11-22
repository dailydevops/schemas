namespace NetEvolve.Schemas.Xml.Tests.Unit.Constraints;

using System.Collections.Generic;
using System.Globalization;

/// <summary>Renders the coverage matrix as the lines of the test output.</summary>
internal static class ConstraintMatrixReport
{
    /// <summary>Renders one line per entry and a summary line.</summary>
    /// <param name="entries">The entries of the matrix.</param>
    /// <returns>The lines of the report.</returns>
    internal static IReadOnlyList<string> Render(IReadOnlyList<ConstraintEntry> entries)
    {
        var lines = new List<string>(entries.Count + 1);
        var gaps = 0;
        foreach (var entry in entries)
        {
            if (entry.Gap is not null)
            {
                gaps++;
            }

            lines.Add(Line(entry));
        }

        lines.Add(
            entries.Count.ToString(CultureInfo.InvariantCulture)
                + " constraints: "
                + (entries.Count - gaps).ToString(CultureInfo.InvariantCulture)
                + " covered, "
                + gaps.ToString(CultureInfo.InvariantCulture)
                + " gaps with an issue each."
        );

        return lines;
    }

    private static string Line(ConstraintEntry entry)
    {
        var status = entry.Gap is { } gap ? "gap " + string.Join(", ", gap.Issues) : "covered";

        return entry.Kind.ToString().PadRight(16)
            + entry.Name.PadRight(48)
            + entry.Code.PadRight(9)
            + ("s" + entry.Section).PadRight(13)
            + status.PadRight(16)
            + "+"
            + entry.Positive.Count.ToString(CultureInfo.InvariantCulture)
            + " -"
            + entry.Negative.Count.ToString(CultureInfo.InvariantCulture);
    }
}
