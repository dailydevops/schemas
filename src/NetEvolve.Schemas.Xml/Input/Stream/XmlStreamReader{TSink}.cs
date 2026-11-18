namespace NetEvolve.Schemas.Xml.Input.Stream;

using System;
using System.Buffers;
using System.Threading;
using System.Threading.Tasks;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Buffers;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Declarations;
using NetEvolve.Schemas.Xml.Dtd.Model;
using NetEvolve.Schemas.Xml.Input.Decoding;
using NetEvolve.Schemas.Xml.Reader.Core;
using NetEvolve.Schemas.Xml.Reader.Declaration;

/// <summary>
/// Reads a <see cref="System.IO.Stream"/> asynchronously into a sink (design #108, AP5): an async fill loop reads bytes into a pooled
/// buffer, the strict decoder turns them into characters, and the chunks feed the resumable parser, so a stream caller
/// gets the same events as the span path without a second parser.
/// </summary>
/// <remarks>
/// The adapter holds no tokenizer logic. It follows the feeder rule of AP2: after the parser needs more data, reading continues until at
/// least <c>max(bufferSize, 2 x pending tail)</c> decoded characters are ready or the stream ends, so work and copying stay linear
/// even for a stream that returns one byte per read. A token larger than the buffer waits in the carry of the parser, bounded by the
/// limits of the options. The encoding follows XML 1.0 Appendix F and the declaration like <c>XmlPipelineReader</c>: the first bytes stay
/// until the declaration is known (bounded by <see cref="XmlReaderLimits.MaxXmlDeclarationLength"/>), then they are decoded with the
/// confirmed encoding. A decoding error is reported after the events of the characters that decoded before it, unless the characters hold an
/// earlier error. The sink is kept in a field so a mutable struct sink keeps its state across awaits; read it by reference
/// through <see cref="Sink"/>. Every buffer returns to its pool when <see cref="ReadAsync"/> ends, by completion, parse error, cancellation, or
/// fault; a token that is already canceled throws before anything is rented, and after a cancellation or fault during a read the reader is unusable. The stream is read until its end (or the first error) and is only disposed by
/// <see cref="Dispose"/> when the reader owns it. Limitations: the buffers of the
/// parser come from the shared pool; an owned stream is disposed synchronously; <c>ReadOnlySequence</c> input is XML-56. Not thread-safe.
/// </remarks>
/// <typeparam name="TSink">The sink type, a class or a mutable struct.</typeparam>
public sealed class XmlStreamReader<TSink> : IDisposable
    where TSink : IXmlSink
{
    private const int DefaultBufferSize = 4096;
    private const string DeclarationStart = "<?xml";
    private const string DeclarationEnd = "?>";

    private enum State : byte
    {
        Idle,
        Running,
        Finished,
        Faulted,
        Disposed,
    }

    private readonly System.IO.Stream _stream;
    private readonly bool _leaveOpen;
    private readonly XmlReaderLimits _limits;
    private readonly int _bufferSize;
    private readonly ArrayPool<byte> _bytePool;
    private readonly ArrayPool<char> _charPool;
    private readonly DtdDoctypeHandler? _doctype;
    private readonly XmlParser _parser;
    private TSink _sink;
    private byte[]? _bytes;
    private PooledBuffer<char>? _chars;
    private XmlInputDecoder? _decoder;
    private XmlErrorCode? _decodeError;
    private XmlDiagnostic? _diagnostic;
    private XmlReadStatus _status;
    private long _fed;
    private State _state;
    private bool _released;

    /// <summary>Initializes a new instance of the <see cref="XmlStreamReader{TSink}"/> class.</summary>
    /// <param name="stream">The stream to read from its current position; it must be readable.</param>
    /// <param name="sink">The sink that receives the events.</param>
    /// <param name="options">The options, <see langword="null"/> for <see cref="XmlReaderOptions.Default"/>.</param>
    /// <param name="bufferSize">The size of the read buffer and the least number of characters per chunk fed to the parser, at least one.</param>
    /// <param name="leaveOpen"><see langword="true"/> to leave the stream open on <see cref="Dispose"/>, <see langword="false"/> to dispose it.</param>
    /// <exception cref="ArgumentNullException"><paramref name="stream"/> or <paramref name="sink"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="bufferSize"/> is less than one.</exception>
    public XmlStreamReader(
        System.IO.Stream stream,
        TSink sink,
        XmlReaderOptions? options = null,
        int bufferSize = DefaultBufferSize,
        bool leaveOpen = true
    )
        : this(stream, sink, options, bufferSize, leaveOpen, null, null) { }

    // The pools are a seam of the tests: a tracking pool proves that every rented buffer returns.
    internal XmlStreamReader(
        System.IO.Stream stream,
        TSink sink,
        XmlReaderOptions? options,
        int bufferSize,
        bool leaveOpen,
        ArrayPool<byte>? bytePool,
        ArrayPool<char>? charPool
    )
    {
        Argument.ThrowIfNull(stream);
        Argument.ThrowIfNull(sink);
        Argument.ThrowIfLessThan(bufferSize, 1);

        options ??= XmlReaderOptions.Default;
        _stream = stream;
        _sink = sink;
        _leaveOpen = leaveOpen;
        _limits = options.Limits;
        _bufferSize = bufferSize;
        _bytePool = bytePool ?? ArrayPool<byte>.Shared;
        _charPool = charPool ?? ArrayPool<char>.Shared;
        _doctype = options.DtdProcessing ? new DtdDoctypeHandler(_limits) : null;
        _parser = new XmlParser(_limits, _doctype);
    }

    /// <summary>Gets the sink by reference; read it after <see cref="ReadAsync"/> returned.</summary>
    public ref TSink Sink => ref _sink;

    /// <summary>Gets the position after the last event that was pumped.</summary>
    public XmlPosition Position => _parser.Position;

    /// <summary>Gets the error, <see langword="null"/> unless <see cref="ReadAsync"/> returned <see cref="XmlReadStatus.Error"/>.</summary>
    public XmlDiagnostic? Diagnostic => _parser.Diagnostic ?? _diagnostic;

    /// <summary>Gets the declarations of the document type declaration, <see langword="null"/> without one or when <see cref="XmlReaderOptions.DtdProcessing"/> is off.</summary>
    internal DtdDeclarations? Declarations => _doctype?.Declarations;

    /// <summary>Reads the stream to its end, or until the first error, and pushes the events into the sink.</summary>
    /// <param name="cancellationToken">The token that cancels the read.</param>
    /// <returns><see cref="XmlReadStatus.Completed"/> or <see cref="XmlReadStatus.Error"/>; a further call returns the same status.</returns>
    /// <exception cref="OperationCanceledException">The token was canceled; the reader is unusable afterwards.</exception>
    /// <exception cref="ObjectDisposedException">The reader is disposed.</exception>
    /// <exception cref="InvalidOperationException">A read is running, or an earlier read ended by cancellation or by an exception of the stream.</exception>
    public async ValueTask<XmlReadStatus> ReadAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        switch (_state)
        {
            case State.Finished:
                return _status;
            case State.Disposed:
                throw Disposed();
            case State.Running:
            case State.Faulted:
                throw new InvalidOperationException(
                    "The reader is already reading, or an earlier read ended by cancellation or by an exception."
                );
            default:
                break;
        }

        _state = State.Running;
        try
        {
            _status = await ReadCoreAsync(cancellationToken).ConfigureAwait(false);
            _state = State.Finished;
            return _status;
        }
        catch
        {
            _state = State.Faulted;
            throw;
        }
        finally
        {
            Release();
        }
    }

    /// <summary>Returns the buffers and, unless the stream was left open, disposes the stream.</summary>
    public void Dispose()
    {
        if (_state == State.Disposed)
        {
            return;
        }

        _state = State.Disposed;
        Release();
        if (!_leaveOpen)
        {
            _stream.Dispose();
        }
    }

    private static ObjectDisposedException Disposed() => new(nameof(XmlStreamReader<TSink>));

    private async ValueTask<XmlReadStatus> ReadCoreAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _bytes = _bytePool.Rent(_bufferSize);
        _chars = new PooledBuffer<char>(_bufferSize, _charPool);

        var ended = await StartAsync(cancellationToken).ConfigureAwait(false);
        while (true)
        {
            var status = Step(ended);
            if (status != XmlReadStatus.NeedMoreData)
            {
                return status;
            }

            if (_decodeError is not null)
            {
                // The characters before the decoding error ended without an error of their own.
                _diagnostic = new XmlDiagnostic(_decodeError, _parser.Position);
                return XmlReadStatus.Error;
            }

            if (ended)
            {
                return Fail(XmlErrorCodes.XML0001);
            }

            ended = await FillAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    // Reads the first bytes until the encoding and the declaration are known, then decodes them. Returns whether the stream ended.
    private async ValueTask<bool> StartAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var head = new PooledBuffer<byte>(Math.Max(_bufferSize, 4), _bytePool);
        while (true)
        {
            var read = await ReadBytesAsync(cancellationToken).ConfigureAwait(false);
            var ended = read == 0;
            head.Append(new ReadOnlySpan<byte>(_bytes, 0, read));
            if ((ended || head.Length >= 4) && TryDecideEncoding(head.WrittenSpan, ended))
            {
                return ended;
            }

            cancellationToken.ThrowIfCancellationRequested();
        }
    }

    // The declaration is read from a probe decoding of the bytes so far; a declaration that is still incomplete asks for more bytes.
    // The bytes are then decoded again from the first one with the confirmed encoding, like the pipeline reader does.
    private bool TryDecideEncoding(ReadOnlySpan<byte> head, bool ended)
    {
        _ = XmlEncodingDetector.TryDetect(head, ended, out var detection);
        if (!XmlInputDecoder.TryCreate(detection, out var probe, out var error))
        {
            _decodeError = error.Code;
            return true;
        }

        var characters = _chars!;
        var decoded = probe!.TryDecode(head, ended, characters, out _);
        var declared = XmlDeclarationParser.Parse(characters.WrittenSpan, XmlDeclarationKind.Xml);
        // A cut inside a pseudo-attribute name is an error of the grammar like any other, so a declaration without its end marker is incomplete.
        var incomplete = declared.IsPresent
            ? !declared.IsValid && characters.WrittenSpan.IndexOf(DeclarationEnd.AsSpan()) < 0
            : DeclarationStart.AsSpan().StartsWith(characters.WrittenSpan);
        if (!ended && decoded && incomplete && characters.Length < _limits.MaxXmlDeclarationLength)
        {
            characters.Clear();
            return false;
        }

        _ = XmlInputDecoder.TryCreate(detection, out var decoder, out _);
        _decoder = decoder;
        if (
            declared.IsValid
            && declared.Encoding.Length > 0
            && !decoder!.TryConfirmEncoding(declared.Encoding, out error)
        )
        {
            characters.Clear();
            _decodeError = error.Code;
            return true;
        }

        characters.Clear();
        if (!decoder!.TryDecode(head, ended, characters, out error))
        {
            _decodeError = error.Code;
        }

        return true;
    }

    // Reads and decodes until enough characters are pending; returns whether the stream ended or decoding stopped on an error.
    private async ValueTask<bool> FillAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var characters = _chars!;
        var tail = Math.Max(0, _fed - _parser.Position.CharOffset);
        var wanted = Math.Max(_bufferSize, 2 * tail);
        while (characters.Length < wanted)
        {
            var read = await ReadBytesAsync(cancellationToken).ConfigureAwait(false);
            if (!_decoder!.TryDecode(new ReadOnlySpan<byte>(_bytes, 0, read), read == 0, characters, out var error))
            {
                _decodeError = error.Code;
                return true;
            }

            if (read == 0)
            {
                return true;
            }

            cancellationToken.ThrowIfCancellationRequested();
        }

        return false;
    }

    private ValueTask<int> ReadBytesAsync(CancellationToken cancellationToken) =>
#if NETSTANDARD2_0
        new(_stream.ReadAsync(_bytes!, 0, _bytes!.Length, cancellationToken));
#else
        _stream.ReadAsync(_bytes.AsMemory(), cancellationToken);
#endif

    // Feeds the pending characters to the parser and pumps; the parser copies what it keeps, so the buffer is reused.
    private XmlReadStatus Step(bool ended)
    {
        var characters = _chars!;
        _parser.Feed(characters.WrittenMemory, ended && _decodeError is null);
        _fed += characters.Length;
        var status = _parser.Pump(ref _sink);
        characters.Clear();
        return status;
    }

    private XmlReadStatus Fail(XmlErrorCode code)
    {
        _diagnostic = new XmlDiagnostic(code, _parser.Position);
        return XmlReadStatus.Error;
    }

    private void Release()
    {
        if (_released)
        {
            return;
        }

        _released = true;
        var bytes = _bytes;
        _bytes = null;
        if (bytes is not null)
        {
            _bytePool.Return(bytes, clearArray: true);
        }

        _chars?.Dispose();
        _chars = null;
        _parser.Dispose();
    }
}
