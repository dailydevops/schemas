namespace NetEvolve.Schemas.Xsd.Compilation;

using System;

/// <summary>One schema document given to the compiler as memory: characters or bytes, with its base URI.</summary>
internal readonly struct SchemaInput
{
    private readonly ReadOnlyMemory<char> _chars;
    private readonly ReadOnlyMemory<byte> _bytes;

    private SchemaInput(ReadOnlyMemory<char> chars, ReadOnlyMemory<byte> bytes, bool isBytes, string? baseUri)
    {
        _chars = chars;
        _bytes = bytes;
        IsBytes = isBytes;
        BaseUri = baseUri;
    }

    /// <summary>Gets a value indicating whether the content is bytes, otherwise characters.</summary>
    public bool IsBytes { get; }

    /// <summary>Gets the base URI of the document, <see langword="null"/> when the caller gave none.</summary>
    public string? BaseUri { get; }

    /// <summary>Gets the characters; empty for byte input.</summary>
    public ReadOnlySpan<char> Chars => _chars.Span;

    /// <summary>Gets the bytes; empty for character input.</summary>
    public ReadOnlySpan<byte> Bytes => _bytes.Span;

    /// <summary>Creates an input of characters.</summary>
    /// <param name="text">The document.</param>
    /// <param name="baseUri">The base URI, or <see langword="null"/>.</param>
    /// <returns>The input.</returns>
    internal static SchemaInput FromChars(ReadOnlyMemory<char> text, string? baseUri) =>
        new(text, default, false, baseUri);

    /// <summary>Creates an input of bytes; the encoding follows XML 1.0 Appendix F.</summary>
    /// <param name="bytes">The document.</param>
    /// <param name="baseUri">The base URI, or <see langword="null"/>.</param>
    /// <returns>The input.</returns>
    internal static SchemaInput FromBytes(ReadOnlyMemory<byte> bytes, string? baseUri) =>
        new(default, bytes, true, baseUri);
}
