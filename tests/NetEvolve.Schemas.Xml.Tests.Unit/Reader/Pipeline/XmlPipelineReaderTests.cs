namespace NetEvolve.Schemas.Xml.Tests.Unit.Reader.Pipeline;

using System;
using System.Linq;
using System.Text;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Reader.Pipeline;
using NetEvolve.Schemas.Xml.Tests.Unit.Contracts.Fakes;
using NetEvolve.Schemas.Xml.Text.NameTable;

/// <summary>The composed reader pipeline (XML-53): factories for char and byte inputs, stage order, and limits.</summary>
public class XmlPipelineReaderTests
{
    private const string Document =
        "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\r\n<!-- c --><r xmlns=\"urn:d\" xmlns:p=\"urn:p\" a=\"1&amp;2\">"
        + "café \U0001D11E &lt;<p:x p:b=\"v\"/><![CDATA[<raw>]]><?pi data?></r>";

    private static RecordingXmlSink NewSink() =>
        new(new SpanInterningNameTable(XmlReaderLimits.Default.MaxDistinctNames));

    private static (XmlReadStatus Status, RecordingXmlSink Sink) Pump(XmlPipelineReader reader)
    {
        var sink = NewSink();
        var status = reader.Pump(ref sink);
        return (status, sink);
    }

    private static string[] Dump(RecordingXmlSink sink) =>
        [
            .. sink.Events.Select(e =>
                $"{e.Kind}|{sink.Names.GetString(e.Name.Local)}|{sink.Names.GetString(e.Name.NamespaceUri)}|{e.Text}|{e.TextKind}|{e.Position}"
            ),
        ];

    [Test]
    public async Task Factories_CharAndUtf8Inputs_GiveIdenticalEventStreams()
    {
        var bytes = Encoding.UTF8.GetBytes(Document);

        using var fromCharSpan = XmlPipelineReader.Create(Document.AsSpan());
        using var fromByteSpan = XmlPipelineReader.Create(bytes.AsSpan());
        using var fromCharMemory = XmlPipelineReader.CreateFromMemory(Document.AsMemory());
        using var fromByteMemory = XmlPipelineReader.CreateFromMemory(bytes.AsMemory());

        var expected = Pump(fromCharSpan);
        await Assert.That(expected.Status).IsEqualTo(XmlReadStatus.Completed);
        var expectedDump = Dump(expected.Sink);
        await Assert.That(expectedDump.Length).IsGreaterThan(10);

        foreach (var reader in new[] { fromByteSpan, fromCharMemory, fromByteMemory })
        {
            var (status, sink) = Pump(reader);
            await Assert.That(status).IsEqualTo(XmlReadStatus.Completed);
            await Assert.That(Dump(sink)).IsEquivalentTo(expectedDump).WithOrdering();
            await Assert.That(reader.Position).IsEqualTo(fromCharSpan.Position);
        }
    }

    [Test]
    public async Task Pump_IntoRecordingSink_EmitsEventsInDocumentOrder()
    {
        using var reader = XmlPipelineReader.Create("<a x=\"1\"><b/>t</a>".AsSpan());
        var sink = NewSink();

        var status = reader.Pump(ref sink);
        var again = reader.Pump(ref sink);

        await Assert.That(status).IsEqualTo(XmlReadStatus.Completed);
        await Assert.That(again).IsEqualTo(XmlReadStatus.Completed);
        await Assert
            .That(sink.Events.Select(e => e.Kind))
            .IsEquivalentTo(
                [
                    XmlEventKind.StartDocument,
                    XmlEventKind.StartElement,
                    XmlEventKind.Attribute,
                    XmlEventKind.EndAttributes,
                    XmlEventKind.StartElement,
                    XmlEventKind.EndAttributes,
                    XmlEventKind.EndElement,
                    XmlEventKind.Text,
                    XmlEventKind.EndElement,
                    XmlEventKind.EndDocument,
                ]
            )
            .WithOrdering();
    }

    [Test]
    public async Task Create_DeclaredIso88591_DecodesWithTheDeclaredEncoding()
    {
        var bytes = Encoding
            .GetEncoding("ISO-8859-1")
            .GetBytes("<?xml version=\"1.0\" encoding=\"ISO-8859-1\"?><a>café</a>");

        using var reader = XmlPipelineReader.Create(bytes.AsSpan());
        var (status, sink) = Pump(reader);

        await Assert.That(status).IsEqualTo(XmlReadStatus.Completed);
        await Assert.That(sink.Events.Single(e => e.Kind == XmlEventKind.Text).Text).IsEqualTo("café");
    }

    [Test]
    public async Task Create_Utf16WithByteOrderMark_GivesTheCharEventStream()
    {
        const string xml = "<a b=\"é\">é</a>";
        byte[] bytes = [0xFF, 0xFE, .. new UnicodeEncoding(false, false).GetBytes(xml)];

        using var fromBytes = XmlPipelineReader.Create(bytes.AsSpan());
        using var fromChars = XmlPipelineReader.Create(xml.AsSpan());

        await Assert.That(Dump(Pump(fromBytes).Sink)).IsEquivalentTo(Dump(Pump(fromChars).Sink)).WithOrdering();
    }

    [Test]
    public async Task Create_ConflictingDeclaredEncoding_ReportsXml4002()
    {
        var bytes = Encoding.UTF8.GetBytes("<?xml version=\"1.0\" encoding=\"UTF-16\"?><a/>");

        using var reader = XmlPipelineReader.Create(bytes.AsSpan());
        var (status, _) = Pump(reader);

        await Assert.That(status).IsEqualTo(XmlReadStatus.Error);
        await Assert.That(reader.Diagnostic!.Code).IsSameReferenceAs(XmlErrorCodes.XML4002);
    }

    [Test]
    public async Task Pump_InvalidUtf8AfterValidPrefix_ReportsXml4003()
    {
        byte[] bytes = [.. Encoding.UTF8.GetBytes("<a>ok"), 0xFF, .. Encoding.UTF8.GetBytes("</a>")];

        using var reader = XmlPipelineReader.CreateFromMemory(bytes.AsMemory());
        var (status, sink) = Pump(reader);

        await Assert.That(status).IsEqualTo(XmlReadStatus.Error);
        await Assert.That(reader.Diagnostic!.Code).IsSameReferenceAs(XmlErrorCodes.XML4003);
        await Assert.That(sink.Events.Select(e => e.Kind)).Contains(XmlEventKind.StartElement);
    }

    [Test]
    public async Task Pump_NotWellFormedBeforeInvalidBytes_ReportsTheEarlierError()
    {
        byte[] bytes = [.. Encoding.UTF8.GetBytes("<a></b>"), 0xFF];

        using var reader = XmlPipelineReader.Create(bytes.AsSpan());
        var (status, _) = Pump(reader);

        await Assert.That(status).IsEqualTo(XmlReadStatus.Error);
        await Assert.That(reader.Diagnostic!.Code).IsNotSameReferenceAs(XmlErrorCodes.XML4003);
    }

    [Test]
    public async Task Pump_UnclosedElement_ReportsErrorNotNeedMoreData()
    {
        using var reader = XmlPipelineReader.Create("<a><b>".AsSpan());

        var (status, _) = Pump(reader);

        await Assert.That(status).IsEqualTo(XmlReadStatus.Error);
        await Assert.That(reader.Diagnostic!.Code).IsSameReferenceAs(XmlErrorCodes.XML0001);
    }

    [Test]
    public async Task Create_LimitInOptions_IsEnforcedThroughThePipeline()
    {
        var options = XmlReaderOptions.Default.WithLimits(XmlReaderLimits.Default.WithMaxElementDepth(2));
        const string xml = "<a><b><c/></b></a>";

        using var chars = XmlPipelineReader.Create(xml.AsSpan(), options);
        using var bytes = XmlPipelineReader.CreateFromMemory(Encoding.UTF8.GetBytes(xml).AsMemory(), options);

        await Assert.That(Pump(chars).Status).IsEqualTo(XmlReadStatus.Error);
        await Assert.That(chars.Diagnostic!.Code).IsSameReferenceAs(XmlErrorCodes.XML1102);
        await Assert.That(Pump(bytes).Status).IsEqualTo(XmlReadStatus.Error);
        await Assert.That(bytes.Diagnostic!.Code).IsSameReferenceAs(XmlErrorCodes.XML1102);
    }

    [Test]
    public async Task Create_DocumentCharacterLimit_StopsDecodingBytes()
    {
        var options = XmlReaderOptions.Default.WithLimits(XmlReaderLimits.Default.WithMaxDocumentCharacters(8));
        var bytes = Encoding.UTF8.GetBytes("<a>" + new string('x', 100_000) + "</a>");

        using var reader = XmlPipelineReader.Create(bytes.AsSpan(), options);

        await Assert.That(Pump(reader).Status).IsEqualTo(XmlReadStatus.Error);
        await Assert.That(reader.Diagnostic!.Code).IsSameReferenceAs(XmlErrorCodes.XML1101);
    }
}
