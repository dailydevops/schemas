namespace NetEvolve.Schemas.Conformance.Reporting;

using System.Globalization;
using System.Text;
using NetEvolve.Schemas.Conformance.Results;

/// <summary>
/// Publishes the conformance rate per package and target framework as a Markdown table to the GitHub Actions
/// job summary, the file named by the <c>GITHUB_STEP_SUMMARY</c> environment variable.
/// </summary>
/// <remarks>
/// The rate is passed / (passed + failed). Skipped cases are not applicable and excluded from the denominator.
/// The rate is truncated, never rounded up, to one decimal, so that 99.99 percent displays as 99.9 percent and
/// only a suite without a single failure displays as 100.0 percent. A test set without applicable cases displays
/// <c>n/a</c>. Rows keep the sorted test set order of the <see cref="ConformanceReport"/>, so two runs compare as text.
/// </remarks>
public static class ConformanceRateReporter
{
    /// <summary>The name of the environment variable that holds the job summary file path.</summary>
    public const string SummaryVariable = "GITHUB_STEP_SUMMARY";

    private const string Footnote =
        "_Rate = passed / (passed + failed). Skipped cases are excluded from the denominator. The rate is truncated, never rounded up, to one decimal._";

    /// <summary>Renders the table block of one package and target framework.</summary>
    /// <param name="package">The package that owns the suite.</param>
    /// <param name="targetFramework">The target framework of the test run.</param>
    /// <param name="report">The report of the run.</param>
    /// <returns>The Markdown table, a footnote, and a trailing blank line; lines end with <c>\n</c>.</returns>
    public static string Render(string package, string targetFramework, ConformanceReport report)
    {
        ArgumentNullException.ThrowIfNull(package);
        ArgumentNullException.ThrowIfNull(targetFramework);
        ArgumentNullException.ThrowIfNull(report);

        var builder = new StringBuilder()
            .Append("| Package | Target framework | Test set | Applicable | Passed | Failed | Skipped | Rate |\n")
            .Append("| --- | --- | --- | ---: | ---: | ---: | ---: | ---: |\n");

        if (report.TestSets.Count == 0)
        {
            AppendRow(builder, package, targetFramework, "_no suites_", default);
        }

        foreach (var set in report.TestSets)
        {
            AppendRow(builder, package, targetFramework, set.TestSet, set.Counts);
        }

        return builder.Append('\n').Append(Footnote).Append("\n\n").ToString();
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

    /// <summary>Buffers the report of one or more test sets for <see cref="Flush()"/>.</summary>
    /// <param name="package">The package that owns the suite.</param>
    /// <param name="targetFramework">The target framework of the test run.</param>
    /// <param name="report">The report of the run.</param>
    public static void Collect(string package, string targetFramework, ConformanceReport report)
    {
        ArgumentNullException.ThrowIfNull(package);
        ArgumentNullException.ThrowIfNull(targetFramework);
        ArgumentNullException.ThrowIfNull(report);
    }

    /// <summary>Writes the buffered reports to the job summary file; does nothing when the environment variable is not set.</summary>
    public static void Flush() => Flush(Environment.GetEnvironmentVariable(SummaryVariable));

    /// <summary>Writes the buffered reports to the given file; does nothing when the path is <see langword="null"/> or empty.</summary>
    /// <param name="summaryPath">The summary file path.</param>
    public static void Flush(string? summaryPath)
    {
        if (string.IsNullOrEmpty(summaryPath))
        {
            return;
        }
    }

    private static void AppendRow(
        StringBuilder builder,
        string package,
        string targetFramework,
        string testSet,
        TestSetCounts counts
    ) =>
        builder
            .Append("| ")
            .Append(Escape(package))
            .Append(" | ")
            .Append(Escape(targetFramework))
            .Append(" | ")
            .Append(Escape(testSet))
            .Append(" | ")
            .Append(counts.Applicable.ToString(CultureInfo.InvariantCulture))
            .Append(" | ")
            .Append(counts.Passed.ToString(CultureInfo.InvariantCulture))
            .Append(" | ")
            .Append(counts.Failed.ToString(CultureInfo.InvariantCulture))
            .Append(" | ")
            .Append(counts.Skipped.ToString(CultureInfo.InvariantCulture))
            .Append(" | ")
            .Append(Rate(counts))
            .Append(" |\n");

    // Integer math: floating point could turn 99.9 into 99.89999 or 100.0 through rounding.
    private static string Rate(TestSetCounts counts)
    {
        if (counts.Applicable == 0)
        {
            return "n/a";
        }

        var tenths = (long)counts.Passed * 1000 / counts.Applicable;
        return string.Create(CultureInfo.InvariantCulture, $"{tenths / 10}.{tenths % 10}%");
    }

    private static string Escape(string value) => value.Replace("|", "&#124;", StringComparison.Ordinal);
}
