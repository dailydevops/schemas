namespace NetEvolve.Schemas.Xml.Model;

using System;
using NetEvolve.Schemas.Xml.Contracts;

/// <summary>
/// A struct adapter over <see cref="XmlTreeBuilder"/> for the loaders: a pump over a value type is specialized by the JIT,
/// so the calls into the sealed builder are direct and inlinable instead of interface dispatch through shared generic code.
/// </summary>
/// <remarks>
/// The adapter holds only the reference to the builder, so a copy shares all state. It forwards the sink events and not the
/// attribute facts of <see cref="IXmlAttributeInfoSink"/>, which a reader offers to a reference-type sink only; a load that
/// processes a document type declaration therefore passes the builder itself.
/// </remarks>
internal readonly struct XmlTreeBuilderSink(XmlTreeBuilder builder) : IXmlSink
{
    /// <inheritdoc />
    public XmlNameTable Names => builder.Names;

    /// <inheritdoc />
    public void StartDocument(in XmlDeclaration declaration) => builder.StartDocument(in declaration);

    /// <inheritdoc />
    public void EndDocument() => builder.EndDocument();

    /// <inheritdoc />
    public void StartElement(XmlName name, XmlPosition position) => builder.StartElement(name, position);

    /// <inheritdoc />
    public void NamespaceDeclaration(XmlSymbol prefix, XmlSymbol uri) => builder.NamespaceDeclaration(prefix, uri);

    /// <inheritdoc />
    public void Attribute(XmlName name, ReadOnlySpan<char> value) => builder.Attribute(name, value);

    /// <inheritdoc />
    public void EndAttributes() => builder.EndAttributes();

    /// <inheritdoc />
    public void EndElement(XmlName name) => builder.EndElement(name);

    /// <inheritdoc />
    public void Text(ReadOnlySpan<char> text, XmlTextKind kind) => builder.Text(text, kind);

    /// <inheritdoc />
    public void Comment(ReadOnlySpan<char> text) => builder.Comment(text);

    /// <inheritdoc />
    public void ProcessingInstruction(XmlSymbol target, ReadOnlySpan<char> data) =>
        builder.ProcessingInstruction(target, data);

    /// <inheritdoc />
    public void Doctype(in XmlDoctypeInfo doctype) => builder.Doctype(in doctype);
}
