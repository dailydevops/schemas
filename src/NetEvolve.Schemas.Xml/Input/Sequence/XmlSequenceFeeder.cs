namespace NetEvolve.Schemas.Xml.Input.Sequence;

using System;
using System.Buffers;
using NetEvolve.Schemas.Xml.Buffers;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Declarations;
using NetEvolve.Schemas.Xml.Input.Decoding;
using NetEvolve.Schemas.Xml.Reader.Core;
using NetEvolve.Schemas.Xml.Reader.EntityInput;

/// <summary>
/// Feeds the segments of a multi-segment sequence to the resumable parser (design #108, AP5): a derived class turns the next
/// pieces of the sequence into characters, this class feeds them in chunks and pumps. It holds no tokenizer logic.
/// </summary>
/// <remarks>
/// The chunk follows the feeder rule of AP2: after the parser needs more data, characters are collected until there are at
/// least <c>max(chunk, 2 x pending tail)</c> or the sequence ends, so work and copying stay linear even for one-element
/// segments. A decoding error is reported after the events of the characters before it, unless they hold an earlier
/// error. Not thread-safe.
/// </remarks>
internal abstract class XmlSequenceFeeder : IDisposable
{
    private const int MinimumChunk = 4096;

    // 2^28 characters (512 MiB) bound one chunk; the pending tail itself is bounded by the reader limits, so the chunk stays bounded too.
    private const int MaximumChunk = 1 << 28;

    private readonly XmlParser _parser;
    private XmlErrorCode? _decodeError;
    private XmlDiagnostic? _diagnostic;
    private XmlReadStatus? _status;
    private long _fed;

    private protected XmlSequenceFeeder(XmlReaderOptions options, ArrayPool<char>? charPool)
    {
        ArgumentNullException.ThrowIfNull(options);

        Limits = options.Limits;
        Characters = new PooledBuffer<char>(MinimumChunk, charPool);
        var doctype = options.DtdProcessing ? new DtdDoctypeHandler(Limits) : null;
        _parser = new XmlParser(Limits, doctype, doctype is null ? null : new XmlEntityInputStack(Limits, doctype));
    }

    /// <summary>Gets the position after the last event that was pumped.</summary>
    internal XmlPosition Position => _parser.Position;

    internal XmlPosition EventPosition => _parser.EventPosition;

    /// <summary>Gets the error, <see langword="null"/> unless <see cref="Pump{TSink}"/> returned <see cref="XmlReadStatus.Error"/>.</summary>
    internal XmlDiagnostic? Diagnostic => _parser.Diagnostic ?? _diagnostic;

    private protected XmlReaderLimits Limits { get; }

    /// <summary>Gets the characters that wait for the next feed.</summary>
    private protected PooledBuffer<char> Characters { get; }

    /// <summary>Reads the sequence to its end, or until the first error, and pushes the events into the sink.</summary>
    /// <typeparam name="TSink">The sink type.</typeparam>
    /// <param name="sink">The sink.</param>
    /// <returns><see cref="XmlReadStatus.Completed"/> or <see cref="XmlReadStatus.Error"/>; a further call returns the same status.</returns>
    internal XmlReadStatus Pump<TSink>(ref TSink sink)
        where TSink : IXmlSink
    {
        if (_status is { } done)
        {
            return done;
        }

        var status = PumpCore(ref sink);
        _status = status;
        return status;
    }

    /// <summary>Returns the buffers.</summary>
    public void Dispose()
    {
        _parser.Dispose();
        Characters.Dispose();
    }

    /// <summary>Decides the encoding before the first read; a failure is reported through <see cref="Fail"/>.</summary>
    private protected abstract void Start();

    /// <summary>Appends characters to <see cref="Characters"/> until there are <paramref name="wanted"/> of them, the sequence ends, or decoding fails.</summary>
    /// <param name="wanted">The number of characters to collect.</param>
    /// <returns><see langword="true"/> when the sequence ended or decoding stopped on an error.</returns>
    internal abstract bool Fill(int wanted);

    /// <summary>Records the error that stops decoding; it is reported after the events of the characters before it.</summary>
    /// <param name="error">The decoding error.</param>
    private protected void Fail(XmlDecodingError error) => _decodeError = error.Code;

    private XmlReadStatus PumpCore<TSink>(ref TSink sink)
        where TSink : IXmlSink
    {
        Start();
        while (true)
        {
            var tail = Math.Max(0, _fed - _parser.Position.CharOffset);
            var ended = Fill((int)Math.Min(MaximumChunk, Math.Max(MinimumChunk, 2 * tail)));

            var characters = Characters;
            _parser.Feed(characters.WrittenMemory, ended && _decodeError is null);
            _fed += characters.Length;
            var status = _parser.Pump(ref sink);
            characters.Clear();
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
        }
    }
}
