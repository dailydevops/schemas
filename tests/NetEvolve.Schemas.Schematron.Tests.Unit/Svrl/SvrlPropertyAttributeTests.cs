namespace NetEvolve.Schemas.Schematron.Tests.Unit.Svrl;

using System.Buffers;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using NetEvolve.Schemas.Schematron.Svrl;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Pipeline.PipelineFixture;

/// <summary>
/// The SVRL writer carries the <c>role</c> and <c>scheme</c> of a referenced property on <c>svrl:property-reference</c> (#918). These
/// mirror the <c>iso-reporting/svrl-property-role</c> and <c>svrl-property-scheme</c> cases of the Schematron conformance suite.
/// </summary>
public class SvrlPropertyAttributeTests
{
    private static readonly XNamespace Svrl = "http://purl.oclc.org/dsdl/svrl";

    private static XElement Reference(string attributes, string reference = "property-reference")
    {
        var output = new ArrayBufferWriter<byte>();
        using (var sink = new SvrlReportSink(output))
        {
            _ = Validate(
                Build(
                        "<schema xmlns=\"http://purl.oclc.org/dsdl/schematron\" schematronEdition=\"2025\"><pattern><rule context=\"a\">"
                            + "<report test=\"true()\" properties=\"p1\" diagnostics=\"d1\"/></rule></pattern>"
                            + $"<properties><property id=\"p1\"{attributes}>x</property></properties>"
                            + "<diagnostics><diagnostic id=\"d1\" role=\"drole\">y</diagnostic></diagnostics></schema>"
                    )
                    .WithReportSink(sink),
                "<a/>"
            );
        }

        return XDocument.Parse(SvrlRuns.Text(output.WrittenSpan.ToArray())).Descendants(Svrl + reference).Single();
    }

    [Test]
    public async Task Property_WritesItsRole()
    {
        var element = Reference(" role=\"info\"");

        await Assert.That((string?)element.Attribute("role")).IsEqualTo("info");
    }

    [Test]
    public async Task Property_WritesItsScheme()
    {
        var element = Reference(" scheme=\"s1\"");

        await Assert.That((string?)element.Attribute("scheme")).IsEqualTo("s1");
    }

    [Test]
    public async Task Property_WithoutRoleAndScheme_WritesNeither()
    {
        var element = Reference(string.Empty);

        await Assert.That(element.Attribute("role") is null && element.Attribute("scheme") is null).IsTrue();
    }

    [Test]
    public async Task DiagnosticReference_WritesNoRole()
    {
        var element = Reference(string.Empty, "diagnostic-reference");

        await Assert.That(element.Attribute("role")).IsNull();
    }
}
