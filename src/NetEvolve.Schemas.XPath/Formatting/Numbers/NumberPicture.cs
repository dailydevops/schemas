namespace NetEvolve.Schemas.XPath.Formatting.Numbers;

using NetEvolve.Arguments;

/// <summary>
/// An analyzed picture string: the sub-picture of positive values and the one of negative values. A picture with one sub-picture uses its
/// analysis for both, with the minus sign of the decimal format before the prefix for negative values.
/// </summary>
internal sealed class NumberPicture
{
    /// <summary>Initializes a new instance of the <see cref="NumberPicture"/> class.</summary>
    /// <param name="positive">The sub-picture of positive values and unsigned zero.</param>
    /// <param name="negative">The sub-picture of negative values.</param>
    /// <exception cref="System.ArgumentNullException">An argument is <see langword="null"/>.</exception>
    public NumberPicture(SubPicture positive, SubPicture negative)
    {
        ArgumentNullException.ThrowIfNull(positive);
        ArgumentNullException.ThrowIfNull(negative);

        Positive = positive;
        Negative = negative;
    }

    /// <summary>Gets the sub-picture of positive values and unsigned zero.</summary>
    public SubPicture Positive { get; }

    /// <summary>Gets the sub-picture of negative values.</summary>
    public SubPicture Negative { get; }
}
