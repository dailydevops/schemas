namespace NetEvolve.Schemas.XPath.Functions.Strings;

using System;
using NetEvolve.Arguments;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Limits;

/// <summary>
/// The full case mappings of <c>fn:upper-case</c> and <c>fn:lower-case</c> (Functions and Operators 1.0, 7.4.8 and 7.4.9), applied by
/// embedded tables (<see cref="CaseMappingTables"/>): the simple mappings of <c>UnicodeData.txt</c> and the unconditional rows of
/// <c>SpecialCasing.txt</c> of the one pinned Unicode version, so every target framework gives the same result and no culture is consulted.
/// </summary>
/// <remarks>
/// Limitations: the conditional mappings of <c>SpecialCasing.txt</c> are not applied (the Final_Sigma rule of lower-case, and the
/// mappings for Lithuanian, Turkish, and Azeri), so a capital sigma always becomes the small sigma and the case mapping is culture-invariant,
/// as the work package states; the Final_Sigma rule is tracked in #678. A surrogate that is not part of a valid pair maps to itself.
/// </remarks>
internal static class CaseMapping
{
    /// <summary>The most code points that one mapping holds.</summary>
    internal const int MaxExpansion = 3;

    private const int MaxCodePoint = 0x10FFFF;
    private const int InitialBuffer = 256;

    /// <summary>Maps one code point.</summary>
    /// <param name="codePoint">The code point; a value outside U+0000 to U+10FFFF maps to itself.</param>
    /// <param name="upper"><see langword="true"/> for the upper-case mapping, <see langword="false"/> for the lower-case mapping.</param>
    /// <param name="destination">Receives the mapped code points; it holds at least <see cref="MaxExpansion"/> items.</param>
    /// <returns>The number of code points written to <paramref name="destination"/>: one, or two or three for a special mapping.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="destination"/> holds fewer than <see cref="MaxExpansion"/> items.</exception>
    public static int Map(int codePoint, bool upper, Span<int> destination)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(destination.Length, MaxExpansion);

        var keys = upper ? CaseMappingTables.UpperKeys : CaseMappingTables.LowerKeys;
        var index = (uint)codePoint <= MaxCodePoint ? Array.BinarySearch(keys, codePoint) : -1;
        if (index < 0)
        {
            destination[0] = codePoint;
            return 1;
        }

        var value = (upper ? CaseMappingTables.UpperValues : CaseMappingTables.LowerValues)[index];
        if (value >= 0)
        {
            destination[0] = value;
            return 1;
        }

        // A negative value is an expansion: the complement of (offset << 2) | (count - 1).
        var packed = ~value;
        var count = (packed & 3) + 1;
        var offset = packed >> 2;
        var expansions = upper ? CaseMappingTables.UpperExpansions : CaseMappingTables.LowerExpansions;
        for (var i = 0; i < count; i++)
        {
            destination[i] = expansions[offset + i];
        }

        return count;
    }

    /// <summary>Maps every code point of a string to upper case.</summary>
    /// <param name="text">The string.</param>
    /// <param name="limits">The evaluation limits; the mapped string may be longer than the string (for example one sharp s gives two S).</param>
    /// <returns>The mapped string; <paramref name="text"/> itself when no code point changes.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="text"/> or <paramref name="limits"/> is <see langword="null"/>.</exception>
    /// <exception cref="XPathException">The mapped string is longer than the limit (XP0006).</exception>
    public static string ToUpper(string text, XPathLimits limits) => Apply(text, limits, upper: true);

    /// <summary>Maps every code point of a string to lower case.</summary>
    /// <param name="text">The string.</param>
    /// <param name="limits">The evaluation limits; the mapped string may be longer than the string (for example a capital I with a dot gives two code points).</param>
    /// <returns>The mapped string; <paramref name="text"/> itself when no code point changes.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="text"/> or <paramref name="limits"/> is <see langword="null"/>.</exception>
    /// <exception cref="XPathException">The mapped string is longer than the limit (XP0006).</exception>
    public static string ToLower(string text, XPathLimits limits) => Apply(text, limits, upper: false);

    private static string Apply(string text, XPathLimits limits, bool upper)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(limits);

        var first = IndexOfChange(text, upper);
        if (first < 0)
        {
            return text;
        }

        Span<char> initial = stackalloc char[InitialBuffer];
        var builder = new TextBuilder(initial, limits);
        try
        {
            builder.Append(text.AsSpan(0, first));
            Span<int> mapped = stackalloc int[MaxExpansion];
            var span = text.AsSpan();
            var index = first;
            while (index < span.Length)
            {
                var count = Map(CodePoints.Next(span, ref index), upper, mapped);
                for (var i = 0; i < count; i++)
                {
                    builder.AppendCodePoint(mapped[i]);
                }
            }

            return builder.ToString();
        }
        finally
        {
            builder.Dispose();
        }
    }

    // The index of the first code unit whose code point maps to something else, or -1 when nothing changes.
    private static int IndexOfChange(string text, bool upper)
    {
        var span = text.AsSpan();
        Span<int> mapped = stackalloc int[MaxExpansion];
        var index = 0;
        while (index < span.Length)
        {
            var start = index;
            var unit = span[index];
            if (unit < 0x80)
            {
                // ASCII needs no table lookup: only the letters of the other case change.
                index++;
                if (upper ? unit is >= 'a' and <= 'z' : unit is >= 'A' and <= 'Z')
                {
                    return start;
                }

                continue;
            }

            var codePoint = CodePoints.Next(span, ref index);
            if (Map(codePoint, upper, mapped) != 1 || mapped[0] != codePoint)
            {
                return start;
            }
        }

        return -1;
    }
}
