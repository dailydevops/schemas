namespace NetEvolve.Schemas.Xml.Tests.Integration.Libxml2;

using NetEvolve.Schemas.Conformance.Baseline;
using NetEvolve.Schemas.Conformance.Results;

/// <summary>Compares the results of one libxml2 sub-tree with its committed baselines and regenerates them.</summary>
internal static class Libxml2BaselineSet
{
    /// <summary>Compares a run with the baselines in a directory.</summary>
    /// <param name="directory">The baseline directory.</param>
    /// <param name="results">The results of the run.</param>
    /// <returns>The report.</returns>
    public static ConformanceReport Compare(string directory, IReadOnlyList<CaseResult> results)
    {
        ArgumentNullException.ThrowIfNull(results);
        var baselines = results
            .Select(r => r.TestSet)
            .Distinct(StringComparer.Ordinal)
            .ToDictionary(set => set, set => BaselineFiles.Load(directory, set), StringComparer.Ordinal);
        return ConformanceReport.Compare(results, baselines);
    }

    /// <summary>Rewrites the baseline file of every test set from a run.</summary>
    /// <param name="directory">The baseline directory.</param>
    /// <param name="results">The results of the run.</param>
    public static void Regenerate(string directory, IReadOnlyList<CaseResult> results)
    {
        ArgumentNullException.ThrowIfNull(results);
        _ = Directory.CreateDirectory(directory);
        foreach (var set in results.Select(r => r.TestSet).Distinct(StringComparer.Ordinal))
        {
            var failures = results
                .Where(r => string.Equals(r.TestSet, set, StringComparison.Ordinal) && r.Outcome == CaseOutcome.Fail)
                .Select(r => r.CaseId)
                .ToList();
            if (failures.Count == 0)
            {
                File.Delete(BaselineFiles.PathFor(directory, set));
            }
            else
            {
                BaselineFiles.Save(directory, set, failures);
            }
        }
    }

    /// <summary>Describes a report for an assertion message.</summary>
    /// <param name="report">The report.</param>
    /// <returns>One line per test set.</returns>
    public static string Describe(ConformanceReport report)
    {
        ArgumentNullException.ThrowIfNull(report);
        return string.Join(
            Environment.NewLine,
            report.TestSets.Select(s =>
                $"{s.TestSet}: passed {s.Counts.Passed}, failed {s.Counts.Failed}, skipped {s.Counts.Skipped}; regressions [{string.Join(", ", s.Regressions)}]; unexpected passes [{string.Join(", ", s.UnexpectedPasses)}]"
            )
        );
    }
}
