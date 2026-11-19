namespace NetEvolve.Schemas.XPath.Functions.Strings;

using System;
using NetEvolve.Arguments;

/// <summary>
/// Reads and counts the code points of UTF-16 text: the functions of the string group count and index in code points, not in code units.
/// </summary>
/// <remarks>
/// Limitation: a surrogate that is not part of a valid pair counts as one code point of its own, with the value of its code unit
/// (the convention of the codepoint collation, <c>CodepointComparison</c>). A valid <c>xs:string</c> holds none.
/// </remarks>
internal static class CodePoints
{
    /// <summary>Counts the code points of a text.</summary>
    /// <param name="text">The text.</param>
    /// <returns>The number of code points: the code units, less one for every valid surrogate pair.</returns>
    public static int Count(ReadOnlySpan<char> text)
    {
        var count = text.Length;
        for (var i = 1; i < text.Length; i++)
        {
            if (char.IsLowSurrogate(text[i]) && char.IsHighSurrogate(text[i - 1]))
            {
                count--;
            }
        }

        return count;
    }

    /// <summary>Reads the code point at an index and moves the index behind it.</summary>
    /// <param name="text">The text.</param>
    /// <param name="index">The index of the first code unit of the code point; moved by one code unit, or by two for a surrogate pair.</param>
    /// <returns>The code point; the value of the code unit for an unpaired surrogate.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is negative or not before the end of <paramref name="text"/>.</exception>
    public static int Next(ReadOnlySpan<char> text, ref int index)
    {
        Argument.ThrowIfLessThan(index, 0);
        Argument.ThrowIfGreaterThanOrEqual(index, text.Length);

        var unit = text[index++];
        if (char.IsHighSurrogate(unit) && index < text.Length && char.IsLowSurrogate(text[index]))
        {
            return char.ConvertToUtf32(unit, text[index++]);
        }

        return unit;
    }
}
