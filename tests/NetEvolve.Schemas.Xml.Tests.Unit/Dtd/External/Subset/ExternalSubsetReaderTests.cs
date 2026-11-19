namespace NetEvolve.Schemas.Xml.Tests.Unit.Dtd.External.Subset;

using System.IO;
using System.Linq;
using System.Text;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Declarations;
using NetEvolve.Schemas.Xml.Input.Stream;
using NetEvolve.Schemas.Xml.Reader.Pipeline;
using NetEvolve.Schemas.Xml.Tests.Unit.Contracts.Fakes;
using NetEvolve.Schemas.Xml.Tests.Unit.Dtd.External.Loader;
using NetEvolve.Schemas.Xml.Text.NameTable;

/// <summary>The readers hand the options of external access to the doctype handler: opt in, default options refuse, no options leave the subset unread.</summary>
public class ExternalSubsetReaderTests
{
    private const string Document = "<!DOCTYPE doc SYSTEM \"ext.dtd\"><doc>&e;</doc>";
    private const string Subset = "<!ELEMENT doc (#PCDATA)><!ENTITY e \"from the external subset\">";

    private static XmlReaderOptions Opted(DtdValidationOptions? access) =>
        XmlReaderOptions
            .Default.WithDtdProcessing(true)
            .WithExternalAccess(access)
            .WithBaseUri(SubsetHarness.DocumentUri);

    private static (XmlReadStatus Status, string? Code, string Text) ReadPipeline(string xml, XmlReaderOptions options)
    {
        using var reader = XmlPipelineReader.Create(xml.AsSpan(), options);
        var sink = new RecordingXmlSink(new SpanInterningNameTable(XmlReaderLimits.Default.MaxDistinctNames));
        var status = reader.Pump(ref sink);

        return (status, reader.Diagnostic?.Code.Alias, TextOf(sink));
    }

    private static string TextOf(RecordingXmlSink sink) =>
        string.Concat(sink.Events.Where(e => e.Kind == XmlEventKind.Text).Select(e => e.Text));

    [Test]
    public async Task Options_DefaultToNoExternalAccessAndNoBaseUri()
    {
        await Assert.That(XmlReaderOptions.Default.ExternalAccess).IsNull();
        await Assert.That(XmlReaderOptions.Default.BaseUri).IsNull();
    }

    [Test]
    public async Task Options_WithExternalAccessAndBaseUri_ReturnCopies()
    {
        var access = DtdValidationOptions.Default;
        var changed = XmlReaderOptions.Default.WithExternalAccess(access).WithBaseUri("http://example.org/a.xml");

        await Assert.That(changed.ExternalAccess).IsSameReferenceAs(access);
        await Assert.That(changed.BaseUri).IsEqualTo("http://example.org/a.xml");
        await Assert.That(XmlReaderOptions.Default.ExternalAccess).IsNull();
        await Assert.That(XmlReaderOptions.Default.BaseUri).IsNull();
        await Assert.That(changed.WithExternalAccess(null).ExternalAccess).IsNull();
    }

    [Test]
    public async Task Pipeline_WithExternalAccess_ExpandsAnEntityDeclaredInTheExternalSubset()
    {
        var resolver = SubsetHarness.Serving(Subset);

        var (status, code, text) = ReadPipeline(Document, Opted(LoaderHarness.Open(resolver)));

        await Assert.That(status).IsEqualTo(XmlReadStatus.Completed);
        await Assert.That(code).IsNull();
        await Assert.That(text).IsEqualTo("from the external subset");
        await Assert.That(resolver.Requests[0].ResolvedUri).IsEqualTo(SubsetHarness.SubsetUri);
    }

    [Test]
    public async Task Pipeline_WithoutExternalAccess_LeavesTheExternalSubsetUnread()
    {
        var (status, code, text) = ReadPipeline(Document, Opted(null));

        await Assert.That(status).IsEqualTo(XmlReadStatus.Completed);
        await Assert.That(code).IsNull();
        await Assert.That(text).IsEmpty();
    }

    [Test]
    public async Task Pipeline_WithDefaultExternalAccessOptions_RefusesWithXml2002_WithoutCallingTheResolver()
    {
        var resolver = SubsetHarness.Serving(Subset);

        var (status, code, _) = ReadPipeline(Document, Opted(DtdValidationOptions.Default.WithResolver(resolver)));

        await Assert.That(status).IsEqualTo(XmlReadStatus.Error);
        await Assert.That(code).IsEqualTo("XML2002");
        await Assert.That(resolver.Requests).IsEmpty();
    }

    [Test]
    public async Task Pipeline_ExternalSubsetThatIsNotWellFormed_IsAFatalError()
    {
        var (status, code, _) = ReadPipeline(
            Document,
            Opted(LoaderHarness.Open(SubsetHarness.Serving("<![ INCLUDE [ <!ELEMENT doc ANY>")))
        );

        await Assert.That(status).IsEqualTo(XmlReadStatus.Error);
        await Assert.That(code).IsEqualTo("XML0011");
    }

    [Test]
    public async Task Pipeline_DocumentWithoutASystemIdentifier_DoesNotRequestAnything()
    {
        var resolver = SubsetHarness.Serving(Subset);

        var (status, _, text) = ReadPipeline(
            "<!DOCTYPE doc [<!ENTITY e \"internal\">]><doc>&e;</doc>",
            Opted(LoaderHarness.Open(resolver))
        );

        await Assert.That(status).IsEqualTo(XmlReadStatus.Completed);
        await Assert.That(text).IsEqualTo("internal");
        await Assert.That(resolver.Requests).IsEmpty();
    }

    [Test]
    public async Task Stream_WithExternalAccess_ExpandsAnEntityDeclaredInTheExternalSubset(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(Document));
        var options = Opted(LoaderHarness.Open(SubsetHarness.Serving(Subset)));
        using var reader = new XmlStreamReader<RecordingXmlSink>(
            stream,
            new RecordingXmlSink(new SpanInterningNameTable(XmlReaderLimits.Default.MaxDistinctNames)),
            options,
            8
        );

        var status = await reader.ReadAsync(cancellationToken);

        await Assert.That(status).IsEqualTo(XmlReadStatus.Completed);
        await Assert.That(TextOf(reader.Sink)).IsEqualTo("from the external subset");
    }

    [Test]
    public async Task Stream_WithDefaultExternalAccessOptions_RefusesWithXml2002(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(Document));
        using var reader = new XmlStreamReader<RecordingXmlSink>(
            stream,
            new RecordingXmlSink(new SpanInterningNameTable(XmlReaderLimits.Default.MaxDistinctNames)),
            Opted(DtdValidationOptions.Default)
        );

        var status = await reader.ReadAsync(cancellationToken);

        await Assert.That(status).IsEqualTo(XmlReadStatus.Error);
        await Assert.That(reader.Diagnostic!.Code.Alias).IsEqualTo("XML2002");
    }

    [Test]
    public async Task Handler_WithOptions_ParsesTheExternalSubsetIntoTheModel()
    {
        var resolver = SubsetHarness.Serving(Subset);
        var handler = new DtdDoctypeHandler(
            XmlReaderLimits.Default,
            LoaderHarness.Open(resolver),
            SubsetHarness.DocumentUri
        );
        var info = new XmlDoctypeInfo("doc".AsSpan(), default, "ext.dtd".AsSpan(), default);

        var error = handler.Handle(in info);

        await Assert.That(error).IsNull();
        await Assert.That(handler.Declarations!.GeneralEntities.Keys).IsEquivalentTo(["e"]);
    }
}
