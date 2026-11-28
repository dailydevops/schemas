namespace NetEvolve.Schemas.XPath.Tests.Integration.Conformance;

using NetEvolve.Schemas.Conformance.Reporting;

/// <summary>Writes the rate row of the QT3 run to the job summary.</summary>
internal static class ConformanceSummaryHooks
{
    /// <summary>Flushes the collected rate rows after the last test of the session.</summary>
    [After(TestSession)]
    public static void PublishConformanceRate() => ConformanceRateReporter.Flush();
}
