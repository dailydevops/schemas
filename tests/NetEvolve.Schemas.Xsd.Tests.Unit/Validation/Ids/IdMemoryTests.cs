namespace NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Ids;

using System;
using System.Text;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Reader.Pipeline;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Core;
using NetEvolve.Schemas.Xsd.Validation.Core;

public class IdMemoryTests
{
    // The difference between one thousand and one million elements must stay far below one byte per element.
    private const long GrowthBound = 32 * 1024;

    private static string Document(int count)
    {
        var builder = new StringBuilder((count * 14) + 16);
        _ = builder.Append("<r>");
        for (var i = 0; i < count; i++)
        {
            _ = builder.Append("<e note=\"x\"/><n>x</n>");
        }

        return builder.Append("</r>").ToString();
    }

    private static long Measure(SchemaFixture schema, string xml, out int issues)
    {
        var collector = new ValidationIssueCollector(ValidationLimits.Default);
        var validator = new StreamingValidator(
            schema.Model(),
            new ScriptedAssessor(),
            new XsdIssueReporter(collector),
            ValidationLimits.Default,
            ValidationHarness.Source,
            [IdFixture.Listener(new ScriptedAssessor(), "x")]
        );
        using var reader = XmlPipelineReader.Create(xml.AsSpan());
        var before = GC.GetAllocatedBytesForCurrentThread();
        var status = reader.Pump(ref validator);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        issues = status == XmlReadStatus.Completed ? collector.ToResult().Issues.Count : -1;
        return allocated;
    }

    [Test]
    public async Task DocumentWithoutIdTypedValues_AllocatesNoGrowthWithTheElementCount()
    {
        // Every value is typed as string, so the listener sees the values and stores nothing.
        var schema = IdFixture.Schema(
            id: SchemaFixture.Simple("string"),
            idref: SchemaFixture.Simple("string"),
            idrefs: SchemaFixture.Simple("string"),
            notation: SchemaFixture.Simple("string")
        );
        var small = Document(1_000);
        var large = Document(1_000_000);

        _ = Measure(schema, small, out _);
        var smallBytes = Measure(schema, small, out var smallIssues);
        var largeBytes = Measure(schema, large, out var largeIssues);

        await Assert.That(smallIssues).IsEqualTo(0);
        await Assert.That(largeIssues).IsEqualTo(0);
        await Assert.That(largeBytes - smallBytes).IsLessThan(GrowthBound);
    }

    [Test]
    public async Task DocumentWithOnlyResolvedReferences_AllocatesNoGrowthWithTheReferenceCount()
    {
        // One ID and many references to it: a reference to a known ID waits for nothing, so nothing is stored per reference.
        var schema = IdFixture.Schema();
        var small = ReferencesTo("a", 1_000);
        var large = ReferencesTo("a", 1_000_000);

        _ = Measure(schema, small, out _);
        var smallBytes = Measure(schema, small, out var smallIssues);
        var largeBytes = Measure(schema, large, out var largeIssues);

        await Assert.That(smallIssues).IsEqualTo(0);
        await Assert.That(largeIssues).IsEqualTo(0);
        await Assert.That(largeBytes - smallBytes).IsLessThan(GrowthBound);
    }

    private static string ReferencesTo(string id, int count)
    {
        var builder = new StringBuilder((count * 14) + 32);
        _ = builder.Append("<r><e id=\"").Append(id).Append("\"/>");
        for (var i = 0; i < count; i++)
        {
            _ = builder.Append("<e ref=\"").Append(id).Append("\"/>");
        }

        return builder.Append("</r>").ToString();
    }
}
