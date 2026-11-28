namespace NetEvolve.Schemas.Xml.Datatypes.Facets.Ordered;

using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Decimals;
using NetEvolve.Schemas.Xml.Datatypes.Numeric;

/// <summary>
/// The <c>fractionDigits</c> facet (XSD 1.1 Part 2, section 4.3.12): a decimal-derived value has at most the given number of
/// fraction digits. The trailing zeros of the fraction do not count.
/// </summary>
/// <remarks>The count is <see cref="XsdDecimal.FractionDigits"/>. An <see cref="XsdInteger"/> has no fraction, so every integer satisfies the facet.</remarks>
public sealed class FractionDigitsFacet : IValueFacet<XsdDecimal>, IValueFacet<XsdInteger>
{
    /// <summary>Initializes a new instance of the <see cref="FractionDigitsFacet"/> class.</summary>
    /// <param name="value">The maximum number of fraction digits, a non-negative integer.</param>
    /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="value"/> is negative.</exception>
    public FractionDigitsFacet(int value)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(value, 0);
        Value = value;
    }

    /// <inheritdoc />
    public FacetKind Kind => FacetKind.FractionDigits;

    /// <summary>Gets the maximum number of fraction digits.</summary>
    public int Value { get; }

    /// <inheritdoc />
    public bool IsSatisfiedBy(in XsdDecimal value) => value.FractionDigits <= Value;

    /// <inheritdoc />
    public bool IsSatisfiedBy(in XsdInteger value) => true;
}
