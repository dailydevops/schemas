namespace NetEvolve.Schemas.XPath.Functions.Strings;

using System;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Limits;

/// <summary>
/// The full case mappings of <c>fn:upper-case</c> and <c>fn:lower-case</c> (Functions and Operators 1.0, 7.4.8 and 7.4.9), applied by
/// embedded tables (<see cref="CaseMappingTables"/>): the simple mappings of <c>UnicodeData.txt</c> and the unconditional rows of
/// <c>SpecialCasing.txt</c> of the one pinned Unicode version, so every target framework gives the same result and no culture is consulted.
/// </summary>
/// <remarks>
/// The Final_Sigma rule of lower-case is applied (a capital sigma at the end of a word becomes the final small sigma U+03C2), with the
/// Cased and Case_Ignorable ranges of <c>DerivedCoreProperties.txt</c>.
/// Limitations: the other conditional mappings of <c>SpecialCasing.txt</c> (Lithuanian, Turkish, and Azeri) are not applied, so the case
/// mapping is culture-invariant, as the work package states. A surrogate that is not part of a valid pair maps to itself.
/// </remarks>
internal static class CaseMapping
{
    /// <summary>The most code points that one mapping holds.</summary>
    internal const int MaxExpansion = 3;

    private const int MaxCodePoint = 0x10FFFF;
    private const int CapitalSigma = 0x3A3;
    private const int FinalSmallSigma = 0x3C2;
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
                var unit = span[index];
                if (unit < 0x80)
                {
                    // ASCII needs no table lookup: the letters of the other case shift by 32, everything else stays.
                    index++;
                    builder.Append(
                        upper ? (unit is >= 'a' and <= 'z' ? (char)(unit - 32) : unit)
                        : unit is >= 'A' and <= 'Z' ? (char)(unit + 32)
                        : unit
                    );
                    continue;
                }

                var start = index;
                var codePoint = CodePoints.Next(span, ref index);
                if (!upper && codePoint == CapitalSigma && IsFinalSigma(span, start, index))
                {
                    builder.AppendCodePoint(FinalSmallSigma);
                    continue;
                }

                var count = Map(codePoint, upper, mapped);
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

    // Final_Sigma (SpecialCasing.txt): the sigma is preceded by a cased letter and case-ignorable characters, and is not followed by
    // case-ignorable characters and a cased letter. A character that is both cased and case ignorable counts as cased.
    private static bool IsFinalSigma(ReadOnlySpan<char> text, int start, int end)
    {
        var before = false;
        for (var i = start; i > 0; )
        {
            var codePoint = Previous(text, ref i);
            if (InRanges(codePoint, CaseMappingTables.CasedFirst, CaseMappingTables.CasedLast))
            {
                before = true;
                break;
            }

            if (!InRanges(codePoint, CaseMappingTables.CaseIgnorableFirst, CaseMappingTables.CaseIgnorableLast))
            {
                break;
            }
        }

        if (!before)
        {
            return false;
        }

        for (var i = end; i < text.Length; )
        {
            var codePoint = CodePoints.Next(text, ref i);
            if (InRanges(codePoint, CaseMappingTables.CasedFirst, CaseMappingTables.CasedLast))
            {
                return false;
            }

            if (!InRanges(codePoint, CaseMappingTables.CaseIgnorableFirst, CaseMappingTables.CaseIgnorableLast))
            {
                break;
            }
        }

        return true;
    }

    // Reads the code point that ends at the index and moves the index to its start.
    private static int Previous(ReadOnlySpan<char> text, ref int index)
    {
        var unit = text[--index];
        if (char.IsLowSurrogate(unit) && index > 0 && char.IsHighSurrogate(text[index - 1]))
        {
            return char.ConvertToUtf32(text[--index], unit);
        }

        return unit;
    }

    // Whether the code point lies in one of the sorted ranges.
    private static bool InRanges(int codePoint, int[] first, int[] last)
    {
        var index = Array.BinarySearch(first, codePoint);
        if (index >= 0)
        {
            return true;
        }

        index = ~index - 1;
        return index >= 0 && codePoint <= last[index];
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
