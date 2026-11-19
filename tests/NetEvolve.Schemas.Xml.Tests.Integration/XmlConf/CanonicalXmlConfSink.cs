namespace NetEvolve.Schemas.Xml.Tests.Integration.XmlConf;

using System.Text;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Text.NameTable;

/// <summary>
/// Writes the events in the canonical form of the output files of the suite: no comments, every element with an end tag,
/// attributes in ascending order of their names, and <c>&amp;</c>, <c>&lt;</c>, <c>&gt;</c>, <c>"</c>, TAB, LF, and CR
/// written as references.
/// </summary>
internal sealed class CanonicalXmlConfSink : IXmlSink
{
    private readonly StringBuilder _output = new();
    private readonly List<(string Name, string Value)> _attributes = [];
    private readonly Stack<string> _open = new();
    private string _pending = string.Empty;

    public XmlNameTable Names { get; } = new SpanInterningNameTable(XmlReaderLimits.Default.MaxDistinctNames);

    /// <summary>Gets the canonical form of the events received so far.</summary>
    public string Output => _output.ToString();

    public void StartDocument(in XmlDeclaration declaration) { }

    public void EndDocument() { }

    public void StartElement(XmlName name, XmlPosition position)
    {
        _pending = Qualified(name);
        _attributes.Clear();
    }

    public void NamespaceDeclaration(XmlSymbol prefix, XmlSymbol uri)
    {
        var declared = Names.GetString(prefix);
        _attributes.Add((declared.Length == 0 ? "xmlns" : "xmlns:" + declared, Names.GetString(uri)));
    }

    public void Attribute(XmlName name, ReadOnlySpan<char> value) =>
        _attributes.Add((Qualified(name), value.ToString()));

    public void EndAttributes()
    {
        _attributes.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
        _ = _output.Append('<').Append(_pending);
        foreach (var (name, value) in _attributes)
        {
            _ = _output.Append(' ').Append(name).Append("=\"");
            Escape(value);
            _ = _output.Append('"');
        }

        _ = _output.Append('>');
        _open.Push(_pending);
    }

    public void EndElement(XmlName name) => _output.Append("</").Append(_open.Pop()).Append('>');

    public void Text(ReadOnlySpan<char> text, XmlTextKind kind) => Escape(text.ToString());

    public void Comment(ReadOnlySpan<char> text) { }

    public void ProcessingInstruction(XmlSymbol target, ReadOnlySpan<char> data) =>
        _output.Append("<?").Append(Names.GetString(target)).Append(' ').Append(data).Append("?>");

    public void Doctype(in XmlDoctypeInfo doctype) { }

    private string Qualified(XmlName name)
    {
        var prefix = Names.GetString(name.Prefix);
        var local = Names.GetString(name.Local);
        return prefix.Length == 0 ? local : prefix + ":" + local;
    }

    private void Escape(string text)
    {
        foreach (var c in text)
        {
            _ = c switch
            {
                '&' => _output.Append("&amp;"),
                '<' => _output.Append("&lt;"),
                '>' => _output.Append("&gt;"),
                '"' => _output.Append("&quot;"),
                '\t' => _output.Append("&#9;"),
                '\n' => _output.Append("&#10;"),
                '\r' => _output.Append("&#13;"),
                _ => _output.Append(c),
            };
        }
    }
}
