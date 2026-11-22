namespace NetEvolve.Schemas.Xsd.Validation.Assembly;

using System;
using System.IO;
using NetEvolve.Arguments;

/// <summary>One schema document that the builder collected: characters, bytes, or a stream that is read at build time.</summary>
/// <remarks>The memory is borrowed until the build returns, and a stream is read from its current position and never disposed.</remarks>
internal readonly struct SchemaSource
{
    private SchemaSource(
        ReadOnlyMemory<char> chars,
        ReadOnlyMemory<byte> bytes,
        bool isBytes,
        Stream? stream,
        string? systemId
    )
    {
        IsBytes = isBytes;
        Chars = chars;
        Bytes = bytes;
        Stream = stream;
        SystemId = systemId;
    }

    /// <summary>Gets a value indicating whether the memory is bytes, otherwise characters; meaningless for a stream.</summary>
    internal bool IsBytes { get; }

    /// <summary>Gets the characters, empty for the other kinds.</summary>
    internal ReadOnlyMemory<char> Chars { get; }

    /// <summary>Gets the bytes, empty for the other kinds.</summary>
    internal ReadOnlyMemory<byte> Bytes { get; }

    /// <summary>Gets the stream, <see langword="null"/> for the memory kinds.</summary>
    internal Stream? Stream { get; }

    /// <summary>Gets the system identifier and base URI, <see langword="null"/> when the caller gave none.</summary>
    internal string? SystemId { get; }

    /// <summary>Creates a source of characters.</summary>
    /// <param name="chars">The document.</param>
    /// <param name="systemId">The system identifier, or <see langword="null"/>.</param>
    /// <returns>The source.</returns>
    internal static SchemaSource FromChars(ReadOnlyMemory<char> chars, string? systemId) =>
        new(chars, default, false, null, systemId);

    /// <summary>Creates a source of bytes.</summary>
    /// <param name="bytes">The document.</param>
    /// <param name="systemId">The system identifier, or <see langword="null"/>.</param>
    /// <returns>The source.</returns>
    internal static SchemaSource FromBytes(ReadOnlyMemory<byte> bytes, string? systemId) =>
        new(default, bytes, true, null, systemId);

    /// <summary>Creates a source that is read from a stream at build time.</summary>
    /// <param name="stream">The stream.</param>
    /// <param name="systemId">The system identifier, or <see langword="null"/>.</param>
    /// <returns>The source.</returns>
    internal static SchemaSource FromStream(Stream stream, string? systemId)
    {
        ArgumentNullException.ThrowIfNull(stream);

        return new(default, default, false, stream, systemId);
    }
}
