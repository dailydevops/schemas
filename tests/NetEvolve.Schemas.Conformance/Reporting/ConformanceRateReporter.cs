namespace NetEvolve.Schemas.Conformance.Reporting;

using System.Collections.Concurrent;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using NetEvolve.Schemas.Conformance.Results;

/// <summary>
/// Publishes a rough overview of a conformance run to the GitHub Actions job summary, the file named by the
/// <c>GITHUB_STEP_SUMMARY</c> environment variable.
/// </summary>
/// <remarks>
/// The summary has one list item per package and target framework: the passed and the applicable cases with the
/// rate, then the failed, unexpectedly passed, pending, and skipped cases; a count of zero is left out. A run
/// without a failure shows nothing else. Failed cases are the regressions, the failed cases outside the
/// known-failure baseline; pending cases are the failed cases that the baseline lists. A regression is listed below
/// its item with its test set, its id, and its failure message, and a case that passes although the baseline lists
/// it is listed the same way; both fail the run. At most <c>50</c> cases are listed per item.
/// The rate is passed / (passed + failed + pending). Skipped cases are not applicable and excluded from the
/// denominator. The rate is truncated, never rounded up, to one decimal, so that 99.99 percent displays as
/// 99.9 percent and only a suite without a single failure displays as 100.0 percent. The items are plain list
/// items without a header, so the items of several test processes form one list.
/// </remarks>
public static class ConformanceRateReporter
{
    /// <summary>The name of the environment variable that holds the job summary file path.</summary>
    public const string SummaryVariable = "GITHUB_STEP_SUMMARY";

    private const int MaxListedCases = 50;

    private const int MaxReasonLength = 300;

    private const string NoReason = "fails and is not a known failure";

    private const string UnexpectedPass = "passes, but the baseline lists it as a known failure";

    private static readonly ConcurrentQueue<(
        string Package,
        string TargetFramework,
        ConformanceReport Report
    )> Pending = new();

    /// <summary>Renders the list item of one package and target framework.</summary>
    /// <param name="package">The package that owns the suite.</param>
    /// <param name="targetFramework">The target framework of the test run.</param>
    /// <param name="report">The report of the run.</param>
    /// <param name="listCases">
    /// <see langword="false"/> to show the counts only; a report that repeats the cases of another report passes it,
    /// so that a failed case is listed once.
    /// </param>
    /// <returns>The list item and, for a run with failures, one nested item per failed case; lines end with <c>\n</c>.</returns>
    public static string Render(string package, string targetFramework, ConformanceReport report, bool listCases = true)
    {
        ArgumentNullException.ThrowIfNull(package);
        ArgumentNullException.ThrowIfNull(targetFramework);
        ArgumentNullException.ThrowIfNull(report);

        var group = new Group(package, targetFramework) { ListCases = listCases };
        group.Sets.AddRange(report.TestSets);
        return RenderGroups([group]);
    }

    /// <summary>Appends the list item to the job summary file; does nothing when the environment variable is not set.</summary>
    /// <param name="package">The package that owns the suite.</param>
    /// <param name="targetFramework">The target framework of the test run.</param>
    /// <param name="report">The report of the run.</param>
    public static void Append(string package, string targetFramework, ConformanceReport report) =>
        Append(package, targetFramework, report, Environment.GetEnvironmentVariable(SummaryVariable));

    /// <summary>Appends the list item to the given file; does nothing when the path is <see langword="null"/> or empty.</summary>
    /// <param name="package">The package that owns the suite.</param>
    /// <param name="targetFramework">The target framework of the test run.</param>
    /// <param name="report">The report of the run.</param>
    /// <param name="summaryPath">The summary file path.</param>
    /// <param name="listCases"><see langword="false"/> to show the counts only; see <see cref="Render"/>.</param>
    public static void Append(
        string package,
        string targetFramework,
        ConformanceReport report,
        string? summaryPath,
        bool listCases = true
    )
    {
        if (string.IsNullOrEmpty(summaryPath))
        {
            return;
        }

        File.AppendAllText(summaryPath, Render(package, targetFramework, report, listCases), new UTF8Encoding(false));
    }

    /// <summary>
    /// Buffers the report of one or more test sets, so that the whole run publishes one list item per package
    /// through <see cref="Flush()"/> instead of one item per call.
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
    /// Appends the buffered reports as one list to the given file and empties the buffer; writes nothing when the
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

        File.AppendAllText(summaryPath, RenderGroups(groups), new UTF8Encoding(false));
    }

    private static string RenderGroups(List<Group> groups)
    {
        groups.Sort(CompareGroups);

        var builder = new StringBuilder();
        foreach (var group in groups)
        {
            var totals = default(Totals);
            foreach (var set in group.Sets)
            {
                totals = totals.Add(set);
            }

            var failed = totals.Regressions > 0 || totals.UnexpectedPasses > 0;
            builder
                .Append(failed ? "- :x: **" : "- :white_check_mark: **")
                .Append(Escape(group.Package))
                .Append("** (")
                .Append(Escape(group.TargetFramework))
                .Append("): ");
            if (totals.Applicable == 0)
            {
                builder.Append("no applicable cases");
            }
            else
            {
                builder
                    .Append(totals.Passed.ToString(CultureInfo.InvariantCulture))
                    .Append(" of ")
                    .Append(totals.Applicable.ToString(CultureInfo.InvariantCulture))
                    .Append(" passed (")
                    .Append(Rate(totals))
                    .Append(')');
            }

            AppendCount(builder, totals.Regressions, "failed");
            AppendCount(builder, totals.UnexpectedPasses, "unexpectedly passed");
            AppendCount(builder, totals.Baselined, "pending");
            AppendCount(builder, totals.Skipped, "skipped");
            builder.Append('\n');

            if (failed && group.ListCases)
            {
                AppendCases(builder, group.Sets);
            }
        }

        return builder.ToString();
    }

    private static void AppendCount(StringBuilder builder, int count, string label)
    {
        if (count > 0)
        {
            builder.Append(", ").Append(count.ToString(CultureInfo.InvariantCulture)).Append(' ').Append(label);
        }
    }

    // One nested item per regression and per unexpected pass, by test set name; the cases above the limit are counted only.
    private static void AppendCases(StringBuilder builder, List<TestSetReport> sets)
    {
        sets.Sort(static (left, right) => string.CompareOrdinal(left.TestSet, right.TestSet));

        var cases = 0;
        foreach (var set in sets)
        {
            foreach (var id in set.Regressions)
            {
                if (++cases <= MaxListedCases)
                {
                    AppendCase(builder, set.TestSet, id, set.RegressionReasons.GetValueOrDefault(id) ?? NoReason);
                }
            }

            foreach (var id in set.UnexpectedPasses)
            {
                if (++cases <= MaxListedCases)
                {
                    AppendCase(builder, set.TestSet, id, UnexpectedPass);
                }
            }
        }

        if (cases > MaxListedCases)
        {
            builder
                .Append("  - and ")
                .Append((cases - MaxListedCases).ToString(CultureInfo.InvariantCulture))
                .Append(" more\n");
        }
    }

    private static void AppendCase(StringBuilder builder, string testSet, string id, string reason)
    {
        builder.Append("  - ");

        // A hierarchical id repeats its test set as the first segment; the test set is then not shown twice.
        if (!id.StartsWith(testSet + "/", StringComparison.Ordinal))
        {
            builder.Append('`').Append(Code(testSet)).Append("` ");
        }

        builder.Append('`').Append(Code(id)).Append("`: ").Append(Text(reason)).Append('\n');
    }

    // Integer math: floating point could turn 99.9 into 99.89999 or 100.0 through rounding.
    private static string Rate(Totals totals)
    {
        var tenths = (long)totals.Passed * 1000 / totals.Applicable;
        return FormattableString.Invariant($"{tenths / 10}.{tenths % 10}%");
    }

    private static int CompareGroups(Group left, Group right)
    {
        var package = string.CompareOrdinal(left.Package, right.Package);
        return package != 0 ? package : string.CompareOrdinal(left.TargetFramework, right.TargetFramework);
    }

    private static string Escape(string value) =>
        value
            .Replace("&", "&amp;", StringComparison.Ordinal)
            .Replace("<", "&lt;", StringComparison.Ordinal)
            .Replace("*", "\\*", StringComparison.Ordinal)
            .Replace("`", "\\`", StringComparison.Ordinal);

    // A code span takes its content literally and ends at the next backtick.
    private static string Code(string value) => value.Replace('`', '\'');

    // The first line of the message, cut to the limit, so that one case stays one line.
    private static string Text(string reason)
    {
        var line = reason.AsSpan().Trim();
        var end = line.IndexOfAny('\r', '\n');
        if (end >= 0)
        {
            line = line[..end].TrimEnd();
        }

        return line.Length > MaxReasonLength
            ? Escape(line[..MaxReasonLength].ToString()) + "..."
            : Escape(line.ToString());
    }

    private sealed class Group(string package, string targetFramework)
    {
        public string Package { get; } = package;

        public string TargetFramework { get; } = targetFramework;

        public bool ListCases { get; init; } = true;

        public List<TestSetReport> Sets { get; } = [];
    }

    // Failed cases split into baselined known failures (pending) and regressions; the rate counts both as failed.
    [StructLayout(LayoutKind.Auto)]
    private readonly record struct Totals(
        int Passed,
        int Baselined,
        int Regressions,
        int UnexpectedPasses,
        int Skipped
    )
    {
        public int Applicable => Passed + Baselined + Regressions;

        public Totals Add(TestSetReport set) =>
            new(
                Passed + set.Counts.Passed,
                Baselined + set.Counts.Failed - set.Regressions.Count,
                Regressions + set.Regressions.Count,
                UnexpectedPasses + set.UnexpectedPasses.Count,
                Skipped + set.Counts.Skipped
            );
    }
}
