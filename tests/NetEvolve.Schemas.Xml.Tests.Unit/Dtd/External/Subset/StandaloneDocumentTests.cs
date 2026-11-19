namespace NetEvolve.Schemas.Xml.Tests.Unit.Dtd.External.Subset;

using System.Linq;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Reader.Pipeline;
using NetEvolve.Schemas.Xml.Tests.Unit.Contracts.Fakes;
using NetEvolve.Schemas.Xml.Tests.Unit.Dtd.External.Loader;
using NetEvolve.Schemas.Xml.Text.NameTable;

/// <summary>Section 4.1, WFC Entity Declared: with <c>standalone="yes"</c> an entity must be declared in the internal subset.</summary>
public class StandaloneDocumentTests
{
    private const string Subset = "<!ELEMENT doc ANY><!ENTITY e \"external\">";

    private static (XmlReadStatus Status, string? Code, string Text) Read(string xml, string subset)
    {
        var options = XmlReaderOptions
            .Default.WithDtdProcessing(true)
            .WithExternalAccess(LoaderHarness.Open(SubsetHarness.Serving(subset)))
            .WithBaseUri(SubsetHarness.DocumentUri);
        using var reader = XmlPipelineReader.Create(xml.AsSpan(), options);
        var sink = new RecordingXmlSink(new SpanInterningNameTable(XmlReaderLimits.Default.MaxDistinctNames));
        var status = reader.Pump(ref sink);
        var text = string.Concat(sink.Events.Where(e => e.Kind == XmlEventKind.Text).Select(e => e.Text));

        return (status, reader.Diagnostic?.Code.Alias, text);
    }

    [Test]
    [Arguments("<doc>&e;</doc>")]
    [Arguments("<doc a=\"&e;\"/>")]
    public async Task StandaloneYes_EntityDeclaredOnlyInTheExternalSubset_RaisesXml0008(string body)
    {
        ArgumentNullException.ThrowIfNull(body);
        var (status, code, _) = Read(
            $"<?xml version=\"1.0\" standalone=\"yes\"?><!DOCTYPE doc SYSTEM \"ext.dtd\">{body}",
            Subset
        );

        await Assert.That(status).IsEqualTo(XmlReadStatus.Error);
        await Assert.That(code).IsEqualTo("XML0008");
    }

    [Test]
    [Arguments("<?xml version=\"1.0\" standalone=\"no\"?>")]
    [Arguments("<?xml version=\"1.0\"?>")]
    [Arguments("")]
    public async Task StandaloneNoOrAbsent_EntityDeclaredInTheExternalSubset_IsExpanded(string declaration)
    {
        ArgumentNullException.ThrowIfNull(declaration);
        var (status, _, text) = Read($"{declaration}<!DOCTYPE doc SYSTEM \"ext.dtd\"><doc>&e;</doc>", Subset);

        await Assert.That(status).IsEqualTo(XmlReadStatus.Completed);
        await Assert.That(text).IsEqualTo("external");
    }

    [Test]
    public async Task StandaloneYes_EntityDeclaredInTheInternalSubset_IsExpanded_EvenWhenTheExternalSubsetDeclaresItToo()
    {
        var (status, _, text) = Read(
            "<?xml version=\"1.0\" standalone=\"yes\"?><!DOCTYPE doc SYSTEM \"ext.dtd\" [<!ENTITY e \"internal\">]><doc>&e;</doc>",
            Subset
        );

        await Assert.That(status).IsEqualTo(XmlReadStatus.Completed);
        await Assert.That(text).IsEqualTo("internal");
    }

    [Test]
    public async Task Declarations_RememberWhetherTheyStandInExternalText()
    {
        var resolver = new RecordingResolver()
            .Add(SubsetHarness.SubsetUri, LoaderHarness.Utf8("<!ENTITY fromSubset 'x'> %ext;"))
            .Add("http://example.org/doc/pe.ent", LoaderHarness.Utf8("<!ENTITY fromEntity 'y'>"));

        var (model, error) = SubsetHarness.ParseModel(
            "<!ENTITY fromDocument 'z'><!ENTITY % ext SYSTEM \"pe.ent\">",
            resolver
        );

        await Assert.That(error).IsNull();
        await Assert.That(model!.GeneralEntities["fromDocument"].InExternalText).IsFalse();
        await Assert.That(model.GeneralEntities["fromSubset"].InExternalText).IsTrue();
        await Assert.That(model.GeneralEntities["fromEntity"].InExternalText).IsTrue();
    }
}
