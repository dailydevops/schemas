namespace NetEvolve.Schemas.Xml.Model.Loading;

using System;
using System.Buffers;
using System.Threading;
using System.Threading.Tasks;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Input.Sequence;
using NetEvolve.Schemas.Xml.Input.Stream;
using NetEvolve.Schemas.Xml.Reader.Pipeline;

/// <summary>
/// The one entry for building an <see cref="XmlDocumentTree"/> from every input kind: spans, memory, sequences, and streams
/// of characters or bytes. The entry points are thin adapters over the readers of the input kinds and the shared
/// <see cref="XmlTreeBuilder"/>, so every input kind gives the same tree, the same element positions, and the same
/// error code and position for a malformed document.
/// </summary>
/// <remarks>
/// <para>
/// A malformed document, an unsupported encoding, and an exceeded limit throw <see cref="XmlParseException"/> with the
/// <see cref="XmlDiagnostic"/>; a byte input is decoded by byte order mark and declaration. The options pass through to the
/// reader: its limits, its document type declaration processing, and its name table, which is shared and not frozen when
/// given, and owned and frozen per document when not.
/// </para>
/// <para>
/// The <c>baseUri</c> becomes the document URI of the tree, so the document node reports it as its base URI, and an
/// <c>xml:base</c> attribute resolves against it. An absolute URI also resolves the relative system identifier of the
/// document type declaration; a relative URI is reported as given and resolves nothing.
/// </para>
/// <para>
/// Limitations: span inputs load synchronously and only the stream path is async; the stream is read from its current
/// position to its end and is left open; a <see cref="ReadOnlySpan{T}"/> is copied or decoded during the call, and a
/// <see cref="ReadOnlyMemory{T}"/> and a <see cref="ReadOnlySequence{T}"/> are read during the call only; the notation and
/// unparsed entity declarations of a document type declaration are not added to the tree, which belongs to the validating
/// load (XML-68, XML-69); loading from <c>XmlReader</c> or <c>XDocument</c> is XML-18 and the resolver-backed
/// <c>doc()</c> is XP-60.
/// </para>
/// </remarks>
public static class XmlDocumentLoader
{
    /// <summary>Loads a document from characters.</summary>
    /// <param name="input">The document.</param>
    /// <param name="options">The options, <see langword="null"/> for <see cref="XmlReaderOptions.Default"/>.</param>
    /// <param name="baseUri">The base URI of the document, <see langword="null"/> when unknown.</param>
    /// <returns>The tree.</returns>
    /// <exception cref="XmlParseException">The document is not well formed or exceeds a limit.</exception>
    public static XmlDocumentTree Load(ReadOnlySpan<char> input, XmlReaderOptions? options = null, Uri? baseUri = null)
    {
        options = WithBaseUri(options, baseUri);
        using var reader = XmlPipelineReader.Create(input, options);
        return Build(reader, options, baseUri);
    }

    /// <summary>Loads a document from bytes; the encoding follows XML 1.0 Appendix F and the declaration.</summary>
    /// <param name="input">The document.</param>
    /// <param name="options">The options, <see langword="null"/> for <see cref="XmlReaderOptions.Default"/>.</param>
    /// <param name="baseUri">The base URI of the document, <see langword="null"/> when unknown.</param>
    /// <returns>The tree.</returns>
    /// <exception cref="XmlParseException">The document is not well formed, uses an unsupported encoding, or exceeds a limit.</exception>
    public static XmlDocumentTree Load(ReadOnlySpan<byte> input, XmlReaderOptions? options = null, Uri? baseUri = null)
    {
        options = WithBaseUri(options, baseUri);
        using var reader = XmlPipelineReader.Create(input, options);
        return Build(reader, options, baseUri);
    }

    /// <summary>Loads a document from characters in memory.</summary>
    /// <param name="input">The document.</param>
    /// <param name="options">The options, <see langword="null"/> for <see cref="XmlReaderOptions.Default"/>.</param>
    /// <param name="baseUri">The base URI of the document, <see langword="null"/> when unknown.</param>
    /// <returns>The tree.</returns>
    /// <exception cref="XmlParseException">The document is not well formed or exceeds a limit.</exception>
    public static XmlDocumentTree LoadFromMemory(
        ReadOnlyMemory<char> input,
        XmlReaderOptions? options = null,
        Uri? baseUri = null
    )
    {
        options = WithBaseUri(options, baseUri);
        using var reader = XmlPipelineReader.CreateFromMemory(input, options);
        return Build(reader, options, baseUri);
    }

    /// <summary>Loads a document from bytes in memory; the encoding follows XML 1.0 Appendix F and the declaration.</summary>
    /// <param name="input">The document.</param>
    /// <param name="options">The options, <see langword="null"/> for <see cref="XmlReaderOptions.Default"/>.</param>
    /// <param name="baseUri">The base URI of the document, <see langword="null"/> when unknown.</param>
    /// <returns>The tree.</returns>
    /// <exception cref="XmlParseException">The document is not well formed, uses an unsupported encoding, or exceeds a limit.</exception>
    public static XmlDocumentTree LoadFromMemory(
        ReadOnlyMemory<byte> input,
        XmlReaderOptions? options = null,
        Uri? baseUri = null
    )
    {
        options = WithBaseUri(options, baseUri);
        using var reader = XmlPipelineReader.CreateFromMemory(input, options);
        return Build(reader, options, baseUri);
    }

    /// <summary>Loads a document from a sequence of characters.</summary>
    /// <param name="input">The document.</param>
    /// <param name="options">The options, <see langword="null"/> for <see cref="XmlReaderOptions.Default"/>.</param>
    /// <param name="baseUri">The base URI of the document, <see langword="null"/> when unknown.</param>
    /// <returns>The tree.</returns>
    /// <exception cref="XmlParseException">The document is not well formed or exceeds a limit.</exception>
    public static XmlDocumentTree Load(
        ReadOnlySequence<char> input,
        XmlReaderOptions? options = null,
        Uri? baseUri = null
    )
    {
        options = WithBaseUri(options, baseUri);
        using var reader = XmlSequenceReader.Create(input, options);
        return Build(reader, options, baseUri);
    }

    /// <summary>Loads a document from a sequence of bytes; the encoding follows XML 1.0 Appendix F and the declaration.</summary>
    /// <param name="input">The document.</param>
    /// <param name="options">The options, <see langword="null"/> for <see cref="XmlReaderOptions.Default"/>.</param>
    /// <param name="baseUri">The base URI of the document, <see langword="null"/> when unknown.</param>
    /// <returns>The tree.</returns>
    /// <exception cref="XmlParseException">The document is not well formed, uses an unsupported encoding, or exceeds a limit.</exception>
    public static XmlDocumentTree Load(
        ReadOnlySequence<byte> input,
        XmlReaderOptions? options = null,
        Uri? baseUri = null
    )
    {
        options = WithBaseUri(options, baseUri);
        using var reader = XmlSequenceReader.Create(input, options);
        return Build(reader, options, baseUri);
    }

    /// <summary>Loads a document from a stream asynchronously; the encoding follows XML 1.0 Appendix F and the declaration.</summary>
    /// <param name="stream">The stream, read from its current position to its end; it must be readable and is left open.</param>
    /// <param name="options">The options, <see langword="null"/> for <see cref="XmlReaderOptions.Default"/>.</param>
    /// <param name="baseUri">The base URI of the document, <see langword="null"/> when unknown.</param>
    /// <param name="cancellationToken">The token that cancels the load.</param>
    /// <returns>The tree.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="stream"/> is <see langword="null"/>.</exception>
    /// <exception cref="XmlParseException">The document is not well formed, uses an unsupported encoding, or exceeds a limit.</exception>
    /// <exception cref="OperationCanceledException">The token was canceled.</exception>
    public static ValueTask<XmlDocumentTree> LoadAsync(
        System.IO.Stream stream,
        XmlReaderOptions? options = null,
        Uri? baseUri = null,
        CancellationToken cancellationToken = default
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        Argument.ThrowIfNull(stream);

        return LoadCoreAsync(stream, WithBaseUri(options, baseUri), baseUri, cancellationToken);
    }

    // An absolute base URI is the base of the relative system identifier of the document type declaration.
    internal static XmlReaderOptions WithBaseUri(XmlReaderOptions? options, Uri? baseUri)
    {
        options ??= XmlReaderOptions.Default;
        return baseUri is { IsAbsoluteUri: true } ? options.WithBaseUri(baseUri.AbsoluteUri) : options;
    }

    private static async ValueTask<XmlDocumentTree> LoadCoreAsync(
        System.IO.Stream stream,
        XmlReaderOptions options,
        Uri? baseUri,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var builder = new XmlTreeBuilder(options.NameTable, baseUri);
        using var reader = new XmlStreamReader<XmlTreeBuilder>(stream, builder, options, leaveOpen: true);
        var status = await reader.ReadAsync(cancellationToken).ConfigureAwait(false);

        return Complete(builder, status, reader.Diagnostic);
    }

    private static XmlDocumentTree Build<TReader>(TReader reader, XmlReaderOptions options, Uri? baseUri)
        where TReader : IXmlReader
    {
        using var builder = new XmlTreeBuilder(options.NameTable, baseUri);
        var sink = builder;
        var status = reader.Pump(ref sink);

        return Complete(builder, status, reader.Diagnostic);
    }

    private static XmlDocumentTree Complete(XmlTreeBuilder builder, XmlReadStatus status, XmlDiagnostic? diagnostic) =>
        status == XmlReadStatus.Completed ? builder.Complete() : throw new XmlParseException(diagnostic!);
}
