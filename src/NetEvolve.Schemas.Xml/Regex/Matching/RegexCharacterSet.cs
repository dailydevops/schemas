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

    // The membership of the code points 0 to 127, bit n of the low word for n below 64, of the high word above.
    private readonly ulong _asciiLow;
    private readonly ulong _asciiHigh;

    /// <summary>Initializes a new instance of the <see cref="RegexCharacterSet"/> class.</summary>
    /// <param name="ranges">The range pairs <c>start0, end0, start1, end1, ...</c>, copied.</param>
    public RegexCharacterSet(ReadOnlySpan<int> ranges)
    {
        _ranges = ranges.ToArray();
        for (var codePoint = 0; codePoint < 128; codePoint++)
        {
            if (!Search(codePoint))
            {
                continue;
            }

            if (codePoint < 64)
            {
                _asciiLow |= 1UL << codePoint;
            }
            else
            {
                _asciiHigh |= 1UL << (codePoint - 64);
            }
        }
    }

    /// <summary>Determines whether a code point is in the set.</summary>
    /// <param name="codePoint">The code point.</param>
    /// <returns><see langword="true"/> when a range contains <paramref name="codePoint"/>.</returns>
    public bool Contains(int codePoint)
    {
        if ((uint)codePoint < 128)
        {
            return codePoint < 64 ? ((_asciiLow >> codePoint) & 1) != 0 : ((_asciiHigh >> (codePoint - 64)) & 1) != 0;
        }

        return Search(codePoint);
    }

    private bool Search(int codePoint)
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
