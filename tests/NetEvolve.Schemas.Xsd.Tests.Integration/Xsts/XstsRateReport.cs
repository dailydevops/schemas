namespace NetEvolve.Schemas.Xsd.Tests.Integration.Xsts;

using NetEvolve.Schemas.Conformance.Baseline;
using NetEvolve.Schemas.Conformance.Reporting;
using NetEvolve.Schemas.Conformance.Results;

/// <summary>Publishes the XSTS conformance rate through the shared reporter.</summary>
internal static class XstsRateReport
{
    /// <summary>The package that owns the XSTS suite.</summary>
    public const string Package = "NetEvolve.Schemas.Xsd";

    /// <summary>Compares the results against the committed baselines and appends the rate rows to the job summary.</summary>
    /// <param name="results">The case results of all chunks of the run.</param>
    /// <param name="summaryPath">The summary file path; nothing is written when it is <see langword="null"/> or empty.</param>
    public static void Publish(IReadOnlyCollection<CaseResult> results, string? summaryPath)
    {
        if (string.IsNullOrEmpty(summaryPath) || results.Count == 0)
        {
            return;
        }

        var baselines = new Dictionary<string, KnownFailureBaseline>(StringComparer.Ordinal);
        foreach (var result in results)
        {
            if (!baselines.ContainsKey(result.TestSet))
            {
                baselines.Add(result.TestSet, BaselineFiles.Load(XstsSuite.BaselineDirectory, result.TestSet));
            }
        }

        ConformanceRateReporter.Append(
            Package,
            TargetFrameworkName.Of(typeof(XstsRateReport).Assembly),
            ConformanceReport.Compare(results, baselines),
            summaryPath
        );
    }

    /// <summary>Publishes to the job summary file named by the environment variable of the shared reporter.</summary>
    /// <param name="results">The case results of all chunks of the run.</param>
    public static void Publish(IReadOnlyCollection<CaseResult> results) =>
        Publish(results, Environment.GetEnvironmentVariable(ConformanceRateReporter.SummaryVariable));
}
