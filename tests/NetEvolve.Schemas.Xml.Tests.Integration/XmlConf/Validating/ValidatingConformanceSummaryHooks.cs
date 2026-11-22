namespace NetEvolve.Schemas.Xml.Tests.Integration.XmlConf.Validating;

using NetEvolve.Schemas.Conformance.Reporting;

/// <summary>Writes the rate row of the validating reader mode to the job summary.</summary>
internal static class ValidatingConformanceSummaryHooks
{
    /// <summary>Flushes the collected rate rows after the last test of the session.</summary>
    [After(TestSession)]
    public static void PublishConformanceRate() => ConformanceRateReporter.Flush();
}
