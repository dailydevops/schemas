namespace NetEvolve.Schemas.Xsd.Compilation;

using System;

/// <summary>One schema document given to the compiler as memory: characters or bytes, with its base URI.</summary>
internal readonly struct SchemaInput
{
    private readonly ReadOnlyMemory<char> _chars;
    private readonly ReadOnlyMemory<byte> _bytes;

    private SchemaInput(
        ReadOnlyMemory<char> chars,
        ReadOnlyMemory<byte> bytes,
        bool isBytes,
        string? baseUri,
        string? location = null,
        string? namespaceName = null
    )
    {
        _chars = chars;
        _bytes = bytes;
        IsBytes = isBytes;
        BaseUri = baseUri;
        Location = location;
        NamespaceName = namespaceName;
    }

    /// <summary>Gets the reference of a document that the compiler loads through the resolver, <see langword="null"/> for memory.</summary>
    public string? Location { get; }

    /// <summary>Gets the namespace that the caller named for a <see cref="Location"/>, <see langword="null"/> when none.</summary>
    public string? NamespaceName { get; }

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

    /// <summary>Creates an input of a document that the compiler loads through the resolver.</summary>
    /// <param name="location">The reference as written.</param>
    /// <param name="namespaceName">The target namespace that the document must have, or <see langword="null"/>.</param>
    /// <returns>The input.</returns>
    internal static SchemaInput FromLocation(string location, string? namespaceName) =>
        new(default, default, false, null, location, namespaceName is { Length: 0 } ? null : namespaceName);

    /// <summary>Creates an input of bytes; the encoding follows XML 1.0 Appendix F.</summary>
    /// <param name="bytes">The document.</param>
    /// <param name="baseUri">The base URI, or <see langword="null"/>.</param>
    /// <returns>The input.</returns>
    internal static SchemaInput FromBytes(ReadOnlyMemory<byte> bytes, string? baseUri) =>
        new(default, bytes, true, baseUri);
}
