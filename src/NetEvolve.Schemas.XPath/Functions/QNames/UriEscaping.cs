namespace NetEvolve.Schemas.XPath.Functions.QNames;

using System;
using System.Buffers;
using NetEvolve.Arguments;

/// <summary>
/// The percent-encoding of <c>fn:encode-for-uri</c> and <c>fn:iri-to-uri</c> (Functions and Operators 1.0, 8.4 and 8.5): the UTF-8
/// octets of a character, each as <c>%</c> and two upper-case hexadecimal digits. A string without a character to escape is returned as
/// it is; otherwise the result is built in a stack buffer that grows into a pooled array, so only the result string is allocated.
/// </summary>
/// <remarks>
/// A lone surrogate has no UTF-8 form; it is escaped as U+FFFD (<c>%EF%BF%BD</c>), which is what the UTF-8 encoder of the platform does
/// (#270, decision).
/// </remarks>
internal static class UriEscaping
{
    private const int StackLength = 256;
    private const string HexDigits = "0123456789ABCDEF";

    /// <summary>Escapes every character but the unreserved ones of RFC 3986 (letters, digits, <c>-</c>, <c>_</c>, <c>.</c>, <c>~</c>).</summary>
    /// <param name="text">The text.</param>
    /// <returns>The escaped text; <paramref name="text"/> itself when nothing needs escaping.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="text"/> is <see langword="null"/>.</exception>
    internal static string EncodeForUri(string text) => Escape(text, unreservedOnly: true);

    /// <summary>Escapes the characters that no URI may contain: control characters, space, <c>&lt;&gt;"{}|\^`</c>, and every character beyond ASCII.</summary>
    /// <param name="text">The text.</param>
    /// <returns>The escaped text; <paramref name="text"/> itself when nothing needs escaping.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="text"/> is <see langword="null"/>.</exception>
    internal static string IriToUri(string text) => Escape(text, unreservedOnly: false);

    /// <summary>Determines whether a character may not appear in a URI reference.</summary>
    /// <param name="c">The character.</param>
    /// <returns><see langword="true"/> for a control character, space, <c>&lt;&gt;"{}|\^`</c>, and a character beyond ASCII.</returns>
    internal static bool IsForbiddenInUri(char c) =>
        c <= 0x20 || c >= 0x7F || c is '<' or '>' or '"' or '{' or '}' or '|' or '\\' or '^' or '`';

    private static string Escape(string text, bool unreservedOnly)
    {
        Argument.ThrowIfNull(text);

        var length = Measure(text, unreservedOnly);
        if (length == text.Length)
        {
            return text;
        }

        if (length <= StackLength)
        {
            Span<char> stack = stackalloc char[StackLength];
            return Write(text, unreservedOnly, stack).ToString();
        }

        var rented = ArrayPool<char>.Shared.Rent(length);
        try
        {
            return Write(text, unreservedOnly, rented).ToString();
        }
        finally
        {
            ArrayPool<char>.Shared.Return(rented);
        }
    }

    private static bool Keeps(char c, bool unreservedOnly) =>
        unreservedOnly
            ? c is (>= 'a' and <= 'z') or (>= 'A' and <= 'Z') or (>= '0' and <= '9') or '-' or '_' or '.' or '~'
            : !IsForbiddenInUri(c);

    // The length of the result: a kept character is one, an escaped code point is three characters per UTF-8 octet.
    private static int Measure(string text, bool unreservedOnly)
    {
        var length = 0;
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (Keeps(c, unreservedOnly))
            {
                length++;
                continue;
            }

            if (char.IsHighSurrogate(c) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
            {
                i++;
                length += 12;
            }
            else
            {
                length += OctetCount(c) * 3;
            }
        }

        return length;
    }

    private static Span<char> Write(string text, bool unreservedOnly, Span<char> destination)
    {
        var length = 0;
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (Keeps(c, unreservedOnly))
            {
                destination[length++] = c;
                continue;
            }

            int codePoint;
            if (char.IsHighSurrogate(c) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
            {
                codePoint = char.ConvertToUtf32(c, text[++i]);
            }
            else
            {
                codePoint = char.IsSurrogate(c) ? 0xFFFD : c;
            }

            length += WriteOctets(codePoint, destination.Slice(length));
        }

        return destination.Slice(0, length);
    }

    private static int OctetCount(char c) =>
        c < 0x80 ? 1
        : c < 0x800 ? 2
        : 3;

    // Writes the UTF-8 octets of a code point as %XX each and returns the number of characters written.
    private static int WriteOctets(int codePoint, Span<char> destination)
    {
        Span<byte> octets = stackalloc byte[4];
        int count;
        if (codePoint < 0x80)
        {
            octets[0] = (byte)codePoint;
            count = 1;
        }
        else if (codePoint < 0x800)
        {
            octets[0] = (byte)(0xC0 | (codePoint >> 6));
            octets[1] = (byte)(0x80 | (codePoint & 0x3F));
            count = 2;
        }
        else if (codePoint < 0x10000)
        {
            octets[0] = (byte)(0xE0 | (codePoint >> 12));
            octets[1] = (byte)(0x80 | ((codePoint >> 6) & 0x3F));
            octets[2] = (byte)(0x80 | (codePoint & 0x3F));
            count = 3;
        }
        else
        {
            octets[0] = (byte)(0xF0 | (codePoint >> 18));
            octets[1] = (byte)(0x80 | ((codePoint >> 12) & 0x3F));
            octets[2] = (byte)(0x80 | ((codePoint >> 6) & 0x3F));
            octets[3] = (byte)(0x80 | (codePoint & 0x3F));
            count = 4;
        }

        for (var i = 0; i < count; i++)
        {
            destination[i * 3] = '%';
            destination[(i * 3) + 1] = HexDigits[octets[i] >> 4];
            destination[(i * 3) + 2] = HexDigits[octets[i] & 0xF];
        }

        return count * 3;
    }
}
