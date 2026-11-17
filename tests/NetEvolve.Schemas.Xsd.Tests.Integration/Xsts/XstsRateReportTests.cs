namespace NetEvolve.Schemas.Xsd.Tests.Integration.Xsts;

using NetEvolve.Schemas.Conformance.Reporting;
using NetEvolve.Schemas.Conformance.Results;

public class XstsRateReportTests
{
    [Test]
    public async Task Publish_ChunkResultsOfOneSet_AppendsOneAggregatedRow(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var summary = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        try
        {
            CaseResult[] results =
            [
                new("a", "ibmMeta-allGroup", CaseOutcome.Pass),
                new("b", "ibmMeta-allGroup", CaseOutcome.Pass),
                new("c", "ibmMeta-allGroup", CaseOutcome.Skipped, "x"),
            ];

            XstsRateReport.Publish(results, summary);

            var tfm = TargetFrameworkName.Of(typeof(XstsRateReportTests).Assembly);
            await Assert
                .That(await File.ReadAllTextAsync(summary, cancellationToken))
                .Contains($"| NetEvolve.Schemas.Xsd | {tfm} | ibmMeta-allGroup | 2 | 2 | 0 | 1 | 100.0% |\n");
        }
        finally
        {
            File.Delete(summary);
        }
    }

    [Test]
    public async Task Publish_NoSummaryPath_WritesNothing(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        XstsRateReport.Publish([new("a", "ibmMeta-allGroup", CaseOutcome.Pass)], null);

        await Assert.That(XstsRateReport.Package).IsEqualTo("NetEvolve.Schemas.Xsd");
    }
}
