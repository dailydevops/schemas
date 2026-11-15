namespace NetEvolve.Schemas.Xml.Text.CharClasses;

using System;
using System.Runtime.CompilerServices;

/// <summary>
/// Single implementation of the XML 1.0 (Fifth Edition) character classes <c>Char</c> (production 2),
/// <c>NameStartChar</c> (production 4), and <c>NameChar</c> (production 4a).
/// </summary>
/// <remarks>
/// Limitation: XML 1.1 character classes and restricted characters are not covered.
/// The <see cref="char"/> overloads classify one UTF-16 code unit, so a surrogate is never a match on its own;
/// supplementary code points are reached through the <see cref="int"/> overloads or the span helpers.
/// </remarks>
internal static class XmlCharClass
{
    /// <summary>Production 2 for a UTF-16 code unit. Surrogates are rejected.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool IsChar(char c) =>
        c >= 0x20 ? c <= 0xD7FF || (c >= 0xE000 && c <= 0xFFFD) : c is '\t' or '\n' or '\r';

    /// <summary>Production 2 for a full code point.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool IsChar(int codePoint) =>
        codePoint <= 0xFFFF ? codePoint >= 0 && IsChar((char)codePoint) : codePoint <= 0x10FFFF;

    /// <summary>Production 4 for a UTF-16 code unit. Surrogates are rejected.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool IsNameStartChar(char c) =>
        c < 0x80 ? (uint)((c | 0x20) - 'a') <= 'z' - 'a' || c is ':' or '_' : IsNonAsciiNameStart(c);

    /// <summary>Production 4 for a full code point.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool IsNameStartChar(int codePoint) =>
        codePoint <= 0xFFFF ? codePoint >= 0 && IsNameStartChar((char)codePoint) : codePoint <= 0xEFFFF;

    /// <summary>Production 4a for a UTF-16 code unit. Surrogates are rejected.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool IsNameChar(char c) =>
        c < 0x80
            ? (uint)((c | 0x20) - 'a') <= 'z' - 'a' || (uint)(c - '0') <= 9 || c is ':' or '_' or '-' or '.'
            : IsNonAsciiNameStart(c) || c == 0xB7 || (c >= 0x300 && c <= 0x36F) || (c >= 0x203F && c <= 0x2040);

    /// <summary>Production 4a for a full code point.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool IsNameChar(int codePoint) =>
        codePoint <= 0xFFFF ? codePoint >= 0 && IsNameChar((char)codePoint) : codePoint <= 0xEFFFF;

    /// <summary>Returns the index of the first character that is not a <c>Char</c>, or -1 when all are.</summary>
    /// <remarks>A valid surrogate pair counts as one supplementary <c>Char</c>; a lone surrogate does not.</remarks>
    internal static int IndexOfNonChar(ReadOnlySpan<char> text)
    {
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (IsChar(c))
            {
                continue;
            }

            if (char.IsHighSurrogate(c) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
            {
                i++;
                continue;
            }

            return i;
        }

        return -1;
    }

    /// <summary>Returns the length in UTF-16 code units of the leading <c>Name</c> (production 5), or 0 if none.</summary>
    internal static int NameLength(ReadOnlySpan<char> text)
    {
        var i = 0;
        while (i < text.Length)
        {
            var c = text[i];
            int width;
            bool ok;
            if (char.IsHighSurrogate(c))
            {
                // Valid pairs map to U+10000..U+10FFFF; only up to U+EFFFF is a name character.
                if (i + 1 >= text.Length || !char.IsLowSurrogate(text[i + 1]))
                {
                    break;
                }

                width = 2;
                ok = c <= 0xDB7F;
            }
            else
            {
                width = 1;
                ok = i == 0 ? IsNameStartChar(c) : IsNameChar(c);
            }

            if (!ok)
            {
                break;
            }

            i += width;
        }

        return i;
    }

    private static bool IsNonAsciiNameStart(char c) =>
        (c >= 0xC0 && c <= 0xD6)
        || (c >= 0xD8 && c <= 0xF6)
        || (c >= 0xF8 && c <= 0x2FF)
        || (c >= 0x370 && c <= 0x37D)
        || (c >= 0x37F && c <= 0x1FFF)
        || c is '‌' or '‍'
        || (c >= 0x2070 && c <= 0x218F)
        || (c >= 0x2C00 && c <= 0x2FEF)
        || (c >= 0x3001 && c <= 0xD7FF)
        || (c >= 0xF900 && c <= 0xFDCF)
        || (c >= 0xFDF0 && c <= 0xFFFD);
}
