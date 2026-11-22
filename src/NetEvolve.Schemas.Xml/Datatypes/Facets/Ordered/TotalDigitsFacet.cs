namespace NetEvolve.Schemas.Xml.Datatypes.Facets.Ordered;

using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Decimals;
using NetEvolve.Schemas.Xml.Datatypes.Numeric;

/// <summary>
/// The <c>totalDigits</c> facet (XSD 1.1 Part 2, section 4.3.11): a decimal-derived value has at most the given number of digits.
/// Leading zeros and the trailing zeros of the fraction do not count, and zero counts as one digit.
/// </summary>
/// <remarks>
/// The count is <see cref="XsdDecimal.TotalDigits"/>. An <see cref="XsdInteger"/> is counted as a decimal without a fraction, so the
/// zeros at the end of an integer count (<c>100</c> has three digits). The facet applies to the decimal value type and to the integer
/// family, which derives from decimal.
/// </remarks>
public sealed class TotalDigitsFacet : IValueFacet<XsdDecimal>, IValueFacet<XsdInteger>
{
    /// <summary>Initializes a new instance of the <see cref="TotalDigitsFacet"/> class.</summary>
    /// <param name="value">The maximum number of digits, a positive integer.</param>
    /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="value"/> is less than one.</exception>
    public TotalDigitsFacet(int value)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(value, 1);
        Value = value;
    }

    /// <inheritdoc />
    public FacetKind Kind => FacetKind.TotalDigits;

    /// <summary>Gets the maximum number of digits.</summary>
    public int Value { get; }

    /// <inheritdoc />
    public bool IsSatisfiedBy(in XsdDecimal value) => value.TotalDigits <= Value;

    /// <inheritdoc />
    public bool IsSatisfiedBy(in XsdInteger value) => new XsdDecimal(value.Value, 0).TotalDigits <= Value;
}
