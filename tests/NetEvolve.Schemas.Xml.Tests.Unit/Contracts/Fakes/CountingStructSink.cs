namespace NetEvolve.Schemas.Xml.Tests.Unit.Contracts.Fakes;

using System;
using NetEvolve.Schemas.Xml.Contracts;

/// <summary>A mutable struct sink: a copy would lose the count, so it proves the sink is passed by reference.</summary>
internal struct CountingStructSink : IXmlSink
{
    public CountingStructSink(XmlNameTable names) => Names = names;

    public int Count { get; private set; }

    public XmlNameTable Names { get; }

    public void StartDocument(in XmlDeclaration declaration) => Count++;

    public void EndDocument() => Count++;

    public void StartElement(XmlName name, XmlPosition position) => Count++;

    public void NamespaceDeclaration(XmlSymbol prefix, XmlSymbol uri) => Count++;

    public void Attribute(XmlName name, ReadOnlySpan<char> value) => Count++;

    public void EndAttributes() => Count++;

    public void EndElement(XmlName name) => Count++;

    public void Text(ReadOnlySpan<char> text, XmlTextKind kind) => Count++;

    public void Comment(ReadOnlySpan<char> text) => Count++;

    public void ProcessingInstruction(XmlSymbol target, ReadOnlySpan<char> data) => Count++;

    public void Doctype(in XmlDoctypeInfo doctype) => Count++;
}
