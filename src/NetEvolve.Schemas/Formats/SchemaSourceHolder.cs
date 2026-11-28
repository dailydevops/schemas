namespace NetEvolve.Schemas.Formats;

using System;
using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// The source of one schema document that a builder holds until <c>Build</c>. Spans and sequences are copied once, so the caller may
/// reuse its buffer; a stream is kept unread and undisposed and read from its current position when the bytes are asked for.
/// </summary>
/// <remarks>
/// <para>
/// A schema is a resource, not an instance input, so every read is bounded by <see cref="ValidationLimits.MaxResourceSize"/>: bytes
/// count as bytes and characters as characters. A schema over the limit is a <see cref="SchemaCompilationException"/> with
/// <see cref="CoreDiagnosticCodes.ResourceSizeLimitExceeded"/>.
/// </para>
/// <para>
/// The bytes of a stream that was read are kept, so a second build replays them and a stream that cannot seek is never read twice.
/// Limitation: the kept bytes live as long as the holder. The holder is not thread-safe and owns only managed arrays, so it is not
/// <see cref="IDisposable"/>; a stream is never disposed by it.
/// </para>
/// </remarks>
public sealed class SchemaSourceHolder
{
    private readonly byte[]? _bytes;
    private readonly char[]? _chars;
    private readonly Stream? _stream;
    private byte[]? _replay;

    private SchemaSourceHolder(DocumentSourceKind kind, byte[]? bytes, char[]? chars, Stream? stream, string? systemId)
    {
        Kind = kind;
        _bytes = bytes;
        _chars = chars;
        _stream = stream;
        SystemId = systemId;
    }

    /// <summary>Gets the base URI of the schema and the source of every location, or <see langword="null"/>.</summary>
    public string? SystemId { get; }

    /// <summary>Gets <see cref="DocumentSourceKind.Chars"/> for a text source and <see cref="DocumentSourceKind.Bytes"/> for bytes and for a stream.</summary>
    public DocumentSourceKind Kind { get; }

    /// <summary>Creates a holder with a copy of the bytes.</summary>
    /// <param name="content">The encoded schema; copied.</param>
    /// <param name="systemId">The base URI of the schema, or <see langword="null"/>.</param>
    /// <returns>The holder.</returns>
    public static SchemaSourceHolder FromBytes(ReadOnlySpan<byte> content, string? systemId = null) =>
        new(DocumentSourceKind.Bytes, content.ToArray(), null, null, systemId);

    /// <summary>Creates a holder with a copy of the bytes of a sequence.</summary>
    /// <param name="content">The encoded schema; copied.</param>
    /// <param name="systemId">The base URI of the schema, or <see langword="null"/>.</param>
    /// <returns>The holder.</returns>
    public static SchemaSourceHolder FromBytes(in ReadOnlySequence<byte> content, string? systemId = null) =>
        new(DocumentSourceKind.Bytes, content.ToArray(), null, null, systemId);

    /// <summary>Creates a holder with a copy of the characters.</summary>
    /// <param name="content">The decoded schema; copied.</param>
    /// <param name="systemId">The base URI of the schema, or <see langword="null"/>.</param>
    /// <returns>The holder.</returns>
    public static SchemaSourceHolder FromChars(ReadOnlySpan<char> content, string? systemId = null) =>
        new(DocumentSourceKind.Chars, null, content.ToArray(), null, systemId);

    /// <summary>Creates a holder with a copy of the characters of a sequence.</summary>
    /// <param name="content">The decoded schema; copied.</param>
    /// <param name="systemId">The base URI of the schema, or <see langword="null"/>.</param>
    /// <returns>The holder.</returns>
    public static SchemaSourceHolder FromChars(in ReadOnlySequence<char> content, string? systemId = null) =>
        new(DocumentSourceKind.Chars, null, content.ToArray(), null, systemId);

    /// <summary>Creates a holder over a stream that is kept unread and never disposed.</summary>
    /// <param name="stream">The stream; read from its current position to its end when the bytes are asked for.</param>
    /// <param name="systemId">The base URI of the schema, or <see langword="null"/>.</param>
    /// <returns>The holder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="stream"/> is <see langword="null"/>.</exception>
    public static SchemaSourceHolder FromStream(Stream stream, string? systemId = null)
    {
        ArgumentNullException.ThrowIfNull(stream);

        return new(DocumentSourceKind.Bytes, null, null, stream, systemId);
    }

    /// <summary>Gets the bytes of a schema in bytes or in a stream, reading the stream when it has not been read.</summary>
    /// <param name="limits">The limits of the build.</param>
    /// <param name="cancellationToken">The token of the build.</param>
    /// <returns>The bytes of the schema.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="limits"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">The holder is a text source.</exception>
    /// <exception cref="OperationCanceledException">The token is canceled.</exception>
    /// <exception cref="SchemaCompilationException">The schema is larger than <see cref="ValidationLimits.MaxResourceSize"/>.</exception>
    public ReadOnlyMemory<byte> GetBytes(ValidationLimits limits, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(limits);
        if (_chars is not null)
        {
            throw new InvalidOperationException("The schema source is text; use GetChars.");
        }

        if (_stream is not null && _replay is null)
        {
            _replay = ReadStream(_stream, limits, cancellationToken);
        }

        var bytes = _stream is null ? _bytes! : _replay!;
        return bytes.Length > limits.MaxResourceSize ? throw TooLarge(limits) : bytes;
    }

    /// <summary>Gets the characters of a schema in text, checked against the limit.</summary>
    /// <param name="limits">The limits of the build.</param>
    /// <returns>The characters of the schema.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="limits"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">The holder is not a text source.</exception>
    /// <exception cref="SchemaCompilationException">The schema is larger than <see cref="ValidationLimits.MaxResourceSize"/>.</exception>
    public ReadOnlyMemory<char> GetChars(ValidationLimits limits)
    {
        ArgumentNullException.ThrowIfNull(limits);
        if (_chars is null)
        {
            throw new InvalidOperationException("The schema source is not text; use GetBytes.");
        }

        return _chars.Length > limits.MaxResourceSize ? throw TooLarge(limits) : _chars;
    }

    /// <summary>Reads a stream asynchronously and keeps its bytes; a later <see cref="GetBytes"/> then finds them.</summary>
    /// <param name="limits">The limits of the build.</param>
    /// <param name="cancellationToken">The token of the build, observed at every read.</param>
    /// <returns>A task that completes when the stream is read; it does nothing for a source in memory or a stream that was read.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="limits"/> is <see langword="null"/>.</exception>
    /// <exception cref="OperationCanceledException">The token is canceled.</exception>
    /// <exception cref="SchemaCompilationException">The stream is larger than <see cref="ValidationLimits.MaxResourceSize"/>.</exception>
    public async ValueTask PrefetchAsync(ValidationLimits limits, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(limits);

        if (_stream is null || _replay is not null)
        {
            return;
        }

        if (ExceedsLimit(_stream, limits))
        {
            throw TooLarge(limits);
        }

        using var buffer = new PooledStreamBuffer(_stream, limits.MaxResourceSize);
        int read;
        do
        {
            cancellationToken.ThrowIfCancellationRequested();
#if NETSTANDARD2_0
            read = await _stream
                .ReadAsync(buffer.Array, buffer.Offset, buffer.Count, cancellationToken)
                .ConfigureAwait(false);
#else
            read = await _stream.ReadAsync(buffer.Window, cancellationToken).ConfigureAwait(false);
#endif
        } while (read > 0 && buffer.Advance(read));

        cancellationToken.ThrowIfCancellationRequested();
        _replay = read > 0 ? throw TooLarge(limits) : buffer.Content.ToArray();
    }

    private byte[] ReadStream(Stream stream, ValidationLimits limits, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (ExceedsLimit(stream, limits))
        {
            throw TooLarge(limits);
        }

        using var buffer = new PooledStreamBuffer(stream, limits.MaxResourceSize);
        int read;
        do
        {
            cancellationToken.ThrowIfCancellationRequested();
            read = stream.Read(buffer.Array, buffer.Offset, buffer.Count);
        } while (read > 0 && buffer.Advance(read));

        return read > 0 ? throw TooLarge(limits) : buffer.Content.ToArray();
    }

    // A seekable stream that is already too long is refused without reading a byte; the read loop covers a wrong Length.
    private static bool ExceedsLimit(Stream stream, ValidationLimits limits) =>
        stream.CanSeek && stream.Length - stream.Position > limits.MaxResourceSize;

    private SchemaCompilationException TooLarge(ValidationLimits limits) =>
        new([
            new ValidationIssue(
                CoreDiagnosticCodes.ResourceSizeLimitExceeded,
                IssueSeverity.Error,
                "The schema is larger than " + limits.MaxResourceSize + " (MaxResourceSize).",
                new DocumentLocation(SystemId, 0, 0)
            ),
        ]);
}
