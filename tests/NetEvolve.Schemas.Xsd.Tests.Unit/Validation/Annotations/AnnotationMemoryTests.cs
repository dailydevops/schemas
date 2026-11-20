namespace NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Annotations;

using System;
using System.Text;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Reader.Pipeline;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Core;
using NetEvolve.Schemas.Xsd.Validation.Annotations;
using NetEvolve.Schemas.Xsd.Validation.Core;

public class AnnotationMemoryTests
{
    private const int Count = 3000;

    private static string Document()
    {
        var builder = new StringBuilder(Count * 12 + 16);
        _ = builder.Append("<r>");
        for (var i = 0; i < Count; i++)
        {
            _ = builder.Append("<i>1</i>");
        }

        return builder.Append("</r>").ToString();
    }

    private static long Measure(SchemaFixture schema, string xml, params ValidationListener[] listeners)
    {
        var limits = ValidationLimits.Default;
        var validator = new StreamingValidator(
            schema.Model(),
            new DescriptorValueAssessor(XsdVersion.V11),
            new XsdIssueReporter(new ValidationIssueCollector(limits)),
            limits,
            ValidationHarness.Source,
            listeners
        );
        using var reader = XmlPipelineReader.Create(xml.AsSpan());
        var before = GC.GetAllocatedBytesForCurrentThread();
        var status = reader.Pump(ref validator);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        return status == XmlReadStatus.Completed ? allocated : -1;
    }

    [Test]
    public async Task Annotation_PlainRun_WithoutAssertions_AllocatesNoTable()
    {
        var schema = AnnotationFixture.Schema();
        var xml = Document();
        _ = Measure(schema, xml);

        var plain = Measure(schema, xml);
        var withoutSink = Measure(schema, xml, new AnnotationListener(null));
        var table = new AnnotationTable();
        var withSink = Measure(schema, xml, new AnnotationListener(table));

        // Without a sink the listener asks for no hook, so the run allocates what a run without the listener allocates.
        await Assert.That(Math.Abs(withoutSink - plain)).IsLessThan(1024);
        await Assert.That(withSink - plain).IsGreaterThan(Count * 16L);
        await Assert.That(table.ElementOrdinalLimit).IsEqualTo(Count + 1);
    }
}
