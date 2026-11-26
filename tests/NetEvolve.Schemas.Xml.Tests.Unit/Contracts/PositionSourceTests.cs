namespace NetEvolve.Schemas.Xml.Tests.Unit.Contracts;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Input.Sequence;
using NetEvolve.Schemas.Xml.Reader.Pipeline;
using NetEvolve.Schemas.Xml.Tests.Unit.Contracts.Fakes;
using NetEvolve.Schemas.Xml.Tests.Unit.Input.Sequence;

/// <summary>The readers tell a sink where the event of a callback began (issue #606).</summary>
public class PositionSourceTests
{
    private const string Document = "<r a=\"1\"  b=\"2\">text<e/><!--c--><?p d?></r >";

    private sealed class PositionSink : IXmlSink
    {
        private readonly List<string> _seen = [];

        public IXmlPositionSource? Source { get; set; }

        public XmlNameTable Names { get; } = new FakeNameTable();

        public IReadOnlyList<string> Seen => _seen;

        private void Note(string kind) => _seen.Add(kind + "@" + Source!.Current.CharOffset);

        public void StartDocument(in XmlDeclaration declaration) { }

        public void EndDocument() { }

        public void StartElement(XmlName name, XmlPosition position) =>
            _seen.Add("start@" + position.CharOffset + "=" + Source!.Current.CharOffset);

        public void NamespaceDeclaration(XmlSymbol prefix, XmlSymbol uri) { }

        public void Attribute(XmlName name, ReadOnlySpan<char> value) => Note("attribute");

        public void EndAttributes() { }

        public void EndElement(XmlName name) => Note("end");

        public void Text(ReadOnlySpan<char> text, XmlTextKind kind) => Note("text");

        public void Comment(ReadOnlySpan<char> text) => Note("comment");

        public void ProcessingInstruction(XmlSymbol target, ReadOnlySpan<char> data) => Note("pi");

        public void Doctype(in XmlDoctypeInfo doctype) { }
    }

    private static string[] Expected()
    {
        static int At(string marker) => Document.IndexOf(marker, StringComparison.Ordinal);

        return
        [
            "start@0=0",
            "attribute@" + At("a="),
            "attribute@" + At("b="),
            "text@" + At("text"),
            "start@" + At("<e/>") + "=" + At("<e/>"),
            "end@" + At("<e/>"),
            "comment@" + At("<!--"),
            "pi@" + At("<?p"),
            "end@" + At("</r"),
        ];
    }

    [Test]
    public async Task PipelineReader_ReportsTheStartOfEachEvent()
    {
        using var reader = XmlPipelineReader.Create(Document.AsSpan());
        var sink = new PositionSink { Source = reader };

        var status = reader.Pump(ref sink);

        await Assert.That(status).IsEqualTo(XmlReadStatus.Completed);
        await Assert.That(string.Join(" ", sink.Seen)).IsEqualTo(string.Join(" ", Expected()));
    }

    [Test]
    [Arguments(0)]
    [Arguments(1)]
    [Arguments(7)]
    public async Task SequenceReader_ReportsTheStartOfEachEvent_AcrossSegments(int chunk)
    {
        var data = Document.ToCharArray();
        var input = chunk == 0 ? SequenceBuilder.Whole(data) : SequenceBuilder.Chunks(data, chunk);
        using var reader = XmlSequenceReader.Create(input);
        var sink = new PositionSink { Source = reader };

        var status = reader.Pump(ref sink);

        await Assert.That(status).IsEqualTo(XmlReadStatus.Completed);
        await Assert.That(string.Join(" ", sink.Seen)).IsEqualTo(string.Join(" ", Expected()));
    }
}
