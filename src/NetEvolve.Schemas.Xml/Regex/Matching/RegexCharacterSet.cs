namespace NetEvolve.Schemas.Xml.Regex.Matching;

using System;

/// <summary>
/// An immutable set of code points held as sorted, non-overlapping, inclusive range pairs. The parser resolves a character
/// class, with its categories, blocks, negation, and subtraction, to one set; the matcher only asks whether a code point is in it.
/// </summary>
/// <remarks>The ranges are a precondition of the caller and are not validated: an even count of values, each range with its start not above its end, ascending and without overlap.</remarks>
public sealed class RegexCharacterSet
{
    private readonly int[] _ranges;

    /// <summary>Initializes a new instance of the <see cref="RegexCharacterSet"/> class.</summary>
    /// <param name="ranges">The range pairs <c>start0, end0, start1, end1, ...</c>, copied.</param>
    public RegexCharacterSet(ReadOnlySpan<int> ranges) => _ranges = ranges.ToArray();

    /// <summary>Determines whether a code point is in the set.</summary>
    /// <param name="codePoint">The code point.</param>
    /// <returns><see langword="true"/> when a range contains <paramref name="codePoint"/>.</returns>
    public bool Contains(int codePoint)
    {
        var low = 0;
        var high = (_ranges.Length / 2) - 1;
        while (low <= high)
        {
            var mid = low + ((high - low) / 2);
            if (codePoint < _ranges[2 * mid])
            {
                high = mid - 1;
            }
            else if (codePoint > _ranges[(2 * mid) + 1])
            {
                low = mid + 1;
            }
            else
            {
                return true;
            }
        }

        return false;
    }
}
