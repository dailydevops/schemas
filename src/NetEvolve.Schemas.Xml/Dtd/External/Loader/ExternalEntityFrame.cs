namespace NetEvolve.Schemas.Xml.Dtd.External.Loader;

using System;
using System.Buffers;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Resources;
using NetEvolve.Schemas.Xml.Buffers;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Input.Decoding;
using NetEvolve.Schemas.Xml.Reader.Declaration;

/// <summary>
/// One open external entity (design #122, section 8): it reads the stream of the resolver in chunks, decodes it with an
/// encoding of its own (XML 1.0 section 4.3.3), consumes the text declaration, normalizes the line ends (section 2.11),
/// and charges the decoded characters to the expansion budget of the loader.
/// </summary>
/// <remarks>
/// Read it with <see cref="TryRead"/> until it returns <see langword="false"/>, then look at <see cref="Error"/>. A chunk
/// is valid until the next call. The stream of the resolver is disposed at the end of the entity, on every error, when
/// the reading throws, and by <see cref="Dispose"/>, whichever comes first. The characters that decoded before an invalid
/// byte are delivered before the error is reported. The encoding follows Appendix F and the text declaration like the
/// reader of documents; the probing of the head is a sync copy of the logic of the stream reader, whose version is private
/// and async. Limitation: an encoding other than UTF-8, UTF-16, UCS-4 (octet orders 1234 and 4321), ISO-8859-1, and
/// US-ASCII is <c>XML4001</c> (DEF-62, DEF-64). Not thread-safe.
/// </remarks>
internal sealed class ExternalEntityFrame : IDisposable
{
    private const int ReadSize = 4096;
    private const int DetectionLength = 4;
    private const string DeclarationStart = "<?xml";
    private const string DeclarationEnd = "?>";

    private readonly ExternalEntityLoader _loader;
    private readonly XmlReaderLimits _limits;
    private readonly CancellationToken _cancellationToken;
    private readonly Stream _stream;
    private byte[]? _bytes;
    private PooledBuffer<char>? _decoded;
    private PooledBuffer<char>? _normalized;
    private XmlInputDecoder? _decoder;
    private XmlErrorCode? _pendingError;
    private long _pendingErrorOffset;
    private int _consumed;
    private bool _ended;
    private bool _pendingCarriageReturn;
    private bool _released;

    private ExternalEntityFrame(
        ExternalEntityLoader loader,
        ResourceKind kind,
        string name,
        string? resolvedUri,
        in ResourceResult result,
        XmlReaderLimits limits,
        CancellationToken cancellationToken
    )
    {
        Argument.ThrowIfNull(loader);
        Argument.ThrowIfNull(name);
        Argument.ThrowIfNull(limits);

        _loader = loader;
        Kind = kind;
        Name = name;
        ResolvedUri = resolvedUri;
        BaseUri = result.SystemId ?? resolvedUri;
        _limits = limits;
        _cancellationToken = cancellationToken;
        _stream = result.Stream ?? StreamOver(result.Content);
    }

    /// <summary>Gets the kind of the entity.</summary>
    internal ResourceKind Kind { get; }

    /// <summary>Gets the entity name, or the document type name of the external subset.</summary>
    internal string Name { get; }

    /// <summary>Gets the absolute URI the system identifier resolved to, <see langword="null"/> without an absolute base.</summary>
    internal string? ResolvedUri { get; }

    /// <summary>Gets the base URI of the entity: the final URI the resolver reported, otherwise <see cref="ResolvedUri"/>. Relative identifiers inside the entity resolve against it.</summary>
    internal string? BaseUri { get; }

    /// <summary>Gets the error that ended the reading, <see langword="null"/> while reading and after a complete read.</summary>
    internal XmlErrorCode? Error { get; private set; }

    /// <summary>Gets the offset of the first offending byte of a <c>XML4003</c> error, counted from the first byte of the entity.</summary>
    internal long ErrorByteOffset { get; private set; }

    /// <summary>Opens a frame over a found result and reads its head; the frame owns the stream of the result.</summary>
    /// <returns><see langword="false"/> when the text declaration or the encoding is wrong; the stream is then disposed.</returns>
    internal static bool TryCreate(
        ExternalEntityLoader loader,
        ResourceKind kind,
        string name,
        string? resolvedUri,
        in ResourceResult result,
        XmlReaderLimits limits,
        CancellationToken cancellationToken,
        out ExternalEntityFrame? frame,
        out XmlErrorCode? error
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        Argument.ThrowIfGreaterThan((int)kind, (int)ResourceKind.ExternalParameterEntity);

        var created = new ExternalEntityFrame(loader, kind, name, resolvedUri, in result, limits, cancellationToken);
        try
        {
            error = created.Start();
        }
        catch
        {
            created.Release();
            throw;
        }

        if (error is not null)
        {
            created.Release();
            frame = null;
            return false;
        }

        frame = created;
        return true;
    }

    /// <summary>Reads the next chunk of the entity.</summary>
    /// <param name="chunk">The characters, valid until the next call.</param>
    /// <returns><see langword="false"/> at the end of the entity or after an error; look at <see cref="Error"/>.</returns>
    internal bool TryRead(out ReadOnlyMemory<char> chunk)
    {
        chunk = default;
        try
        {
            while (!_released)
            {
                _cancellationToken.ThrowIfCancellationRequested();

                var decoded = _decoded!;
                if (_consumed < decoded.Length)
                {
                    if (TryDeliver(out chunk))
                    {
                        return true;
                    }

                    continue;
                }

                decoded.Clear();
                _consumed = 0;
                if (_pendingError is not null)
                {
                    Fail(_pendingError, _pendingErrorOffset);
                }
                else if (_ended)
                {
                    Release();
                }
                else
                {
                    ReadMore();
                }
            }

            return false;
        }
        catch
        {
            Release();
            throw;
        }
    }

    /// <summary>Closes the frame: disposes the stream and gives the depth back to the loader; repeated calls do nothing.</summary>
    public void Dispose()
    {
        Release();
        _loader.Leave(this);
    }

    private static MemoryStream StreamOver(ReadOnlyMemory<byte> content) =>
        MemoryMarshal.TryGetArray(content, out var segment) && segment.Array is not null
            ? new MemoryStream(segment.Array, segment.Offset, segment.Count, writable: false)
            : new MemoryStream(content.ToArray(), writable: false);

    // Reads the first bytes until the encoding and the text declaration are known; returns the fatal error, if any.
    private XmlErrorCode? Start()
    {
        _bytes = ArrayPool<byte>.Shared.Rent(ReadSize);
        _decoded = new PooledBuffer<char>(ReadSize);
        _normalized = new PooledBuffer<char>(ReadSize);
        using var head = new PooledBuffer<byte>(ReadSize);
        while (true)
        {
            _cancellationToken.ThrowIfCancellationRequested();

            var read = _stream.Read(_bytes, 0, ReadSize);
            _ended = read == 0;
            head.Append(new ReadOnlySpan<byte>(_bytes, 0, read));
            if ((_ended || head.Length >= DetectionLength) && TryDecide(head.WrittenSpan, out var error))
            {
                return error;
            }
        }
    }

    // The text declaration is read from a probe decoding of the bytes so far; a declaration that is still incomplete asks
    // for more bytes. The bytes are then decoded again from the first one with the confirmed encoding.
    private bool TryDecide(ReadOnlySpan<byte> head, out XmlErrorCode? error)
    {
        error = null;
        _ = XmlEncodingDetector.TryDetect(head, _ended, out var detection);
        if (!XmlInputDecoder.TryCreate(detection, out var probe, out var createError))
        {
            error = createError.Code;
            return true;
        }

        var characters = _decoded!;
        var decoded = probe!.TryDecode(head, _ended, characters, out _);
        var declared = XmlDeclarationParser.Parse(characters.WrittenSpan, XmlDeclarationKind.Text);
        var incomplete = declared.IsPresent
            ? !declared.IsValid && characters.WrittenSpan.IndexOf(DeclarationEnd.AsSpan()) < 0
            : DeclarationStart.AsSpan().StartsWith(characters.WrittenSpan);
        if (!_ended && decoded && incomplete && characters.Length < _limits.MaxXmlDeclarationLength)
        {
            characters.Clear();
            return false;
        }

        if (declared.IsPresent && !declared.IsValid)
        {
            error = declared.Error;
            return true;
        }

        _ = XmlInputDecoder.TryCreate(detection, out var decoder, out _);
        _decoder = decoder;
        var skip = 0;
        if (declared.IsValid)
        {
            if (!decoder!.TryConfirmEncoding(declared.Encoding, out var confirmError))
            {
                error = confirmError.Code;
                return true;
            }

            skip = declared.Length;
        }

        characters.Clear();
        if (!decoder!.TryDecode(head, _ended, characters, out var decodeError))
        {
            _pendingError = decodeError.Code;
            _pendingErrorOffset = decodeError.ByteOffset;
        }

        _consumed = Math.Min(skip, characters.Length);
        return true;
    }

    private void ReadMore()
    {
        var read = _stream.Read(_bytes!, 0, ReadSize);
        _ended = read == 0;
        if (!_decoder!.TryDecode(new ReadOnlySpan<byte>(_bytes, 0, read), _ended, _decoded!, out var error))
        {
            _pendingError = error.Code;
            _pendingErrorOffset = error.ByteOffset;
        }
    }

    // Charges and normalizes the decoded characters that were not delivered yet; false when nothing is left to deliver.
    private bool TryDeliver(out ReadOnlyMemory<char> chunk)
    {
        chunk = default;
        var decoded = _decoded!;
        var count = decoded.Length - _consumed;
        if (!_loader.TryChargeExpansion(count))
        {
            Fail(XmlErrorCodes.XML1109, 0);
            return false;
        }

        var source = decoded.WrittenSpan.Slice(_consumed);
        if (!_pendingCarriageReturn && source.IndexOf('\r') < 0)
        {
            chunk = decoded.WrittenMemory.Slice(_consumed);
            _consumed = decoded.Length;
            return true;
        }

        var normalized = _normalized!;
        normalized.Clear();
        normalized.EnsureCapacity(count);
        normalized.Advance(Normalize(source, normalized.GetFreeSpan()));
        _consumed = decoded.Length;
        if (normalized.Length == 0)
        {
            return false;
        }

        chunk = normalized.WrittenMemory;
        return true;
    }

    // Section 2.11: CR LF and a lone CR become LF; the state carries a CR at the end of a chunk into the next one.
    private int Normalize(ReadOnlySpan<char> source, Span<char> destination)
    {
        var written = 0;
        foreach (var c in source)
        {
            if (c == '\n' && _pendingCarriageReturn)
            {
                _pendingCarriageReturn = false;
                continue;
            }

            _pendingCarriageReturn = c == '\r';
            destination[written++] = c == '\r' ? '\n' : c;
        }

        return written;
    }

    private void Fail(XmlErrorCode code, long byteOffset)
    {
        Error = code;
        ErrorByteOffset = byteOffset;
        Release();
    }

    private void Release()
    {
        if (_released)
        {
            return;
        }

        _released = true;
        _stream.Dispose();
        var bytes = _bytes;
        _bytes = null;
        if (bytes is not null)
        {
            ArrayPool<byte>.Shared.Return(bytes);
        }

        _decoded?.Dispose();
        _normalized?.Dispose();
    }
}
