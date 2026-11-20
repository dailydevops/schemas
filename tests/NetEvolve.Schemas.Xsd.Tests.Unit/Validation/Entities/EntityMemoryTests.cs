namespace NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Entities;

using System;
using System.Text;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Reader.Pipeline;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Core;
using NetEvolve.Schemas.Xsd.Validation.Core;
using NetEvolve.Schemas.Xsd.Validation.Entities;

public class EntityMemoryTests
{
    // The difference between one thousand and one million elements must stay far below one byte per element.
    private const long GrowthBound = 32 * 1024;

    private static string Document(string? subset, int count, string content)
    {
        var builder = new StringBuilder((count * content.Length) + 128);
        if (subset is not null)
        {
            _ = builder.Append("<!DOCTYPE r [").Append(subset).Append("]>\n");
        }

        _ = builder.Append("<r>");
        for (var i = 0; i < count; i++)
        {
            _ = builder.Append(content);
        }

        return builder.Append("</r>").ToString();
    }

    private static long Measure(SchemaFixture schema, string xml, out int issues)
    {
        var assessor = new ScriptedAssessor();
        var listener = new EntityListener(assessor);
        var collector = new ValidationIssueCollector(ValidationLimits.Default);
        var sink = new DoctypeSink(
            new StreamingValidator(
                schema.Model(),
                assessor,
                new XsdIssueReporter(collector),
                ValidationLimits.Default,
                ValidationHarness.Source,
                [listener]
            ),
            listener
        );
        using var reader = XmlPipelineReader.Create(xml.AsSpan(), XmlReaderOptions.Default.WithDtdProcessing(true));
        var before = GC.GetAllocatedBytesForCurrentThread();
        var status = reader.Pump(ref sink);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        issues = status == XmlReadStatus.Completed ? collector.ToResult().Issues.Count : -1;
        return allocated;
    }

    [Test]
    public async Task DocumentWithoutEntityTypedValues_AllocatesNoGrowthWithTheElementCount()
    {
        // Every value is typed as string, so the listener sees the values and checks nothing.
        var schema = EntityFixture.Schema(
            entity: SchemaFixture.Simple("string"),
            entities: SchemaFixture.Simple("string")
        );
        var small = Document(null, 1_000, "<e ent=\"x\"/><n>x</n>");
        var large = Document(null, 1_000_000, "<e ent=\"x\"/><n>x</n>");

        _ = Measure(schema, small, out _);
        var smallBytes = Measure(schema, small, out var smallIssues);
        var largeBytes = Measure(schema, large, out var largeIssues);

        await Assert.That(smallIssues).IsEqualTo(0);
        await Assert.That(largeIssues).IsEqualTo(0);
        await Assert.That(largeBytes - smallBytes).IsLessThan(GrowthBound);
    }

    [Test]
    public async Task DocumentWithDeclaredEntityValues_AllocatesNoGrowthWithTheValueCount()
    {
        // The lookup works on spans: a valid value names a known entity and costs no string.
        var schema = EntityFixture.Schema();
        const string Content = "<e ent=\"pic\" ents=\"pic logo\"/><n>logo</n><ns>pic logo</ns>";
        var small = Document(EntityFixture.Declarations, 1_000, Content);
        var large = Document(EntityFixture.Declarations, 1_000_000, Content);

        _ = Measure(schema, small, out _);
        var smallBytes = Measure(schema, small, out var smallIssues);
        var largeBytes = Measure(schema, large, out var largeIssues);

        await Assert.That(smallIssues).IsEqualTo(0);
        await Assert.That(largeIssues).IsEqualTo(0);
        await Assert.That(largeBytes - smallBytes).IsLessThan(GrowthBound);
    }
}
