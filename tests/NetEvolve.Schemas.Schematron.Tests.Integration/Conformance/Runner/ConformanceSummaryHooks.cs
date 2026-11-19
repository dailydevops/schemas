namespace NetEvolve.Schemas.Schematron.Tests.Integration.Conformance.Runner;

using NetEvolve.Schemas.Conformance.Reporting;

/// <summary>Writes the rate rows of all Schematron test sets as one table to the job summary.</summary>
internal static class ConformanceSummaryHooks
{
    /// <summary>Flushes the collected rate rows after the last test of the session.</summary>
    [After(TestSession)]
    public static void PublishConformanceRate() => ConformanceRateReporter.Flush();
}
