namespace NetEvolve.Schemas.XPath.Tests.Integration.Conformance;

using NetEvolve.Schemas.Conformance.Reporting;

/// <summary>Writes the rate rows of all QT3 test sets as one table, and the total XPath 2.0 row, to the job summary.</summary>
internal static class ConformanceSummaryHooks
{
    /// <summary>Flushes the collected rate rows after the last test of the session.</summary>
    [After(TestSession)]
    public static async Task PublishConformanceRate(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ConformanceRateReporter.Flush();
        await Qt3Xp20Rate.PublishAsync(cancellationToken);
    }
}
