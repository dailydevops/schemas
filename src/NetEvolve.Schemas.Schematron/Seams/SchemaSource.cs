namespace NetEvolve.Schemas.Schematron.Seams;

using System;
using System.Buffers;
using System.IO;
using NetEvolve.Arguments;

/// <summary>
/// The schema source a builder holds until <c>Build</c>. Spans and sequences are copied once, so the caller may reuse its
/// buffer; a stream is kept unread and undisposed for the pipeline to read from its current position.
/// </summary>
/// <remarks>
/// Limitation: the shared schema-source holder of the core facade follow-up does not exist, so the read of a stream (the size cap, the
/// asynchronous read) is done by <c>Pipeline.SchemaSourceReader</c>, and this holder keeps the bytes of a stream that was read once, so
/// that a second build replays them: a stream may not seek back and is never read twice.
/// </remarks>
internal sealed class SchemaSource
{
    private readonly byte[]? _bytes;
    private readonly char[]? _chars;
    private byte[]? _replay;

    private SchemaSource(SchemaSourceKind kind, byte[]? bytes, char[]? chars, Stream? stream, string? systemId)
    {
        Kind = kind;
        _bytes = bytes;
        _chars = chars;
        Stream = stream;
        SystemId = systemId;
    }

    internal SchemaSourceKind Kind { get; }

    /// <summary>Gets the stream, <see langword="null"/> unless <see cref="Kind"/> is <see cref="SchemaSourceKind.Stream"/>.</summary>
    internal Stream? Stream { get; }

    /// <summary>Gets the base URI of the schema and the source of every model location, or <see langword="null"/>.</summary>
    internal string? SystemId { get; }

    /// <summary>Gets the bytes of a stream that was read to its end, empty before; see <see cref="Remember"/>.</summary>
    internal ReadOnlyMemory<byte> Replay => _replay;

    /// <summary>Gets a value indicating whether the stream was read to its end and its bytes are kept.</summary>
    internal bool HasReplay => _replay is not null;

    /// <summary>Gets the copied bytes; empty unless <see cref="Kind"/> is <see cref="SchemaSourceKind.Bytes"/>.</summary>
    internal ReadOnlyMemory<byte> Bytes => _bytes;

    /// <summary>Gets the copied characters; empty unless <see cref="Kind"/> is <see cref="SchemaSourceKind.Chars"/>.</summary>
    internal ReadOnlyMemory<char> Chars => _chars;

    /// <summary>Keeps the bytes of the stream, read to its end, for the next build.</summary>
    /// <param name="bytes">The bytes; owned by the source from now on.</param>
    internal void Remember(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);

        _replay = bytes;
    }

    internal static SchemaSource FromStream(Stream stream, string? systemId)
    {
        ArgumentNullException.ThrowIfNull(stream);

        return new(SchemaSourceKind.Stream, null, null, stream, systemId);
    }

    internal static SchemaSource FromBytes(ReadOnlySpan<byte> bytes, string? systemId) =>
        new(SchemaSourceKind.Bytes, bytes.ToArray(), null, null, systemId);

    internal static SchemaSource FromBytes(in ReadOnlySequence<byte> bytes, string? systemId) =>
        new(SchemaSourceKind.Bytes, bytes.ToArray(), null, null, systemId);

    internal static SchemaSource FromChars(ReadOnlySpan<char> chars, string? systemId) =>
        new(SchemaSourceKind.Chars, null, chars.ToArray(), null, systemId);

    internal static SchemaSource FromChars(in ReadOnlySequence<char> chars, string? systemId) =>
        new(SchemaSourceKind.Chars, null, chars.ToArray(), null, systemId);
}
