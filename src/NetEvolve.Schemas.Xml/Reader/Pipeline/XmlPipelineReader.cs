namespace NetEvolve.Schemas.Xml.Reader.Pipeline;

using System;
using NetEvolve.Schemas.Xml.Buffers;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Input.Decoding;
using NetEvolve.Schemas.Xml.Reader.Core;
using NetEvolve.Schemas.Xml.Reader.Declaration;

/// <summary>
/// The composed reader for complete in-memory input (design #108, AP5 and AP9): the stages run in a fixed order, decode
/// (bytes only, with the encoding found by detection and confirmed by the declaration), normalize line ends, declaration,
/// tokenize, resolve references, and namespaces, and <see cref="Pump{TSink}"/> pushes the events into the sink.
/// </summary>
/// <remarks>
/// The options and their limits reach every stage. A <see cref="ReadOnlySpan{T}"/> input is copied (char) or decoded (byte)
/// at creation because a span cannot outlive the call; a <see cref="ReadOnlyMemory{T}"/> of chars is read in place and must
/// stay unchanged until the first <see cref="Pump{TSink}"/> returns. A decoding error is reported after the events of the
/// characters that decoded before it, unless the characters hold an earlier error. Limitations: the input is complete
/// (chunked and async input are XML-17, <c>ReadOnlySequence</c> input is XML-56); a decoding error reports the offset of the
/// undecodable sequence in the decoded characters and no line or column; the document type declaration is rejected
/// (<c>XML2001</c>). Not thread-safe. Dispose the reader to return its buffers.
/// </remarks>
public sealed class XmlPipelineReader : IXmlReader, IDisposable
{
    private const int DecodeChunkLength = 16_384;

    private readonly XmlParser _parser;
    private readonly PooledBuffer<char>? _owned;
    private readonly XmlErrorCode? _pending;
    private XmlDiagnostic? _diagnostic;

    private XmlPipelineReader(
        XmlReaderOptions options,
        ReadOnlyMemory<char> characters,
        PooledBuffer<char>? owned,
        XmlErrorCode? pending
    )
    {
        _owned = owned;
        _pending = pending;
        _parser = new XmlParser(options.Limits);
        _parser.Feed(characters, pending is null);
    }

    /// <inheritdoc />
    public XmlPosition Position => _parser.Position;

    /// <inheritdoc />
    public XmlDiagnostic? Diagnostic => _parser.Diagnostic ?? _diagnostic;

    /// <summary>Creates a reader over a copy of the characters.</summary>
    /// <param name="input">The document.</param>
    /// <param name="options">The options, <see langword="null"/> for <see cref="XmlReaderOptions.Default"/>.</param>
    /// <returns>The reader.</returns>
    public static XmlPipelineReader Create(ReadOnlySpan<char> input, XmlReaderOptions? options = null)
    {
        var owned = new PooledBuffer<char>(input.Length);
        owned.Append(input);
        return new XmlPipelineReader(options ?? XmlReaderOptions.Default, owned.WrittenMemory, owned, null);
    }

    /// <summary>Creates a reader that decodes the bytes; the encoding follows XML 1.0 Appendix F and the declaration.</summary>
    /// <param name="input">The document.</param>
    /// <param name="options">The options, <see langword="null"/> for <see cref="XmlReaderOptions.Default"/>.</param>
    /// <returns>The reader.</returns>
    public static XmlPipelineReader Create(ReadOnlySpan<byte> input, XmlReaderOptions? options = null)
    {
        options ??= XmlReaderOptions.Default;
        var owned = new PooledBuffer<char>(Math.Min(input.Length, DecodeChunkLength));
        var pending = Decode(input, options.Limits.MaxDocumentCharacters, owned);
        return new XmlPipelineReader(options, owned.WrittenMemory, owned, pending);
    }

    /// <summary>Creates a reader over the characters, which are read in place.</summary>
    /// <param name="input">The document; it must stay unchanged until the first <see cref="Pump{TSink}"/> returns.</param>
    /// <param name="options">The options, <see langword="null"/> for <see cref="XmlReaderOptions.Default"/>.</param>
    /// <returns>The reader.</returns>
    public static XmlPipelineReader CreateFromMemory(ReadOnlyMemory<char> input, XmlReaderOptions? options = null) =>
        new(options ?? XmlReaderOptions.Default, input, null, null);

    /// <summary>Creates a reader that decodes the bytes; the encoding follows XML 1.0 Appendix F and the declaration.</summary>
    /// <param name="input">The document.</param>
    /// <param name="options">The options, <see langword="null"/> for <see cref="XmlReaderOptions.Default"/>.</param>
    /// <returns>The reader.</returns>
    public static XmlPipelineReader CreateFromMemory(ReadOnlyMemory<byte> input, XmlReaderOptions? options = null) =>
        Create(input.Span, options);

    /// <inheritdoc />
    public XmlReadStatus Pump<TSink>(ref TSink sink)
        where TSink : IXmlSink
    {
        var status = _parser.Pump(ref sink);
        if (status == XmlReadStatus.NeedMoreData && _pending is not null)
        {
            // The characters before the decoding error ended without an error of their own.
            _diagnostic = new XmlDiagnostic(_pending, _parser.Position);
            return XmlReadStatus.Error;
        }

        return status;
    }

    /// <summary>Returns the buffers.</summary>
    public void Dispose()
    {
        _parser.Dispose();
        _owned?.Dispose();
    }

    // Decodes with the detected encoding, then again with the declared one when it differs; returns the error that stopped decoding.
    private static XmlErrorCode? Decode(ReadOnlySpan<byte> input, long maxCharacters, PooledBuffer<char> output)
    {
        _ = XmlEncodingDetector.TryDetect(input, true, out var detection);
        if (!XmlInputDecoder.TryCreate(detection, out var decoder, out var error))
        {
            return error.Code;
        }

        var failure = DecodeAll(decoder!, input, maxCharacters, output);
        var declared = XmlDeclarationParser.Parse(output.WrittenSpan, XmlDeclarationKind.Xml);
        if (!declared.IsValid || declared.Encoding.Length == 0)
        {
            return failure;
        }

        var detected = decoder!.Encoding;
        if (!decoder.TryConfirmEncoding(declared.Encoding, out error))
        {
            output.Clear();
            return error.Code;
        }

        if (decoder.Encoding == detected)
        {
            return failure;
        }

        output.Clear();
        return DecodeAll(decoder, input, maxCharacters, output);
    }

    private static XmlErrorCode? DecodeAll(
        XmlInputDecoder decoder,
        ReadOnlySpan<byte> input,
        long maxCharacters,
        PooledBuffer<char> output
    )
    {
        var offset = 0;
        while (true)
        {
            var take = Math.Min(DecodeChunkLength, input.Length - offset);
            var isFinal = offset + take >= input.Length;
            if (!decoder.TryDecode(input.Slice(offset, take), isFinal, output, out var error))
            {
                return error.Code;
            }

            // The parser reports the limit at the character that crosses it; decoding stops once it is crossed.
            if (output.Length > maxCharacters)
            {
                return XmlErrorCodes.XML1101;
            }

            if (isFinal)
            {
                return null;
            }

            offset += take;
        }
    }
}
