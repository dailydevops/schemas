namespace NetEvolve.Schemas.XPath.Functions.Documents;

using System;
using System.Runtime.InteropServices;
using System.Text;
using NetEvolve.Schemas.Xml.Text.CharClasses;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Functions.Strings;

/// <summary>
/// Decodes the bytes of a text resource for <c>fn:unparsed-text</c> and its siblings (Functions and Operators 3.1, 14.9.1): strictly, so
/// that a byte sequence that the encoding does not allow is an error and never a replacement character, and into XML characters only.
/// </summary>
/// <remarks>
/// <para>
/// Without an encoding the resource is UTF-16 when it starts with a byte order mark of UTF-16 and UTF-8 otherwise; the mark is not part of
/// the text. With an encoding the mark that belongs to it is dropped.
/// </para>
/// <para>
/// Limitations: the supported names are <c>UTF-8</c>, <c>UTF-16</c>, <c>UTF-16LE</c>, <c>UTF-16BE</c>, <c>ISO-8859-1</c>, and
/// <c>US-ASCII</c>, matched without regard to case, the set of the XML declaration of the core (DEF-62, DEF-64); every other name is
/// FOUT1190. <c>UTF-16</c> without a byte order mark is big endian (RFC 2781). The resolver gives no media type, so no encoding
/// is inferred from one.
/// </para>
/// </remarks>
internal static class TextDecoding
{
    private static readonly UTF8Encoding Utf8 = new(false, true);
    private static readonly UnicodeEncoding Utf16Le = new(false, false, true);
    private static readonly UnicodeEncoding Utf16Be = new(true, false, true);
    private static readonly Encoding Latin1 = Encoding.GetEncoding("iso-8859-1");

    private enum Kind
    {
        Unknown,
        Inferred,
        Utf8,
        Utf16,
        Utf16Le,
        Utf16Be,
        Latin1,
        Ascii,
    }

    /// <summary>Decodes a resource.</summary>
    /// <param name="content">The bytes.</param>
    /// <param name="encoding">The name of the encoding, <see langword="null"/> to infer it.</param>
    /// <param name="text">The text when the result is <see langword="true"/>.</param>
    /// <param name="error"><see cref="DocumentErrors.FOUT1190"/> or <see cref="DocumentErrors.FOUT1200"/> when the result is <see langword="false"/>.</param>
    /// <returns><see langword="true"/> when the resource decodes into XML characters.</returns>
    internal static bool TryDecode(
        ReadOnlyMemory<byte> content,
        string? encoding,
        out string text,
        out XPathErrorCode error
    )
    {
        text = string.Empty;
        var kind = encoding is null ? Kind.Inferred : Resolve(encoding);
        if (kind == Kind.Unknown)
        {
            error = DocumentErrors.FOUT1190;
            return false;
        }

        kind = SkipMark(content.Span, kind, out var skip);
        if (!TryDecode(content, skip, kind, out text))
        {
            text = string.Empty;
            error = DocumentErrors.FOUT1190;
            return false;
        }

        if (!HasOnlyXmlCharacters(text))
        {
            text = string.Empty;
            error = DocumentErrors.FOUT1200;
            return false;
        }

        error = default;
        return true;
    }

    private static Kind Resolve(string name) =>
        Is(name, "UTF-8") ? Kind.Utf8
        : Is(name, "UTF-16") ? Kind.Utf16
        : Is(name, "UTF-16LE") ? Kind.Utf16Le
        : Is(name, "UTF-16BE") ? Kind.Utf16Be
        : Is(name, "ISO-8859-1") ? Kind.Latin1
        : Is(name, "US-ASCII") ? Kind.Ascii
        : Kind.Unknown;

    private static bool Is(string name, string expected) =>
        string.Equals(name, expected, StringComparison.OrdinalIgnoreCase);

    // The byte order mark that belongs to the encoding is skipped; an inferred or unmarked UTF-16 becomes the concrete order.
    private static Kind SkipMark(ReadOnlySpan<byte> bytes, Kind kind, out int skip)
    {
        var utf8Mark = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;
        var bigEndianMark = bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF;
        var littleEndianMark = bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE;
        skip = 0;
        switch (kind)
        {
            case Kind.Inferred when bigEndianMark:
            case Kind.Utf16 when bigEndianMark:
            case Kind.Utf16Be when bigEndianMark:
                skip = 2;
                return Kind.Utf16Be;
            case Kind.Inferred when littleEndianMark:
            case Kind.Utf16 when littleEndianMark:
            case Kind.Utf16Le when littleEndianMark:
                skip = 2;
                return Kind.Utf16Le;
            case Kind.Inferred:
            case Kind.Utf8:
                skip = utf8Mark ? 3 : 0;
                return Kind.Utf8;
            case Kind.Utf16:
                return Kind.Utf16Be;
            default:
                return kind;
        }
    }

    private static bool TryDecode(ReadOnlyMemory<byte> content, int skip, Kind kind, out string text)
    {
        var segment = MemoryMarshal.TryGetArray(content, out var found)
            ? found
            : new ArraySegment<byte>(content.ToArray());
        var array = segment.Array ?? [];
        var offset = segment.Offset + skip;
        var count = segment.Count - skip;
        text = string.Empty;
        if (kind == Kind.Ascii)
        {
            if (!IsAscii(array, offset, count))
            {
                return false;
            }

            text = Encoding.ASCII.GetString(array, offset, count);
            return true;
        }

        var decoder = kind switch
        {
            Kind.Utf16Le => Utf16Le,
            Kind.Utf16Be => Utf16Be,
            Kind.Latin1 => Latin1,
            _ => (Encoding)Utf8,
        };
        try
        {
            text = decoder.GetString(array, offset, count);
            return true;
        }
        catch (DecoderFallbackException)
        {
            return false;
        }
    }

    private static bool IsAscii(byte[] array, int offset, int count)
    {
        for (var i = offset; i < offset + count; i++)
        {
            if (array[i] > 0x7F)
            {
                return false;
            }
        }

        return true;
    }

    private static bool HasOnlyXmlCharacters(string text)
    {
        var span = text.AsSpan();
        var index = 0;
        while (index < span.Length)
        {
            if (!XmlCharClass.IsChar(CodePoints.Next(span, ref index)))
            {
                return false;
            }
        }

        return true;
    }
}
