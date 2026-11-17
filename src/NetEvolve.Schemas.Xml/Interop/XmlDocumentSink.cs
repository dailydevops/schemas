namespace NetEvolve.Schemas.Xml.Interop;

using System;
using System.Xml;
using NetEvolve.Schemas.Xml.Contracts;
using XmlDeclaration = NetEvolve.Schemas.Xml.Contracts.XmlDeclaration;
using XmlNameTable = NetEvolve.Schemas.Xml.Contracts.XmlNameTable;
using XmlNode = System.Xml.XmlNode;

/// <summary>An <see cref="IXmlSink"/> that copies the events into an <see cref="XmlDocument"/>.</summary>
/// <remarks>The XML declaration, positions, and the document type are not carried over; the model does not keep CDATA sections, so text is plain text.</remarks>
internal sealed class XmlDocumentSink : IXmlSink
{
    private readonly XmlDocument _document = new() { PreserveWhitespace = true };
    private XmlNode _current;

    internal XmlDocumentSink(XmlNameTable names)
    {
        Names = names;
        _current = _document;
    }

    public XmlNameTable Names { get; }

    internal XmlDocument Document => _document;

    public void StartDocument(in XmlDeclaration declaration) { }

    public void EndDocument() { }

    public void StartElement(XmlName name, XmlPosition position)
    {
        var element = _document.CreateElement(
            Names.GetString(name.Prefix),
            Names.GetString(name.Local),
            Names.GetString(name.NamespaceUri)
        );
        _ = _current.AppendChild(element);
        _current = element;
    }

    public void NamespaceDeclaration(XmlSymbol prefix, XmlSymbol uri)
    {
        var prefixText = Names.GetString(prefix);
        var attribute =
            prefixText.Length == 0
                ? _document.CreateAttribute(string.Empty, "xmlns", TreeEmitter.XmlnsUri)
                : _document.CreateAttribute("xmlns", prefixText, TreeEmitter.XmlnsUri);
        attribute.Value = Names.GetString(uri);
        _ = ((XmlElement)_current).SetAttributeNode(attribute);
    }

    public void Attribute(XmlName name, ReadOnlySpan<char> value)
    {
        var attribute = _document.CreateAttribute(
            Names.GetString(name.Prefix),
            Names.GetString(name.Local),
            Names.GetString(name.NamespaceUri)
        );
        attribute.Value = value.ToString();
        _ = ((XmlElement)_current).SetAttributeNode(attribute);
    }

    public void EndAttributes() { }

    public void EndElement(XmlName name) => _current = _current.ParentNode!;

    public void Text(ReadOnlySpan<char> text, XmlTextKind kind) =>
        _ = _current.AppendChild(_document.CreateTextNode(text.ToString()));

    public void Comment(ReadOnlySpan<char> text) => _ = _current.AppendChild(_document.CreateComment(text.ToString()));

    public void ProcessingInstruction(XmlSymbol target, ReadOnlySpan<char> data) =>
        _ = _current.AppendChild(_document.CreateProcessingInstruction(Names.GetString(target), data.ToString()));

    public void Doctype(in XmlDoctypeInfo doctype) { }
}
