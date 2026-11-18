namespace NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Core;

using System;
using System.Text;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Reader.Pipeline;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.Validation.Core;

public class MemoryTests
{
    private const int Siblings = 1_000_000;

    // The reader and the validator together allocate a small fixed amount per run; the bound is far below one byte per element
    // (one million elements would add at least one megabyte).
    private const long GrowthBound = 32 * 1024;

    private static string Document(int count)
    {
        var builder = new StringBuilder(count * 22 + 16);
        _ = builder.Append("<r>");
        for (var i = 0; i < count; i++)
        {
            _ = builder.Append("<a i=\"1\"><b>x</b></a>");
        }

        return builder.Append("</r>").ToString();
    }

    private static SchemaFixture Schema()
    {
        var schema = new SchemaFixture();
        var item = schema.ElementOnly(
            SchemaFixture.Seq(SchemaFixture.Part("b", SchemaFixture.Simple("string"))),
            schema.Use("i", "int")
        );
        schema.Global("r", schema.ElementOnly(SchemaFixture.Seq(SchemaFixture.Part("a", item, 0, long.MaxValue))));
        return schema;
    }

    private static long Measure(
        StreamingValidator validator,
        string xml,
        out int issues,
        ValidationIssueCollector collector
    )
    {
        using var reader = XmlPipelineReader.Create(xml.AsSpan());
        var before = GC.GetAllocatedBytesForCurrentThread();
        var status = reader.Pump(ref validator);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        issues = status == XmlReadStatus.Completed ? collector.ToResult().Issues.Count : -1;
        return allocated;
    }

    private static StreamingValidator NewValidator(SchemaFixture schema, ValidationIssueCollector collector) =>
        new(
            schema.Model(),
            new ScriptedAssessor(),
            new XsdIssueReporter(collector),
            ValidationLimits.Default,
            ValidationHarness.Source,
            []
        );

    [Test]
    public async Task OneMillionSiblings_AllocateWithinAFixedBound_IndependentOfTheElementCount()
    {
        var schema = Schema();
        var small = Document(1_000);
        var large = Document(Siblings);

        // Warm up so that one-time costs (JIT, pools, name table growth) are outside the comparison.
        var warmCollector = new ValidationIssueCollector(ValidationLimits.Default);
        _ = Measure(NewValidator(schema, warmCollector), small, out _, warmCollector);

        var smallCollector = new ValidationIssueCollector(ValidationLimits.Default);
        var smallBytes = Measure(NewValidator(schema, smallCollector), small, out var smallIssues, smallCollector);

        var largeCollector = new ValidationIssueCollector(ValidationLimits.Default);
        var largeBytes = Measure(NewValidator(schema, largeCollector), large, out var largeIssues, largeCollector);

        await Assert.That(smallIssues).IsEqualTo(0);
        await Assert.That(largeIssues).IsEqualTo(0);
        await Assert.That(largeBytes - smallBytes).IsLessThan(GrowthBound);
        await Assert.That(largeBytes).IsLessThan(128 * 1024);
    }
}
