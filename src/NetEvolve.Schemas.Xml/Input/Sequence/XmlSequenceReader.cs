namespace NetEvolve.Schemas.Xml.Input.Sequence;

using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Reader.Pipeline;

/// <summary>
/// The reader for input in a <see cref="ReadOnlySequence{T}"/> of bytes or characters (design #108, AP5): the segments feed the
/// resumable decoder and the parser one after the other, so a sequence gives the same events, positions, and errors as the same
/// document in one contiguous span, without flattening it into one array.
/// </summary>
/// <remarks>
/// A sequence of one segment takes the span path of <see cref="XmlPipelineReader"/> directly. A reader is a handle: copies share
/// one state, and <see cref="Dispose"/> on any copy returns the buffers. <see cref="Pump{TSink}"/> reads the whole sequence and
/// returns <see cref="XmlReadStatus.Completed"/> or <see cref="XmlReadStatus.Error"/>, never
/// <see cref="XmlReadStatus.NeedMoreData"/>. Limitations: a sequence of several segments is fed in chunks, so every
/// character of it is copied once into a pooled chunk buffer (large segments are not fed in place), and a token that spans a
/// chunk edge is also kept in the carry of the parser; both buffers are bounded by the configured limits; the sequence must
/// stay valid and unchanged until
/// <see cref="Dispose"/>; the encoding of a byte sequence is decided from its first <c>4 x MaxXmlDeclarationLength + 4</c> bytes, so
/// a declaration longer than that reports the limit instead of a conflicting declared encoding; the document type declaration is
/// handled as by <see cref="XmlPipelineReader"/>. Not thread-safe. The benchmarks of this path belong to the reader benchmarks.
/// </remarks>
public readonly struct XmlSequenceReader : IXmlReader, IDisposable, IEquatable<XmlSequenceReader>
{
    private readonly XmlPipelineReader? _span;
    private readonly XmlSequenceFeeder? _feeder;

    private XmlSequenceReader(XmlPipelineReader span)
    {
        _span = span;
        _feeder = null;
    }

    private XmlSequenceReader(XmlSequenceFeeder feeder)
    {
        _span = null;
        _feeder = feeder;
    }

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">The reader is a default value, not created by <c>Create</c>.</exception>
    public XmlPosition Position => _span?.Position ?? Feeder.Position;

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">The reader is a default value, not created by <c>Create</c>.</exception>
    public XmlDiagnostic? Diagnostic => _span is not null ? _span.Diagnostic : Feeder.Diagnostic;

    private XmlSequenceFeeder Feeder =>
        _feeder ?? throw new InvalidOperationException("The reader is a default value; create it with Create.");

    /// <summary>Creates a reader over the bytes; the encoding follows XML 1.0 Appendix F and the declaration.</summary>
    /// <param name="input">The document; it must stay valid and unchanged until the reader is disposed.</param>
    /// <param name="options">The options, <see langword="null"/> for <see cref="XmlReaderOptions.Default"/>.</param>
    /// <returns>The reader.</returns>
    public static XmlSequenceReader Create(ReadOnlySequence<byte> input, XmlReaderOptions? options = null) =>
        Create(input, options, null, null);

    /// <summary>Creates a reader over the characters.</summary>
    /// <param name="input">The document; it must stay valid and unchanged until the reader is disposed.</param>
    /// <param name="options">The options, <see langword="null"/> for <see cref="XmlReaderOptions.Default"/>.</param>
    /// <returns>The reader.</returns>
    public static XmlSequenceReader Create(ReadOnlySequence<char> input, XmlReaderOptions? options = null) =>
        Create(input, options, null);

    // The pools are a seam of the tests: a tracking pool proves that every rented buffer returns.
    internal static XmlSequenceReader Create(
        ReadOnlySequence<byte> input,
        XmlReaderOptions? options,
        ArrayPool<byte>? bytePool,
        ArrayPool<char>? charPool
    )
    {
        options ??= XmlReaderOptions.Default;
        return input.IsSingleSegment
            ? new XmlSequenceReader(XmlPipelineReader.Create(input.First.Span, options))
            : new XmlSequenceReader(new XmlByteSequenceFeeder(input, options, bytePool, charPool));
    }

    internal static XmlSequenceReader Create(
        ReadOnlySequence<char> input,
        XmlReaderOptions? options,
        ArrayPool<char>? charPool
    )
    {
        options ??= XmlReaderOptions.Default;
        return input.IsSingleSegment
            ? new XmlSequenceReader(XmlPipelineReader.CreateFromMemory(input.First, options))
            : new XmlSequenceReader(new XmlCharSequenceFeeder(input, options, charPool));
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentNullException"><paramref name="sink"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">The reader is a default value, not created by <c>Create</c>.</exception>
    public XmlReadStatus Pump<TSink>(ref TSink sink)
        where TSink : IXmlSink
    {
        // A struct sink cannot be null, and boxing it for the guard would allocate on every pump.
        if (!typeof(TSink).IsValueType)
        {
            ArgumentNullException.ThrowIfNull(sink);
        }

        return _span is not null ? _span.Pump(ref sink) : Feeder.Pump(ref sink);
    }

    /// <summary>Returns the buffers.</summary>
    public void Dispose()
    {
        _span?.Dispose();
        _feeder?.Dispose();
    }

    /// <inheritdoc />
    public bool Equals(XmlSequenceReader other) =>
        ReferenceEquals(_span, other._span) && ReferenceEquals(_feeder, other._feeder);

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is XmlSequenceReader other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => RuntimeHelpers.GetHashCode((object?)_span ?? _feeder);

    /// <summary>Determines whether two handles share one state.</summary>
    /// <param name="left">The first handle.</param>
    /// <param name="right">The second handle.</param>
    /// <returns><see langword="true"/> when both handles share one state.</returns>
    public static bool operator ==(XmlSequenceReader left, XmlSequenceReader right) => left.Equals(right);

    /// <summary>Determines whether two handles do not share one state.</summary>
    /// <param name="left">The first handle.</param>
    /// <param name="right">The second handle.</param>
    /// <returns><see langword="true"/> when the handles do not share one state.</returns>
    public static bool operator !=(XmlSequenceReader left, XmlSequenceReader right) => !left.Equals(right);
}
