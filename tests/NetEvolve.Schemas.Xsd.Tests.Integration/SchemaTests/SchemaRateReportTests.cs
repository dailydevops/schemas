namespace NetEvolve.Schemas.Xsd.Tests.Integration.SchemaTests;

using NetEvolve.Schemas.Conformance.Results;
using NetEvolve.Schemas.Xsd.Tests.Integration.Differential;
using NetEvolve.Schemas.Xsd.Tests.Integration.Xsts;
using static NetEvolve.Schemas.Xsd.Tests.Integration.SchemaTests.SchemaRateReport;

/// <summary>The job summary gets a row for the schema tests of XSD 1.0 alone, beside the row of the whole suite.</summary>
public class SchemaRateReportTests
{
    private static List<CaseResult> Results(string testSet) =>
        [
            new("1.0/g/schema-pass", testSet, CaseOutcome.Pass),
            new("1.0/g/schema-fail", testSet, CaseOutcome.Fail, "Expected Valid but observed Invalid."),
            new("1.0/g/instance", testSet, CaseOutcome.Fail, "Expected Valid but observed NotImplemented."),
        ];

    [Test]
    public async Task Publish_CountsOnlyTheSchemaTestsOfXsd10(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var directory = new TempDirectory();
        var summary = directory.Path + "-summary.md";
        var schemaCases = new HashSet<string>(StringComparer.Ordinal)
        {
            KeyOf("ibmMeta-date", "1.0/g/schema-pass"),
            KeyOf("ibmMeta-date", "1.0/g/schema-fail"),
        };

        try
        {
            Publish(Results("ibmMeta-date"), schemaCases, summary);
            var text = await File.ReadAllTextAsync(summary, cancellationToken);

            await Assert.That(text).Contains(Package);
            await Assert.That(text).Contains("50.0%");
        }
        finally
        {
            File.Delete(summary);
        }
    }

    [Test]
    public async Task Publish_WithoutASummaryFileOrWithoutASchemaTest_WritesNothing(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var directory = new TempDirectory();
        var summary = directory.Path + "-summary.md";
        var none = new HashSet<string>(StringComparer.Ordinal);

        Publish(Results("ibmMeta-date"), none, null);
        Publish(Results("ibmMeta-date"), none, summary);

        await Assert.That(File.Exists(summary)).IsFalse();
        await Assert.That(Package).StartsWith(XstsRateReport.Package);
    }

    [Test]
    public async Task Publish_GuardsItsArguments()
    {
        await Assert
            .That(() => Publish(null!, new HashSet<string>(StringComparer.Ordinal), null))
            .Throws<ArgumentNullException>();
        await Assert.That(() => Publish([], null!, null)).Throws<ArgumentNullException>();
    }
}
