#pragma warning disable CA1065, MA0025 // Contract stub: the owning work package replaces the throwing bodies.

namespace NetEvolve.Schemas.Formats;

using System;
using System.Buffers;

/// <summary>The input of a format core, carrying spans, so it cannot be stored or cross an await.</summary>
/// <remarks>Limitation: members throw until FND-56 lands. There is no public constructor.</remarks>
public readonly ref struct DocumentSource
{
    /// <summary>Gets whether the content is bytes or characters.</summary>
    public DocumentSourceKind Kind => throw Stub.For("FND-56");

    /// <summary>Gets the system identifier and base URI of the document.</summary>
    public string? SystemId => throw Stub.For("FND-56");

    /// <summary>Gets a value indicating whether the content is contiguous.</summary>
    public bool IsSingleSegment => throw Stub.For("FND-56");

    /// <summary>Gets the length in bytes for <see cref="DocumentSourceKind.Bytes"/>, in characters for <see cref="DocumentSourceKind.Chars"/>.</summary>
    public long Length => throw Stub.For("FND-56");

    /// <summary>Gets the contiguous bytes, otherwise empty.</summary>
    public ReadOnlySpan<byte> Bytes => throw Stub.For("FND-56");

    /// <summary>Gets the contiguous characters, otherwise empty.</summary>
    public ReadOnlySpan<char> Chars => throw Stub.For("FND-56");

    /// <summary>Gets the multi-segment bytes, otherwise empty.</summary>
    public ReadOnlySequence<byte> ByteSequence => throw Stub.For("FND-56");

    /// <summary>Gets the multi-segment characters, otherwise empty.</summary>
    public ReadOnlySequence<char> CharSequence => throw Stub.For("FND-56");
}
