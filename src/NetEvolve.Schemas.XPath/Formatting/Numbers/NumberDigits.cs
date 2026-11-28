namespace NetEvolve.Schemas.XPath.Formatting.Numbers;

using System;

/// <summary>
/// The decimal digits of the absolute value of a number: <c>0.DIGITS x 10^Point</c> with no leading and no trailing zero, so zero is the
/// empty digit string. The value is exact for <c>xs:decimal</c> and <c>xs:integer</c> and the shortest decimal that round-trips for
/// <c>xs:float</c> and <c>xs:double</c> (Functions and Operators 3.1, 4.7.5).
/// </summary>
internal readonly struct NumberDigits
{
    /// <summary>Initializes a new instance of the <see cref="NumberDigits"/> struct.</summary>
    /// <param name="digits">The digits without leading and trailing zeros; empty for zero.</param>
    /// <param name="point">The number of digits to the left of the decimal point; negative when zeros follow the point.</param>
    /// <exception cref="ArgumentNullException"><paramref name="digits"/> is <see langword="null"/>.</exception>
    public NumberDigits(string digits, int point)
    {
        ArgumentNullException.ThrowIfNull(digits);

        Digits = digits;
        Point = digits.Length == 0 ? 0 : point;
    }

    /// <summary>Gets the digits.</summary>
    public string Digits { get; }

    /// <summary>Gets the position of the decimal point.</summary>
    public int Point { get; }

    /// <summary>Rounds to a number of fraction digits, a tie to the even neighbor: <c>fn:round-half-to-even</c> of the converted number.</summary>
    /// <param name="maximumFraction">The maximum number of fraction digits.</param>
    /// <returns>The rounded digits; a number that rounds up to the next power of ten is the digit <c>1</c> with the point one place further.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maximumFraction"/> is negative.</exception>
    public NumberDigits Round(int maximumFraction)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maximumFraction, 0);

        var keep = (long)Point + maximumFraction;
        if (keep >= Digits.Length)
        {
            return this;
        }

        // Below one half of the last kept place, whatever the digits: the number is zero.
        if (keep < 0)
        {
            return new NumberDigits(string.Empty, 0);
        }

        var kept = (int)keep;
        var roundUp = RoundsUp(kept);
        if (!roundUp)
        {
            return new NumberDigits(TrimEnd(Digits.Substring(0, kept)), Point);
        }

        // Increment the kept digits: a run of nines carries into the next place.
        var digits = Digits.ToCharArray(0, kept);
        var index = kept - 1;
        while (index >= 0 && digits[index] == '9')
        {
            digits[index] = '0';
            index--;
        }

        if (index < 0)
        {
            return new NumberDigits("1", Point + 1);
        }

        digits[index]++;
        return new NumberDigits(TrimEnd(new string(digits)), Point);
    }

    // The digit after the kept ones decides: above five up, below five down, five with more digits behind it up, and a tie goes to the even kept digit.
    private bool RoundsUp(int kept)
    {
        var next = Digits[kept];
        if (next != '5')
        {
            return next > '5';
        }

        // The digit string has no trailing zero, so a longer string has a nonzero digit behind the five.
        if (kept + 1 < Digits.Length)
        {
            return true;
        }

        return kept > 0 && (Digits[kept - 1] - '0') % 2 == 1;
    }

    private static string TrimEnd(string digits)
    {
        var length = digits.Length;
        while (length > 0 && digits[length - 1] == '0')
        {
            length--;
        }

        return length == digits.Length ? digits : digits.Substring(0, length);
    }
}
