namespace NetEvolve.Schemas.Formats;

using System;
using System.Buffers;

/// <summary>The input of a format core, carrying spans, so it cannot be stored or cross an await.</summary>
/// <remarks>There is no public constructor; only <see cref="SchemaValidatorBase"/> creates instances. <see langword="default"/> is a valid empty byte document.</remarks>
public readonly ref struct DocumentSource
{
    private readonly ReadOnlySpan<byte> _bytes;
    private readonly ReadOnlySpan<char> _chars;
    private readonly ReadOnlySequence<byte> _byteSequence;
    private readonly ReadOnlySequence<char> _charSequence;
    private readonly string? _systemId;
    private readonly DocumentSourceKind _kind;
    private readonly bool _isMultiSegment;

    private DocumentSource(
        DocumentSourceKind kind,
        string? systemId,
        ReadOnlySpan<byte> bytes,
        ReadOnlySpan<char> chars,
        ReadOnlySequence<byte> byteSequence,
        ReadOnlySequence<char> charSequence,
        bool isMultiSegment
    )
    {
        _kind = kind;
        _systemId = systemId;
        _bytes = bytes;
        _chars = chars;
        _byteSequence = byteSequence;
        _charSequence = charSequence;
        _isMultiSegment = isMultiSegment;
    }

    /// <summary>Gets whether the content is bytes or characters.</summary>
    public DocumentSourceKind Kind => _kind;

    /// <summary>Gets the system identifier and base URI of the document.</summary>
    public string? SystemId => _systemId;

    /// <summary>Gets a value indicating whether the content is contiguous.</summary>
    public bool IsSingleSegment => !_isMultiSegment;

    /// <summary>Gets the length in bytes for <see cref="DocumentSourceKind.Bytes"/>, in characters for <see cref="DocumentSourceKind.Chars"/>.</summary>
    public long Length =>
        (_kind, _isMultiSegment) switch
        {
            (DocumentSourceKind.Bytes, false) => _bytes.Length,
            (DocumentSourceKind.Bytes, true) => _byteSequence.Length,
            (_, false) => _chars.Length,
            _ => _charSequence.Length,
        };

    /// <summary>Gets the contiguous bytes, otherwise empty.</summary>
    public ReadOnlySpan<byte> Bytes => _bytes;

    /// <summary>Gets the contiguous characters, otherwise empty.</summary>
    public ReadOnlySpan<char> Chars => _chars;

    /// <summary>Gets the multi-segment bytes, otherwise empty.</summary>
    public ReadOnlySequence<byte> ByteSequence => _byteSequence;

    /// <summary>Gets the multi-segment characters, otherwise empty.</summary>
    public ReadOnlySequence<char> CharSequence => _charSequence;

    internal static DocumentSource Create(ReadOnlySpan<byte> content, string? systemId) =>
        new(DocumentSourceKind.Bytes, systemId, content, default, default, default, false);

    internal static DocumentSource Create(ReadOnlySpan<char> content, string? systemId) =>
        new(DocumentSourceKind.Chars, systemId, default, content, default, default, false);

    // A single-segment sequence is unwrapped without a copy; First.Span is the portable spelling (FirstSpan is missing on netstandard2.0).
    internal static DocumentSource Create(in ReadOnlySequence<byte> content, string? systemId) =>
        content.IsSingleSegment
            ? Create(content.First.Span, systemId)
            : new(DocumentSourceKind.Bytes, systemId, default, default, content, default, true);

    internal static DocumentSource Create(in ReadOnlySequence<char> content, string? systemId) =>
        content.IsSingleSegment
            ? Create(content.First.Span, systemId)
            : new(DocumentSourceKind.Chars, systemId, default, default, default, content, true);
}
