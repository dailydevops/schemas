namespace NetEvolve.Schemas.Xml.Dtd.Validation.Pipeline;

using System;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Text.NameTable;

/// <summary>The inner sink of a run that only validates: it owns the name table and drops every event.</summary>
internal sealed class DtdDiscardSink : IXmlSink
{
    /// <summary>Initializes a new instance of the <see cref="DtdDiscardSink"/> class.</summary>
    /// <param name="limits">The limits that bound the name table.</param>
    internal DtdDiscardSink(XmlReaderLimits limits)
    {
        ArgumentNullException.ThrowIfNull(limits);

        Names = new SpanInterningNameTable(limits.MaxDistinctNames);
    }

    /// <inheritdoc />
    public XmlNameTable Names { get; }

    /// <inheritdoc />
    public void StartDocument(in XmlDeclaration declaration) { }

    /// <inheritdoc />
    public void EndDocument() { }

    /// <inheritdoc />
    public void StartElement(XmlName name, XmlPosition position) { }

    /// <inheritdoc />
    public void NamespaceDeclaration(XmlSymbol prefix, XmlSymbol uri) { }

    /// <inheritdoc />
    public void Attribute(XmlName name, ReadOnlySpan<char> value) { }

    /// <inheritdoc />
    public void EndAttributes() { }

    /// <inheritdoc />
    public void EndElement(XmlName name) { }

    /// <inheritdoc />
    public void Text(ReadOnlySpan<char> text, XmlTextKind kind) { }

    /// <inheritdoc />
    public void Comment(ReadOnlySpan<char> text) { }

    /// <inheritdoc />
    public void ProcessingInstruction(XmlSymbol target, ReadOnlySpan<char> data) { }

    /// <inheritdoc />
    public void Doctype(in XmlDoctypeInfo doctype) { }
}
