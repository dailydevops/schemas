namespace NetEvolve.Schemas.Xml.Writer;

using System;
using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NetEvolve.Schemas.Xml.Contracts;

/// <summary>Stub of the XML writer; the implementation follows in the next commit.</summary>
public sealed class XmlWriter : IXmlSink, IDisposable
{
    /// <summary>Initializes a new instance of the <see cref="XmlWriter"/> class.</summary>
    public XmlWriter(IBufferWriter<char> output, XmlNameTable names, XmlWriterOptions? options = null) =>
        throw new NotSupportedException("Stub until the implementation commit.");

    /// <summary>Initializes a new instance of the <see cref="XmlWriter"/> class.</summary>
    public XmlWriter(IBufferWriter<byte> output, XmlNameTable names, XmlWriterOptions? options = null) =>
        throw new NotSupportedException("Stub until the implementation commit.");

    /// <summary>Initializes a new instance of the <see cref="XmlWriter"/> class.</summary>
    public XmlWriter(Stream stream, XmlNameTable names, XmlWriterOptions? options = null) =>
        throw new NotSupportedException("Stub until the implementation commit.");

    /// <inheritdoc />
    public XmlNameTable Names => throw new NotSupportedException("Stub until the implementation commit.");

    /// <summary>Writes the buffered bytes to the stream.</summary>
    public Task FlushAsync(CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("Stub until the implementation commit.");

    /// <inheritdoc />
    public void StartDocument(in XmlDeclaration declaration) =>
        throw new NotSupportedException("Stub until the implementation commit.");

    /// <inheritdoc />
    public void EndDocument() => throw new NotSupportedException("Stub until the implementation commit.");

    /// <inheritdoc />
    public void StartElement(XmlName name, XmlPosition position) =>
        throw new NotSupportedException("Stub until the implementation commit.");

    /// <inheritdoc />
    public void NamespaceDeclaration(XmlSymbol prefix, XmlSymbol uri) =>
        throw new NotSupportedException("Stub until the implementation commit.");

    /// <inheritdoc />
    public void Attribute(XmlName name, ReadOnlySpan<char> value) =>
        throw new NotSupportedException("Stub until the implementation commit.");

    /// <inheritdoc />
    public void EndAttributes() => throw new NotSupportedException("Stub until the implementation commit.");

    /// <inheritdoc />
    public void EndElement(XmlName name) => throw new NotSupportedException("Stub until the implementation commit.");

    /// <inheritdoc />
    public void Text(ReadOnlySpan<char> text, XmlTextKind kind) =>
        throw new NotSupportedException("Stub until the implementation commit.");

    /// <inheritdoc />
    public void Comment(ReadOnlySpan<char> text) =>
        throw new NotSupportedException("Stub until the implementation commit.");

    /// <inheritdoc />
    public void ProcessingInstruction(XmlSymbol target, ReadOnlySpan<char> data) =>
        throw new NotSupportedException("Stub until the implementation commit.");

    /// <inheritdoc />
    public void Doctype(in XmlDoctypeInfo doctype) =>
        throw new NotSupportedException("Stub until the implementation commit.");

    /// <inheritdoc />
    public void Dispose() { }
}
