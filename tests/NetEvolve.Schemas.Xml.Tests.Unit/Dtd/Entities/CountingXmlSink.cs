namespace NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Entities;

using System;
using NetEvolve.Schemas.Xml.Contracts;

/// <summary>A sink that counts the characters of the text events and allocates nothing per event.</summary>
internal sealed class CountingXmlSink(XmlNameTable names) : IXmlSink
{
    public XmlNameTable Names { get; } = names;

    public long Characters { get; private set; }

    public void StartDocument(in XmlDeclaration declaration) { }

    public void EndDocument() { }

    public void StartElement(XmlName name, XmlPosition position) { }

    public void NamespaceDeclaration(XmlSymbol prefix, XmlSymbol uri) { }

    public void Attribute(XmlName name, ReadOnlySpan<char> value) { }

    public void EndAttributes() { }

    public void EndElement(XmlName name) { }

    public void Text(ReadOnlySpan<char> text, XmlTextKind kind) => Characters += text.Length;

    public void Comment(ReadOnlySpan<char> text) { }

    public void ProcessingInstruction(XmlSymbol target, ReadOnlySpan<char> data) { }

    public void Doctype(in XmlDoctypeInfo doctype) { }
}
