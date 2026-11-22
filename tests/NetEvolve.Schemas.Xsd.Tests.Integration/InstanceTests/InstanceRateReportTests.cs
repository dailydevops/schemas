namespace NetEvolve.Schemas.Xsd.Tests.Integration.InstanceTests;

using NetEvolve.Schemas.Conformance.Results;
using NetEvolve.Schemas.Xsd.Tests.Integration.Differential;
using NetEvolve.Schemas.Xsd.Tests.Integration.SchemaTests;
using NetEvolve.Schemas.Xsd.Tests.Integration.Xsts;

/// <summary>The job summary gets a row for the instance tests of XSD 1.0 alone, beside the rows of the whole suite and of the schema tests.</summary>
public class InstanceRateReportTests
{
    [Test]
    public async Task Publish_CountsOnlyTheInstanceTestsOfXsd10UnderTheirOwnLabel(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var directory = new TempDirectory();
        var summary = directory.Path + "-summary.md";
        List<CaseResult> results =
        [
            new("1.0/g/instance-pass", "ibmMeta-date", CaseOutcome.Pass),
            new("1.0/g/instance-fail", "ibmMeta-date", CaseOutcome.Fail, "Expected Valid but observed Invalid."),
            new("1.0/g/schema", "ibmMeta-date", CaseOutcome.Fail, "Expected Valid but observed Invalid."),
        ];
        var instanceCases = new HashSet<string>(StringComparer.Ordinal)
        {
            SchemaRateReport.KeyOf("ibmMeta-date", "1.0/g/instance-pass"),
            SchemaRateReport.KeyOf("ibmMeta-date", "1.0/g/instance-fail"),
        };

        try
        {
            SchemaRateReport.Publish(results, instanceCases, summary, InstanceRateReport.Package);
            var text = await File.ReadAllTextAsync(summary, cancellationToken);

            await Assert.That(text).Contains(InstanceRateReport.Package);
            await Assert.That(text).Contains("50.0%");
        }
        finally
        {
            File.Delete(summary);
        }
    }

    [Test]
    public async Task Package_IsALabelOfItsOwnBesideTheSchemaTestsAndTheSuite()
    {
        await Assert.That(InstanceRateReport.Package).StartsWith(XstsRateReport.Package);
        await Assert.That(InstanceRateReport.Package).IsNotEqualTo(SchemaRateReport.Package);
        await Assert.That(InstanceRateReport.Package).EndsWith("instance tests");
    }
}
