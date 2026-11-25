namespace NetEvolve.Schemas.Schematron.Tests.Unit.Svrl;

using System.Buffers;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using NetEvolve.Schemas.Schematron.Svrl;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Pipeline.PipelineFixture;

/// <summary>
/// The SVRL writer carries the <c>fpi</c>, <c>icon</c>, and <c>see</c> of a referenced diagnostic or property on the
/// <c>svrl:text</c> of its reference (#916). These mirror the six <c>iso-reporting/svrl-{diagnostic,property}-{fpi,icon,see}</c> cases.
/// </summary>
public class SvrlReferenceAttributeTests
{
    private static readonly XNamespace Svrl = "http://purl.oclc.org/dsdl/svrl";

    private static XElement Text(string attributes, string reference)
    {
        var output = new ArrayBufferWriter<byte>();
        using (var sink = new SvrlReportSink(output))
        {
            _ = Validate(
                Build(
                        "<schema xmlns=\"http://purl.oclc.org/dsdl/schematron\" schematronEdition=\"2025\"><pattern><rule context=\"a\">"
                            + "<report test=\"true()\" properties=\"p1\" diagnostics=\"d1\"/></rule></pattern>"
                            + $"<properties><property id=\"p1\"{attributes}>x</property></properties>"
                            + $"<diagnostics><diagnostic id=\"d1\"{attributes}>y</diagnostic></diagnostics></schema>"
                    )
                    .WithReportSink(sink),
                "<a/>"
            );
        }

        return XDocument
            .Parse(SvrlRuns.Text(output.WrittenSpan.ToArray()))
            .Descendants(Svrl + reference)
            .Single()
            .Element(Svrl + "text")!;
    }

    [Test]
    [Arguments("diagnostic-reference", "fpi")]
    [Arguments("diagnostic-reference", "icon")]
    [Arguments("diagnostic-reference", "see")]
    [Arguments("property-reference", "fpi")]
    [Arguments("property-reference", "icon")]
    [Arguments("property-reference", "see")]
    public async Task Text_WritesTheAttribute(string reference, string name)
    {
        var text = Text($" {name}=\"#target\"", reference);

        await Assert.That((string?)text.Attribute(name)).IsEqualTo("#target");
    }

    [Test]
    [Arguments("diagnostic-reference")]
    [Arguments("property-reference")]
    public async Task Text_WithoutTheAttributes_WritesNone(string reference)
    {
        var text = Text(string.Empty, reference);

        await Assert
            .That(text.Attribute("fpi") is null && text.Attribute("icon") is null && text.Attribute("see") is null)
            .IsTrue();
    }
}
