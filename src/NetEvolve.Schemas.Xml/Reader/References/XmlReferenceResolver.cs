namespace NetEvolve.Schemas.Xml.Reader.References;

using System;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Text.CharClasses;

/// <summary>
/// Resolves character references (XML 1.0 Fifth Edition productions 66 to 68) and the five predefined entity
/// references (section 4.6) in complete text or attribute value content.
/// </summary>
/// <remarks>
/// Limitations: declared general entities (XML-15) and parameter entities (XML-20) are not resolved; any other entity
/// name reports <c>XML0008</c>. The input must be complete; a reference cut off at the end is reported as malformed
/// (<c>XML0016</c>), not as a request for more data. Only XML 1.0 character rules apply.
/// </remarks>
internal static class XmlReferenceResolver
{
    private const int MaxCodePoint = 0x10FFFF;

    /// <summary>Parses the one reference that starts at <c>text[0]</c>, which must be an ampersand.</summary>
    /// <param name="text">The text starting at the ampersand.</param>
    /// <param name="length">The number of characters the reference spans, including the semicolon; valid when the reference is complete, even if it is not legal.</param>
    /// <param name="codePoint">The replacement code point; valid when the result is <see langword="null"/>.</param>
    /// <returns><see langword="null"/> on success, otherwise <c>XML0008</c>, <c>XML0009</c>, or <c>XML0016</c>.</returns>
    internal static XmlErrorCode? TryParseReference(ReadOnlySpan<char> text, out int length, out int codePoint)
    {
        length = 0;
        codePoint = 0;
        if (text.IsEmpty || text[0] != '&')
        {
            return XmlErrorCodes.XML0016;
        }

        return text.Length > 1 && text[1] == '#'
            ? ParseCharacterReference(text, out length, out codePoint)
            : ParseEntityReference(text, out length, out codePoint);
    }

    /// <summary>Replaces every reference in <paramref name="text"/>.</summary>
    /// <param name="text">The complete content.</param>
    /// <param name="buffer">Scratch space for the result; <c>text.Length</c> characters suffice because a replacement is never longer than its reference. Not touched when there is no ampersand.</param>
    /// <param name="resolved">The input itself when it has no ampersand, otherwise a slice of <paramref name="buffer"/>.</param>
    /// <param name="errorOffset">The offset of the failing reference in <paramref name="text"/>, or -1 on success.</param>
    /// <returns><see langword="null"/> on success, otherwise the error code of the first failing reference.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="buffer"/> is shorter than <paramref name="text"/> and <paramref name="text"/> has an ampersand.</exception>
    internal static XmlErrorCode? TryResolve(
        ReadOnlySpan<char> text,
        Span<char> buffer,
        out ReadOnlySpan<char> resolved,
        out int errorOffset
    )
    {
        errorOffset = -1;
        var position = text.IndexOf('&');
        if (position < 0)
        {
            resolved = text;
            return null;
        }

        Argument.ThrowIfLessThan(buffer.Length, text.Length);

        text.Slice(0, position).CopyTo(buffer);
        var written = position;
        while (true)
        {
            var error = TryParseReference(text.Slice(position), out var length, out var codePoint);
            if (error is not null)
            {
                resolved = default;
                errorOffset = position;
                return error;
            }

            written += Append(codePoint, buffer.Slice(written));
            position += length;

            var next = text.Slice(position).IndexOf('&');
            var run = next < 0 ? text.Length - position : next;
            text.Slice(position, run).CopyTo(buffer.Slice(written));
            written += run;
            position += run;
            if (next < 0)
            {
                resolved = buffer.Slice(0, written);
                return null;
            }
        }
    }

    private static int Append(int codePoint, Span<char> destination)
    {
        if (codePoint <= 0xFFFF)
        {
            destination[0] = (char)codePoint;
            return 1;
        }

        var offset = codePoint - 0x10000;
        destination[0] = (char)(0xD800 + (offset >> 10));
        destination[1] = (char)(0xDC00 + (offset & 0x3FF));
        return 2;
    }

    // Production 66: '&#' [0-9]+ ';' | '&#x' [0-9a-fA-F]+ ';'. The value saturates above U+10FFFF so a long digit
    // run cannot overflow; it is then rejected by the Legal Character constraint.
    private static XmlErrorCode? ParseCharacterReference(ReadOnlySpan<char> text, out int length, out int codePoint)
    {
        length = 0;
        codePoint = 0;
        var index = 2;
        var hex = index < text.Length && text[index] == 'x';
        if (hex)
        {
            index++;
        }

        var start = index;
        var radix = hex ? 16 : 10;
        var value = 0;
        for (; index < text.Length; index++)
        {
            var digit = DigitValue(text[index]);
            if (digit < 0 || digit >= radix)
            {
                break;
            }

            if (value <= MaxCodePoint)
            {
                value = (value * radix) + digit;
            }
        }

        if (index == start || index >= text.Length || text[index] != ';')
        {
            return XmlErrorCodes.XML0016;
        }

        length = index + 1;
        if (!XmlCharClass.IsChar(value))
        {
            return XmlErrorCodes.XML0009;
        }

        codePoint = value;
        return null;
    }

    // Production 68: '&' Name ';', resolved against the five predefined entities of section 4.6.
    private static XmlErrorCode? ParseEntityReference(ReadOnlySpan<char> text, out int length, out int codePoint)
    {
        length = 0;
        codePoint = 0;
        var index = 1;
        while (index < text.Length)
        {
            var width = 1;
            var current = (int)text[index];
            if (char.IsHighSurrogate(text[index]) && index + 1 < text.Length && char.IsLowSurrogate(text[index + 1]))
            {
                current = char.ConvertToUtf32(text[index], text[index + 1]);
                width = 2;
            }

            if (index == 1 ? !XmlCharClass.IsNameStartChar(current) : !XmlCharClass.IsNameChar(current))
            {
                break;
            }

            index += width;
        }

        if (index == 1 || index >= text.Length || text[index] != ';')
        {
            return XmlErrorCodes.XML0016;
        }

        length = index + 1;
        var name = text.Slice(1, index - 1);
        if (name.SequenceEqual("lt".AsSpan()))
        {
            codePoint = '<';
        }
        else if (name.SequenceEqual("gt".AsSpan()))
        {
            codePoint = '>';
        }
        else if (name.SequenceEqual("amp".AsSpan()))
        {
            codePoint = '&';
        }
        else if (name.SequenceEqual("apos".AsSpan()))
        {
            codePoint = '\'';
        }
        else if (name.SequenceEqual("quot".AsSpan()))
        {
            codePoint = '"';
        }
        else
        {
            return XmlErrorCodes.XML0008;
        }

        return null;
    }

    private static int DigitValue(char c) =>
        (uint)(c - '0') <= 9 ? c - '0'
        : (uint)((c | 0x20) - 'a') <= 5 ? (c | 0x20) - 'a' + 10
        : -1;
}
