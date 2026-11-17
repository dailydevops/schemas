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
/// Limitation: the size cap, the asynchronous read, and the replay on a second build belong to the shared schema-source
/// holder of the core facade follow-up; until it exists this holder only keeps the source.
/// </remarks>
internal sealed class SchemaSource
{
    private readonly byte[]? _bytes;
    private readonly char[]? _chars;

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

    /// <summary>Gets the copied bytes; empty unless <see cref="Kind"/> is <see cref="SchemaSourceKind.Bytes"/>.</summary>
    internal ReadOnlyMemory<byte> Bytes => _bytes;

    /// <summary>Gets the copied characters; empty unless <see cref="Kind"/> is <see cref="SchemaSourceKind.Chars"/>.</summary>
    internal ReadOnlyMemory<char> Chars => _chars;

    internal static SchemaSource FromStream(Stream stream, string? systemId)
    {
        Argument.ThrowIfNull(stream);

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
