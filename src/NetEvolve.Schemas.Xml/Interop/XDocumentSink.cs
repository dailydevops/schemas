namespace NetEvolve.Schemas.Xml.Interop;

using System;
using System.Xml.Linq;
using NetEvolve.Schemas.Xml.Contracts;
using XmlDeclaration = NetEvolve.Schemas.Xml.Contracts.XmlDeclaration;
using XmlNameTable = NetEvolve.Schemas.Xml.Contracts.XmlNameTable;

/// <summary>An <see cref="IXmlSink"/> that copies the events into an <see cref="XDocument"/>.</summary>
/// <remarks>The XML declaration, positions, and the document type are not carried over; the model does not keep CDATA sections, so text is plain text.</remarks>
internal sealed class XDocumentSink : IXmlSink
{
    private readonly XDocument _document = new();
    private XContainer _current;

    internal XDocumentSink(XmlNameTable names)
    {
        Names = names;
        _current = _document;
    }

    public XmlNameTable Names { get; }

    internal XDocument Document => _document;

    public void StartDocument(in XmlDeclaration declaration) { }

    public void EndDocument() { }

    public void StartElement(XmlName name, XmlPosition position)
    {
        var element = new XElement(XName.Get(Names.GetString(name.Local), Names.GetString(name.NamespaceUri)));
        _current.Add(element);
        _current = element;
    }

    public void NamespaceDeclaration(XmlSymbol prefix, XmlSymbol uri)
    {
        var prefixText = Names.GetString(prefix);
        var uriText = Names.GetString(uri);
        if (prefixText.Length == 0)
        {
            _current.Add(new XAttribute("xmlns", uriText));
        }
        else if (uriText.Length != 0)
        {
            // A prefix cannot be undeclared in XML 1.0, and an xmlns attribute with an empty value is not allowed here.
            _current.Add(new XAttribute(XNamespace.Xmlns + prefixText, uriText));
        }
    }

    public void Attribute(XmlName name, ReadOnlySpan<char> value) =>
        _current.Add(
            new XAttribute(XName.Get(Names.GetString(name.Local), Names.GetString(name.NamespaceUri)), value.ToString())
        );

    public void EndAttributes() { }

    public void EndElement(XmlName name) => _current = _current.Parent ?? (XContainer)_document;

    public void Text(ReadOnlySpan<char> text, XmlTextKind kind) => _current.Add(new XText(text.ToString()));

    public void Comment(ReadOnlySpan<char> text) => _current.Add(new XComment(text.ToString()));

    public void ProcessingInstruction(XmlSymbol target, ReadOnlySpan<char> data) =>
        _current.Add(new XProcessingInstruction(Names.GetString(target), data.ToString()));

    public void Doctype(in XmlDoctypeInfo doctype) { }
}
