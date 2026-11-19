namespace NetEvolve.Schemas.Conformance.Reporting;

using System.Collections.Concurrent;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using NetEvolve.Schemas.Conformance.Results;

/// <summary>
/// Publishes the conformance rate per package and target framework as a compact Markdown table to the GitHub Actions
/// job summary, the file named by the <c>GITHUB_STEP_SUMMARY</c> environment variable.
/// </summary>
/// <remarks>
/// The summary has one table per run, with one row per package and target framework, and one collapsed
/// <c>&lt;details&gt;</c> block per row that lists the test sets. The header and the footnote appear once.
/// The rate is passed / (passed + failed). Skipped cases are not applicable and excluded from the denominator.
/// The rate is truncated, never rounded up, to one decimal, so that 99.99 percent displays as 99.9 percent and
/// only a suite without a single failure displays as 100.0 percent. A row without applicable cases displays
/// <c>n/a</c>. Failed cases that are listed in the known-failure baseline display as baselined, failed cases outside
/// the baseline as regressions. The detail rows are sorted by the lowest rate first, then by the most baselined
/// cases, then by test set name, so a run compares as text and the rows that need attention come first.
/// </remarks>
public static class ConformanceRateReporter
{
    /// <summary>The name of the environment variable that holds the job summary file path.</summary>
    public const string SummaryVariable = "GITHUB_STEP_SUMMARY";

    private const string Footnote =
        "_Rate = passed / (passed + failed). Skipped cases are excluded from the denominator. The rate is truncated, never rounded up, to one decimal."
        + " Baselined = failed cases recorded in the committed known-failure baseline (known gaps, not regressions)."
        + " Regressions = failed cases outside the baseline; they fail the run._";

    private static readonly ConcurrentQueue<(
        string Package,
        string TargetFramework,
        ConformanceReport Report
    )> Pending = new();

    /// <summary>Renders the compact table block of one package and target framework.</summary>
    /// <param name="package">The package that owns the suite.</param>
    /// <param name="targetFramework">The target framework of the test run.</param>
    /// <param name="report">The report of the run.</param>
    /// <returns>
    /// The Markdown table, the collapsed detail block when the report has test sets, a footnote, and a trailing
    /// blank line; lines end with <c>\n</c>.
    /// </returns>
    public static string Render(string package, string targetFramework, ConformanceReport report)
    {
        ArgumentNullException.ThrowIfNull(package);
        ArgumentNullException.ThrowIfNull(targetFramework);
        ArgumentNullException.ThrowIfNull(report);

        var group = new Group(package, targetFramework);
        group.Sets.AddRange(report.TestSets);
        return Render([group]);
    }

    /// <summary>Appends the table block to the job summary file; does nothing when the environment variable is not set.</summary>
    /// <param name="package">The package that owns the suite.</param>
    /// <param name="targetFramework">The target framework of the test run.</param>
    /// <param name="report">The report of the run.</param>
    public static void Append(string package, string targetFramework, ConformanceReport report) =>
        Append(package, targetFramework, report, Environment.GetEnvironmentVariable(SummaryVariable));

    /// <summary>Appends the table block to the given file; does nothing when the path is <see langword="null"/> or empty.</summary>
    /// <param name="package">The package that owns the suite.</param>
    /// <param name="targetFramework">The target framework of the test run.</param>
    /// <param name="report">The report of the run.</param>
    /// <param name="summaryPath">The summary file path.</param>
    public static void Append(string package, string targetFramework, ConformanceReport report, string? summaryPath)
    {
        if (string.IsNullOrEmpty(summaryPath))
        {
            return;
        }

        File.AppendAllText(summaryPath, Render(package, targetFramework, report), new UTF8Encoding(false));
    }

    /// <summary>
    /// Buffers the report of one or more test sets, so that the whole run publishes one table through
    /// <see cref="Flush()"/> instead of one block per call.
    /// </summary>
    /// <param name="package">The package that owns the suite.</param>
    /// <param name="targetFramework">The target framework of the test run.</param>
    /// <param name="report">The report of the test sets.</param>
    public static void Collect(string package, string targetFramework, ConformanceReport report)
    {
        ArgumentNullException.ThrowIfNull(package);
        ArgumentNullException.ThrowIfNull(targetFramework);
        ArgumentNullException.ThrowIfNull(report);

        Pending.Enqueue((package, targetFramework, report));
    }

    /// <summary>Appends the buffered reports to the job summary file; does nothing when the environment variable is not set.</summary>
    public static void Flush() => Flush(Environment.GetEnvironmentVariable(SummaryVariable));

    /// <summary>
    /// Appends the buffered reports as one table to the given file and empties the buffer; writes nothing when the
    /// path is <see langword="null"/> or empty, or when nothing was collected.
    /// </summary>
    /// <param name="summaryPath">The summary file path.</param>
    public static void Flush(string? summaryPath)
    {
        var groups = new List<Group>();
        while (Pending.TryDequeue(out var entry))
        {
            var group = groups.Find(g =>
                string.Equals(g.Package, entry.Package, StringComparison.Ordinal)
                && string.Equals(g.TargetFramework, entry.TargetFramework, StringComparison.Ordinal)
            );
            if (group is null)
            {
                group = new Group(entry.Package, entry.TargetFramework);
                groups.Add(group);
            }

            group.Sets.AddRange(entry.Report.TestSets);
        }

        if (string.IsNullOrEmpty(summaryPath) || groups.Count == 0)
        {
            return;
        }

        File.AppendAllText(summaryPath, Render(groups), new UTF8Encoding(false));
    }

    private static string Render(List<Group> groups)
    {
        groups.Sort(CompareGroups);

        var builder = new StringBuilder()
            .Append(
                "| Package | Target framework | Test sets | Applicable | Passed | Baselined | Regressions | Skipped | Rate |\n"
            )
            .Append("| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |\n");

        foreach (var group in groups)
        {
            var totals = default(Totals);
            foreach (var set in group.Sets)
            {
                totals = totals.Add(set);
            }

            builder
                .Append("| ")
                .Append(Escape(group.Package))
                .Append(" | ")
                .Append(Escape(group.TargetFramework))
                .Append(" | ")
                .Append(group.Sets.Count.ToString(CultureInfo.InvariantCulture));
            AppendCounts(builder, totals);
        }

        foreach (var group in groups)
        {
            if (group.Sets.Count == 0)
            {
                continue;
            }

            group.Sets.Sort(CompareSets);
            builder
                .Append("\n<details>\n<summary>")
                .Append(Escape(group.Package))
                .Append(' ')
                .Append(Escape(group.TargetFramework))
                .Append(": ")
                .Append(group.Sets.Count.ToString(CultureInfo.InvariantCulture))
                .Append(" test sets</summary>\n\n")
                .Append("| Test set | Applicable | Passed | Baselined | Regressions | Skipped | Rate |\n")
                .Append("| --- | ---: | ---: | ---: | ---: | ---: | ---: |\n");

            foreach (var set in group.Sets)
            {
                builder.Append("| ").Append(Escape(set.TestSet));
                AppendCounts(builder, default(Totals).Add(set));
            }

            builder.Append("\n</details>\n");
        }

        return builder.Append('\n').Append(Footnote).Append("\n\n").ToString();
    }

    private static void AppendCounts(StringBuilder builder, Totals totals) =>
        builder
            .Append(" | ")
            .Append(totals.Applicable.ToString(CultureInfo.InvariantCulture))
            .Append(" | ")
            .Append(totals.Passed.ToString(CultureInfo.InvariantCulture))
            .Append(" | ")
            .Append(totals.Baselined.ToString(CultureInfo.InvariantCulture))
            .Append(" | ")
            .Append(totals.Regressions.ToString(CultureInfo.InvariantCulture))
            .Append(" | ")
            .Append(totals.Skipped.ToString(CultureInfo.InvariantCulture))
            .Append(" | ")
            .Append(Rate(totals))
            .Append(" |\n");

    // Integer math: floating point could turn 99.9 into 99.89999 or 100.0 through rounding.
    private static string Rate(Totals totals)
    {
        if (totals.Applicable == 0)
        {
            return "n/a";
        }

        var tenths = (long)totals.Passed * 1000 / totals.Applicable;
        return string.Create(CultureInfo.InvariantCulture, $"{tenths / 10}.{tenths % 10}%");
    }

    private static int CompareGroups(Group left, Group right)
    {
        var package = string.CompareOrdinal(left.Package, right.Package);
        return package != 0 ? package : string.CompareOrdinal(left.TargetFramework, right.TargetFramework);
    }

    // Lowest rate first, a set without applicable cases last, then the most baselined cases, then the name.
    private static int CompareSets(TestSetReport left, TestSetReport right)
    {
        var l = default(Totals).Add(left);
        var r = default(Totals).Add(right);
        var notApplicable = (l.Applicable == 0).CompareTo(r.Applicable == 0);
        if (notApplicable != 0)
        {
            return notApplicable;
        }

        var rate = ((long)l.Passed * r.Applicable).CompareTo((long)r.Passed * l.Applicable);
        if (rate != 0)
        {
            return rate;
        }

        var baselined = r.Baselined.CompareTo(l.Baselined);
        return baselined != 0 ? baselined : string.CompareOrdinal(left.TestSet, right.TestSet);
    }

    private static string Escape(string value) =>
        value
            .Replace("&", "&amp;", StringComparison.Ordinal)
            .Replace("<", "&lt;", StringComparison.Ordinal)
            .Replace("|", "&#124;", StringComparison.Ordinal);

    private sealed class Group(string package, string targetFramework)
    {
        public string Package { get; } = package;

        public string TargetFramework { get; } = targetFramework;

        public List<TestSetReport> Sets { get; } = [];
    }

    // Failed cases split into baselined known failures and regressions; the rate counts both as failed.
    [StructLayout(LayoutKind.Auto)]
    private readonly record struct Totals(int Passed, int Baselined, int Regressions, int Skipped)
    {
        public int Applicable => Passed + Baselined + Regressions;

        public Totals Add(TestSetReport set) =>
            new(
                Passed + set.Counts.Passed,
                Baselined + set.Counts.Failed - set.Regressions.Count,
                Regressions + set.Regressions.Count,
                Skipped + set.Counts.Skipped
            );
    }
}
