namespace NetEvolve.Schemas.Tests.Architecture.Schematron.Fixtures.Duplicates;

using System;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Model;

/// <summary>Violating fixture: a second tree builder, found because it is a sink that produces an XML document tree.</summary>
internal abstract class TreeSinkFixture : IXmlSink
{
    public abstract XmlNameTable Names { get; }

    public abstract void StartDocument(in XmlDeclaration declaration);

    public abstract void EndDocument();

    public abstract void StartElement(XmlName name, XmlPosition position);

    public abstract void NamespaceDeclaration(XmlSymbol prefix, XmlSymbol uri);

    public abstract void Attribute(XmlName name, ReadOnlySpan<char> value);

    public abstract void EndAttributes();

    public abstract void EndElement(XmlName name);

    public abstract void Text(ReadOnlySpan<char> text, XmlTextKind kind);

    public abstract void Comment(ReadOnlySpan<char> text);

    public abstract void ProcessingInstruction(XmlSymbol target, ReadOnlySpan<char> data);

    public abstract void Doctype(in XmlDoctypeInfo doctype);

    public abstract XmlDocumentTree Complete();
}
