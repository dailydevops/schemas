namespace NetEvolve.Schemas.XPath.Formatting.Numbers;

using NetEvolve.Arguments;

/// <summary>
/// One analyzed sub-picture: the variables of the analysis phase (XSLT 2.0, 16.4.3; Functions and Operators 3.1, 4.7.4). Immutable after
/// <see cref="NumberPictureParser"/> has built it.
/// </summary>
internal sealed class SubPicture
{
    /// <summary>Initializes a new instance of the <see cref="SubPicture"/> class.</summary>
    /// <param name="prefix">The passive characters to the left of the leftmost active character.</param>
    /// <param name="suffix">The passive characters to the right of the rightmost active character.</param>
    /// <param name="multiplier">1, 100 for a percent sign, or 1000 for a per-mille sign.</param>
    /// <param name="minimumIntegerSize">The minimum number of integer digits.</param>
    /// <param name="minimumFractionSize">The minimum number of fraction digits.</param>
    /// <param name="maximumFractionSize">The maximum number of fraction digits.</param>
    /// <param name="scalingFactor">The number of mandatory digits of the integer part, which scales the mantissa of an exponent.</param>
    /// <param name="minimumExponentSize">The minimum number of exponent digits, zero for a picture without an exponent separator.</param>
    /// <param name="groupingSize">The size of a regular integer grouping, zero when the positions are explicit or there is no grouping.</param>
    /// <param name="integerGroupingPositions">The explicit integer grouping positions: the digits to the right of the separator.</param>
    /// <param name="fractionGroupingPositions">The fraction grouping positions: the digits to the left of the separator.</param>
    /// <exception cref="System.ArgumentNullException">A string or an array is <see langword="null"/>.</exception>
    public SubPicture(
        string prefix,
        string suffix,
        int multiplier,
        int minimumIntegerSize,
        int minimumFractionSize,
        int maximumFractionSize,
        int scalingFactor,
        int minimumExponentSize,
        int groupingSize,
        int[] integerGroupingPositions,
        int[] fractionGroupingPositions
    )
    {
        ArgumentNullException.ThrowIfNull(prefix);
        ArgumentNullException.ThrowIfNull(suffix);
        ArgumentNullException.ThrowIfNull(integerGroupingPositions);
        ArgumentNullException.ThrowIfNull(fractionGroupingPositions);

        Prefix = prefix;
        Suffix = suffix;
        Multiplier = multiplier;
        MinimumIntegerSize = minimumIntegerSize;
        MinimumFractionSize = minimumFractionSize;
        MaximumFractionSize = maximumFractionSize;
        ScalingFactor = scalingFactor;
        MinimumExponentSize = minimumExponentSize;
        GroupingSize = groupingSize;
        IntegerGroupingPositions = integerGroupingPositions;
        FractionGroupingPositions = fractionGroupingPositions;
    }

    /// <summary>Gets the prefix.</summary>
    public string Prefix { get; }

    /// <summary>Gets the suffix.</summary>
    public string Suffix { get; }

    /// <summary>Gets the factor of a percent sign (100), a per-mille sign (1000), or neither (1).</summary>
    public int Multiplier { get; }

    /// <summary>Gets the minimum number of integer digits.</summary>
    public int MinimumIntegerSize { get; }

    /// <summary>Gets the minimum number of fraction digits.</summary>
    public int MinimumFractionSize { get; }

    /// <summary>Gets the maximum number of fraction digits.</summary>
    public int MaximumFractionSize { get; }

    /// <summary>Gets the scaling factor of an exponent.</summary>
    public int ScalingFactor { get; }

    /// <summary>Gets the minimum number of exponent digits; zero when the picture has no exponent separator.</summary>
    public int MinimumExponentSize { get; }

    /// <summary>Gets the size of a regular integer grouping, which extends to every multiple; zero when there is none.</summary>
    public int GroupingSize { get; }

    /// <summary>Gets the explicit integer grouping positions, empty for a regular grouping or none.</summary>
    public int[] IntegerGroupingPositions { get; }

    /// <summary>Gets the fraction grouping positions.</summary>
    public int[] FractionGroupingPositions { get; }

    /// <summary>Gets a value indicating whether the picture has an exponent separator.</summary>
    public bool HasExponent => MinimumExponentSize > 0;

    /// <summary>Counts the grouping separators that follow an integer digit.</summary>
    /// <param name="digitsToRight">The number of integer digits to the right of the digit, at least one.</param>
    /// <returns>The number of separators to insert after the digit.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="digitsToRight"/> is below one.</exception>
    public int GroupingSeparatorsAfter(int digitsToRight)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(digitsToRight, 1);

        if (GroupingSize > 0)
        {
            return digitsToRight % GroupingSize == 0 ? 1 : 0;
        }

        return Count(IntegerGroupingPositions, digitsToRight);
    }

    /// <summary>Counts the grouping separators that precede a fraction digit.</summary>
    /// <param name="digitsToLeft">The number of fraction digits to the left of the digit, at least one.</param>
    /// <returns>The number of separators to insert before the digit.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="digitsToLeft"/> is below one.</exception>
    public int GroupingSeparatorsBefore(int digitsToLeft)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(digitsToLeft, 1);

        return Count(FractionGroupingPositions, digitsToLeft);
    }

    /// <summary>Creates the sub-picture of negative values that a picture with one sub-picture implies: the minus sign goes before the prefix.</summary>
    /// <param name="minusSign">The minus sign of the decimal format.</param>
    /// <returns>A copy with the changed prefix.</returns>
    public SubPicture WithMinusSign(char minusSign) =>
        new(
            minusSign + Prefix,
            Suffix,
            Multiplier,
            MinimumIntegerSize,
            MinimumFractionSize,
            MaximumFractionSize,
            ScalingFactor,
            MinimumExponentSize,
            GroupingSize,
            IntegerGroupingPositions,
            FractionGroupingPositions
        );

    private static int Count(int[] positions, int position)
    {
        var count = 0;
        for (var i = 0; i < positions.Length; i++)
        {
            if (positions[i] == position)
            {
                count++;
            }
        }

        return count;
    }
}
