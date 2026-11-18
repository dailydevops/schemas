namespace NetEvolve.Schemas.Xml.Tests.Unit.Input.Stream;

using System.Text;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Tests.Unit.Contracts.Fakes;

/// <summary>
/// Dumps the events of a recording sink as one string. Consecutive text events of one kind are one node, so the edges of
/// the chunks must not show.
/// </summary>
internal sealed class StreamEventDump
{
    private readonly RecordingXmlSink _sink;
    private readonly StringBuilder _builder = new();
    private readonly StringBuilder _text = new();
    private XmlTextKind? _textKind;

    private StreamEventDump(RecordingXmlSink sink) => _sink = sink;

    internal static string Of(RecordingXmlSink sink) => new StreamEventDump(sink).Build();

    private string Build()
    {
        foreach (var e in _sink.Events)
        {
            if (e.Kind == XmlEventKind.Text)
            {
                AppendText(e);
                continue;
            }

            FlushText();
            _builder.Append(e.Kind).Append(Detail(e)).Append('|');
        }

        FlushText();
        return _builder.ToString();
    }

    private void AppendText(XmlEventRecord e)
    {
        var kind = e.TextKind == XmlTextKind.CData ? XmlTextKind.CData : XmlTextKind.Text;
        if (_textKind != kind)
        {
            FlushText();
            _textKind = kind;
        }

        _text.Append(e.Text);
    }

    private void FlushText()
    {
        if (_textKind is not null)
        {
            _builder.Append(_textKind).Append('\'').Append(_text).Append("'|");
            _text.Clear();
            _textKind = null;
        }
    }

    private string Name(XmlName name)
    {
        var names = _sink.Names;
        return $"{{{names.GetString(name.NamespaceUri)}}}{names.GetString(name.Prefix)}:{names.GetString(name.Local)}";
    }

    private string Detail(XmlEventRecord e)
    {
        var names = _sink.Names;
        return e.Kind switch
        {
            XmlEventKind.StartElement => Name(e.Name) + e.Position,
            XmlEventKind.EndElement => Name(e.Name),
            XmlEventKind.Attribute => Name(e.Name) + "=" + e.Text,
            XmlEventKind.StartDocument or XmlEventKind.Comment => e.Text,
            XmlEventKind.ProcessingInstruction => names.GetString(e.Prefix) + " " + e.Text,
            XmlEventKind.NamespaceDeclaration => names.GetString(e.Prefix) + "=" + names.GetString(e.Uri),
            _ => string.Empty,
        };
    }
}
