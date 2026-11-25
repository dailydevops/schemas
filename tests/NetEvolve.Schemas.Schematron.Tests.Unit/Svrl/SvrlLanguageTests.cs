namespace NetEvolve.Schemas.Schematron.Tests.Unit.Svrl;

using System.Buffers;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using NetEvolve.Schemas.Schematron.Svrl;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Pipeline.PipelineFixture;

/// <summary>
/// The SVRL writer carries the language in effect (#917): the nearest <c>xml:lang</c> of the assertion and its ancestors on the finding,
/// and the language of a referenced diagnostic or property on its <c>svrl:text</c>. These mirror the six <c>iso-reporting</c> cases of
/// the Schematron conformance suite that assert <c>lang('de')</c> on the SVRL.
/// </summary>
public class SvrlLanguageTests
{
    private static readonly XNamespace Svrl = "http://purl.oclc.org/dsdl/svrl";
    private static readonly XName Lang = XNamespace.Xml + "lang";

    private static XElement Run(string schema, string instance = "<a/>")
    {
        var output = new ArrayBufferWriter<byte>();
        using (var sink = new SvrlReportSink(output))
        {
            _ = Validate(Build(schema).WithReportSink(sink), instance);
        }

        return XDocument.Parse(SvrlRuns.Text(output.WrittenSpan.ToArray())).Root!;
    }

    [Test]
    public async Task FailedAssert_InheritsTheLanguageOfTheRule()
    {
        var root = Run(
            "<schema xmlns=\"http://purl.oclc.org/dsdl/schematron\" xml:lang=\"en\"><pattern><rule context=\"a\" xml:lang=\"de\"><assert test=\"false()\"/></rule></pattern></schema>"
        );

        await Assert.That((string?)root.Descendants(Svrl + "failed-assert").Single().Attribute(Lang)).IsEqualTo("de");
    }

    [Test]
    public async Task SuccessfulReport_InheritsTheLanguageOfTheSchema()
    {
        var root = Run(
            "<schema xmlns=\"http://purl.oclc.org/dsdl/schematron\" xml:lang=\"en\"><pattern><rule context=\"a\"><report test=\"true()\"/></rule></pattern></schema>"
        );

        await Assert
            .That((string?)root.Descendants(Svrl + "successful-report").Single().Attribute(Lang))
            .IsEqualTo("en");
    }

    [Test]
    public async Task Assertion_WithoutLanguage_WritesNone()
    {
        var root = Run(
            "<schema xmlns=\"http://purl.oclc.org/dsdl/schematron\"><pattern><rule context=\"a\"><assert test=\"false()\"/></rule></pattern></schema>"
        );

        await Assert.That(root.Descendants().Any(e => e.Attribute(Lang) is not null)).IsFalse();
    }

    [Test]
    public async Task EmptyLanguage_ResetsTheInheritedOne()
    {
        var root = Run(
            "<schema xmlns=\"http://purl.oclc.org/dsdl/schematron\" xml:lang=\"en\"><pattern><rule context=\"a\" xml:lang=\"\"><assert test=\"false()\"/></rule></pattern></schema>"
        );

        await Assert.That(root.Descendants(Svrl + "failed-assert").Single().Attribute(Lang)).IsNull();
    }

    [Test]
    public async Task Diagnostic_WritesItsLanguageOnTheText()
    {
        var root = Run(
            "<schema xmlns=\"http://purl.oclc.org/dsdl/schematron\"><pattern><rule context=\"a\"><report test=\"true()\" diagnostics=\"d1\"/></rule></pattern><diagnostics><diagnostic id=\"d1\" xml:lang=\"de\">Context: <value-of select=\"name()\"/></diagnostic></diagnostics></schema>"
        );

        var text = root.Descendants(Svrl + "diagnostic-reference").Single().Element(Svrl + "text")!;
        await Assert.That((string?)text.Attribute(Lang)).IsEqualTo("de");
    }

    [Test]
    public async Task Property_WritesItsLanguageOnTheText()
    {
        var root = Run(
            "<schema xmlns=\"http://purl.oclc.org/dsdl/schematron\" schematronEdition=\"2025\"><pattern><rule context=\"a\"><report test=\"true()\" properties=\"p1\"/></rule></pattern><properties><property id=\"p1\" xml:lang=\"de\">Context: <value-of select=\"name()\"/></property></properties></schema>"
        );

        var text = root.Descendants(Svrl + "property-reference").Single().Element(Svrl + "text")!;
        await Assert.That((string?)text.Attribute(Lang)).IsEqualTo("de");
    }

    [Test]
    public async Task AbstractRule_KeepsItsLanguageWhenExtended()
    {
        var root = Run(
            "<schema xmlns=\"http://purl.oclc.org/dsdl/schematron\" xml:lang=\"en\"><pattern><rule abstract=\"true\" id=\"ar\" xml:lang=\"de\"><assert test=\"false()\"/></rule><rule context=\"a\"><extends rule=\"ar\"/></rule></pattern></schema>"
        );

        await Assert.That((string?)root.Descendants(Svrl + "failed-assert").Single().Attribute(Lang)).IsEqualTo("de");
    }

    [Test]
    public async Task AbstractPattern_KeepsItsLanguageWhenInstantiated()
    {
        var root = Run(
            "<schema xmlns=\"http://purl.oclc.org/dsdl/schematron\" xml:lang=\"en\"><pattern abstract=\"true\" id=\"ap\" xml:lang=\"de\"><rule context=\"*\"><assert test=\"false()\"/></rule></pattern><pattern is-a=\"ap\"/></schema>"
        );

        await Assert.That((string?)root.Descendants(Svrl + "failed-assert").Single().Attribute(Lang)).IsEqualTo("de");
    }
}
