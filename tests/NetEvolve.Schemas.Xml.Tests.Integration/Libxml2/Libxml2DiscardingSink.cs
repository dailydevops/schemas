namespace NetEvolve.Schemas.Xml.Tests.Integration.Libxml2;

using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Text.NameTable;

/// <summary>A sink that takes every event and keeps nothing; the runners only need the read status.</summary>
internal sealed class Libxml2DiscardingSink : IXmlSink
{
    public XmlNameTable Names { get; } = new SpanInterningNameTable(XmlReaderLimits.Default.MaxDistinctNames);

    public void StartDocument(in XmlDeclaration declaration) { }

    public void EndDocument() { }

    public void StartElement(XmlName name, XmlPosition position) { }

    public void NamespaceDeclaration(XmlSymbol prefix, XmlSymbol uri) { }

    public void Attribute(XmlName name, ReadOnlySpan<char> value) { }

    public void EndAttributes() { }

    public void EndElement(XmlName name) { }

    public void Text(ReadOnlySpan<char> text, XmlTextKind kind) { }

    public void Comment(ReadOnlySpan<char> text) { }

    public void ProcessingInstruction(XmlSymbol target, ReadOnlySpan<char> data) { }

    public void Doctype(in XmlDoctypeInfo doctype) { }
}
