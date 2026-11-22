namespace NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Validation.Pipeline;

using NetEvolve.Schemas;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Testing.Allocations;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Model;
using NetEvolve.Schemas.Xml.Dtd.Validation.Pipeline;
using NetEvolve.Schemas.Xml.Reader.Pipeline;
using static NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Validation.Pipeline.PipelineHarness;

/// <summary>The decorating sink allocates nothing per event once its buffers and name cache are warm.</summary>
public class PipelineAllocationTests
{
    private const int WarmupRuns = 5;
    private const int MeasuredRuns = 5;

    [Test]
    public async Task Sink_SteadyState_AllocatesNothingPerElementTextAndAttribute()
    {
        using var reader = XmlPipelineReader.Create("<r/>".AsSpan());
        var sink = new DtdValidatingSink<DtdDiscardSink>(
            reader,
            new DtdDiscardSink(XmlReaderLimits.Default),
            [new NoOpStep(), new NoOpStep()],
            DtdValidationOptions.Default,
            new ValidationIssueCollector(ValidationLimits.Default),
            null
        );
        var element = sink.Names.Intern("p", "element", "urn:p");
        var attribute = new ValidationAttributeInfo(
            "k".AsMemory(),
            "some value".AsMemory(),
            DtdAttributeType.Cdata,
            true,
            new XmlPosition(1, 1, 0)
        );
        var at = new XmlPosition(1, 1, 0);

        var allocated = AllocationAssert.Measure(
            () =>
            {
                for (var i = 0; i < 20_000; i++)
                {
                    sink.StartElement(element, at);
                    sink.AttributeInfo(in attribute);
                    sink.EndAttributes();
                    sink.Text("text".AsSpan(), XmlTextKind.Text);
                    sink.Comment("c".AsSpan());
                    sink.EndElement(element);
                }
            },
            WarmupRuns,
            MeasuredRuns
        );

        await Assert.That(allocated).IsEqualTo(0L);
    }
}
