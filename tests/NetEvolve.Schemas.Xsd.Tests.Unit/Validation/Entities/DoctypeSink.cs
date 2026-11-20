namespace NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Entities;

using System;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xsd.Validation.Core;
using NetEvolve.Schemas.Xsd.Validation.Entities;

/// <summary>Forwards every reader event to the validator and hands the document type declaration to the entity listener as well.</summary>
internal sealed class DoctypeSink(StreamingValidator validator, EntityListener listener) : IXmlSink
{
    public XmlNameTable Names => validator.Names;

    public void StartDocument(in XmlDeclaration declaration) => validator.StartDocument(in declaration);

    public void EndDocument() => validator.EndDocument();

    public void StartElement(XmlName name, XmlPosition position) => validator.StartElement(name, position);

    public void NamespaceDeclaration(XmlSymbol prefix, XmlSymbol uri) => validator.NamespaceDeclaration(prefix, uri);

    public void Attribute(XmlName name, ReadOnlySpan<char> value) => validator.Attribute(name, value);

    public void EndAttributes() => validator.EndAttributes();

    public void EndElement(XmlName name) => validator.EndElement(name);

    public void Text(ReadOnlySpan<char> text, XmlTextKind kind) => validator.Text(text, kind);

    public void Comment(ReadOnlySpan<char> text) => validator.Comment(text);

    public void ProcessingInstruction(XmlSymbol target, ReadOnlySpan<char> data) =>
        validator.ProcessingInstruction(target, data);

    public void Doctype(in XmlDoctypeInfo doctype)
    {
        validator.Doctype(in doctype);
        listener.ReceiveDoctype(in doctype);
    }
}
