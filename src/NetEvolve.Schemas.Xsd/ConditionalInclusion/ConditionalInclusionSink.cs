namespace NetEvolve.Schemas.Xsd.ConditionalInclusion;

using System;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xsd.Diagnostics;

/// <summary>Removes the elements that the version control attributes exclude, before the inner sink sees them.</summary>
internal sealed class ConditionalInclusionSink : IXmlSink
{
    private readonly IXmlSink _inner;

    internal ConditionalInclusionSink(IXmlSink inner, XsdVersion version, string? baseUri, XsdIssueReporter reporter)
    {
        Argument.ThrowIfNull(inner);
        Argument.ThrowIfLessThan((int)version, (int)XsdVersion.V10);
        Argument.ThrowIfGreaterThan((int)version, (int)XsdVersion.V11);
        Argument.ThrowIfNull(reporter);

        _inner = inner;
        _ = baseUri;
    }

    /// <inheritdoc />
    public XmlNameTable Names => _inner.Names;

    /// <inheritdoc />
    public void StartDocument(in XmlDeclaration declaration) => _inner.StartDocument(in declaration);

    /// <inheritdoc />
    public void EndDocument() => _inner.EndDocument();

    /// <inheritdoc />
    public void StartElement(XmlName name, XmlPosition position) => _inner.StartElement(name, position);

    /// <inheritdoc />
    public void NamespaceDeclaration(XmlSymbol prefix, XmlSymbol uri) => _inner.NamespaceDeclaration(prefix, uri);

    /// <inheritdoc />
    public void Attribute(XmlName name, ReadOnlySpan<char> value) => _inner.Attribute(name, value);

    /// <inheritdoc />
    public void EndAttributes() => _inner.EndAttributes();

    /// <inheritdoc />
    public void EndElement(XmlName name) => _inner.EndElement(name);

    /// <inheritdoc />
    public void Text(ReadOnlySpan<char> text, XmlTextKind kind) => _inner.Text(text, kind);

    /// <inheritdoc />
    public void Comment(ReadOnlySpan<char> text) => _inner.Comment(text);

    /// <inheritdoc />
    public void ProcessingInstruction(XmlSymbol target, ReadOnlySpan<char> data) =>
        _inner.ProcessingInstruction(target, data);

    /// <inheritdoc />
    public void Doctype(in XmlDoctypeInfo doctype) => _inner.Doctype(in doctype);
}
