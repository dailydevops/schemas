namespace NetEvolve.Schemas.XPath.Tests.Integration.Conformance;

using System.Globalization;
using System.Text;
using NetEvolve.Schemas.Conformance.Results;
using NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Runner;

/// <summary>
/// The total XPath 2.0 row of the job summary: passed, failed, skipped, excluded, applicable, and the rate. The rate is
/// passed / applicable, truncated to one decimal, and applicable is passed + failed; skipped cases are not applicable.
/// </summary>
internal static class Qt3Xp20Rate
{
    private static readonly object Gate = new();
    private static TestSetCounts s_collected;

    /// <summary>Adds the counts of one test set to the total.</summary>
    public static void Collect(TestSetCounts counts)
    {
        lock (Gate)
        {
            s_collected = new(
                s_collected.Passed + counts.Passed,
                s_collected.Failed + counts.Failed,
                s_collected.Skipped + counts.Skipped
            );
        }
    }

    /// <summary>Takes the collected counts and resets the total.</summary>
    public static TestSetCounts Take()
    {
        lock (Gate)
        {
            var counts = s_collected;
            s_collected = default;
            return counts;
        }
    }

    /// <summary>Appends the total row for the given counts; writes nothing when the path is empty or no case ran.</summary>
    public static void Flush(string? summaryPath, TestSetCounts counts, int excluded)
    {
        if (string.IsNullOrEmpty(summaryPath) || counts.Total == 0)
        {
            return;
        }

        File.AppendAllText(summaryPath, Render(counts, excluded), new UTF8Encoding(false));
    }

    /// <summary>Publishes the total row to the job summary file; does nothing when the variable is not set.</summary>
    public static async Task PublishAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var path = Environment.GetEnvironmentVariable("GITHUB_STEP_SUMMARY");
        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        var selection = await Qt3Suite.GetSelectionAsync(cancellationToken);
        Flush(path, Take(), selection.Excluded.Count);
    }

    /// <summary>Renders the table with the total row.</summary>
    public static string Render(TestSetCounts counts, int excluded) =>
        "| Suite | Passed | Failed | Skipped | Excluded | Applicable | Rate |\n"
        + "| --- | ---: | ---: | ---: | ---: | ---: | ---: |\n"
        + FormattableString.Invariant(
            $"| XPath 2.0 (QT3) | {counts.Passed} | {counts.Failed} | {counts.Skipped} | {excluded} | {counts.Applicable} | {Rate(counts)} |\n\n"
        );

    /// <summary>
    /// Returns whether the cases that ran (applicable plus skipped) equal the cases of the catalog minus the excluded cases,
    /// so that no selected case drops out of the rate unnoticed.
    /// </summary>
    public static bool IsConsistent(TestSetCounts counts, int excluded, int totalCases) =>
        counts.Total == totalCases - excluded;

    // Integer math: floating point could turn 99.9 into 99.89999 or 100.0 through rounding.
    private static string Rate(TestSetCounts counts)
    {
        if (counts.Applicable == 0)
        {
            return "n/a";
        }

        var tenths = (long)counts.Passed * 1000 / counts.Applicable;
        return FormattableString.Invariant($"{tenths / 10}.{tenths % 10}%");
    }
}
