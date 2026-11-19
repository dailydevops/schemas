namespace NetEvolve.Schemas.Xml.Tests.Unit.Datatypes.Facets.Ordered;

using System;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Decimals;
using NetEvolve.Schemas.Xml.Datatypes.Facets.Ordered;
using NetEvolve.Schemas.Xml.Datatypes.Numeric;

public class DigitsFacetTests
{
    private static ValidationResult Decimal(IFacet facet, string lexical) =>
        OrderedFacetSupport.Restrict<XsdDecimal>("decimal", XsdVersion.V11, facet).Validate(lexical.AsSpan());

    private static ValidationResult Integer(IFacet facet, string lexical, string type = "integer") =>
        OrderedFacetSupport.Restrict<XsdInteger>(type, XsdVersion.V11, facet).Validate(lexical.AsSpan());

    [Test]
    [Arguments(5, "123.45", true)]
    [Arguments(5, "-123.45", true)]
    [Arguments(5, "+123.45", true)]
    [Arguments(5, "123.456", false)]
    [Arguments(5, "0123.4500", true)]
    [Arguments(5, "00000123.45", true)]
    [Arguments(5, "12345", true)]
    [Arguments(5, "123456", false)]
    [Arguments(5, "100000", false)]
    [Arguments(5, "00012345.00000", true)]
    [Arguments(5, "0.00001", true)]
    [Arguments(5, "000.00001000", true)]
    [Arguments(5, "0.000001", false)]
    [Arguments(2, "1.50", true)]
    [Arguments(2, "1.500", true)]
    [Arguments(2, "1.505", false)]
    [Arguments(1, "0", true)]
    [Arguments(1, "0.0", true)]
    [Arguments(1, "-0.000", true)]
    [Arguments(1, "00", true)]
    [Arguments(1, "10", false)]
    public async Task TotalDigits_OnDecimal_IgnoresLeadingAndTrailingZeros(int digits, string lexical, bool valid)
    {
        var result = Decimal(new TotalDigitsFacet(digits), lexical);

        await Assert.That(result.IsValid).IsEqualTo(valid).Because($"totalDigits {digits}, value {lexical}");
        if (!valid)
        {
            await Assert.That(result.FailedFacet).IsEqualTo(FacetKind.TotalDigits);
        }
    }

    [Test]
    [Arguments(2, "1.5", true)]
    [Arguments(2, "1.50", true)]
    [Arguments(2, "1.500", true)]
    [Arguments(2, "1.0000", true)]
    [Arguments(2, "1.505", false)]
    [Arguments(2, "0.01", true)]
    [Arguments(2, "0.001", false)]
    [Arguments(2, "0.0010", false)]
    [Arguments(2, "100", true)]
    [Arguments(2, "100.00", true)]
    [Arguments(2, "-0.10", true)]
    [Arguments(0, "5", true)]
    [Arguments(0, "5.0", true)]
    [Arguments(0, "5.1", false)]
    [Arguments(0, "0.00", true)]
    public async Task FractionDigits_OnDecimal_IgnoresTrailingZeros(int digits, string lexical, bool valid)
    {
        var result = Decimal(new FractionDigitsFacet(digits), lexical);

        await Assert.That(result.IsValid).IsEqualTo(valid).Because($"fractionDigits {digits}, value {lexical}");
        if (!valid)
        {
            await Assert.That(result.FailedFacet).IsEqualTo(FacetKind.FractionDigits);
        }
    }

    [Test]
    [Arguments(3, "123", true)]
    [Arguments(3, "-123", true)]
    [Arguments(3, "000123", true)]
    [Arguments(3, "+0123", true)]
    [Arguments(3, "1234", false)]
    [Arguments(3, "100", true)]
    [Arguments(3, "1000", false)]
    [Arguments(1, "0", true)]
    [Arguments(1, "-0", true)]
    [Arguments(1, "+000", true)]
    [Arguments(1, "10", false)]
    [Arguments(30, "123456789012345678901234567890", true)]
    [Arguments(30, "1234567890123456789012345678901", false)]
    public async Task TotalDigits_OnInteger_IgnoresLeadingZeros(int digits, string lexical, bool valid)
    {
        var result = Integer(new TotalDigitsFacet(digits), lexical);

        await Assert.That(result.IsValid).IsEqualTo(valid).Because($"totalDigits {digits}, value {lexical}");
        if (!valid)
        {
            await Assert.That(result.FailedFacet).IsEqualTo(FacetKind.TotalDigits);
        }
    }

    [Test]
    [Arguments("integer", "0")]
    [Arguments("integer", "-123456789012345678901234567890")]
    [Arguments("byte", "100")]
    [Arguments("unsignedLong", "18446744073709551615")]
    public async Task FractionDigits_OnInteger_AcceptsEveryValueBecauseAnIntegerHasNoFraction(
        string type,
        string lexical
    )
    {
        var result = Integer(new FractionDigitsFacet(0), lexical, type);

        await Assert.That(result.IsValid).IsTrue();
    }

    [Test]
    [Arguments("byte", 3, "127", true)]
    [Arguments("byte", 2, "127", false)]
    [Arguments("unsignedLong", 20, "18446744073709551615", true)]
    [Arguments("unsignedLong", 19, "18446744073709551615", false)]
    public async Task TotalDigits_OnBoundedIntegerTypes_RestrictsFurther(
        string type,
        int digits,
        string lexical,
        bool valid
    )
    {
        var result = Integer(new TotalDigitsFacet(digits), lexical, type);

        await Assert.That(result.IsValid).IsEqualTo(valid);
    }

    [Test]
    public async Task Facets_ExposeKindAndValue()
    {
        var total = new TotalDigitsFacet(7);
        var fraction = new FractionDigitsFacet(3);

        await Assert.That(total.Kind).IsEqualTo(FacetKind.TotalDigits);
        await Assert.That(total.Value).IsEqualTo(7);
        await Assert.That(fraction.Kind).IsEqualTo(FacetKind.FractionDigits);
        await Assert.That(fraction.Value).IsEqualTo(3);
    }

    [Test]
    [Arguments(0)]
    [Arguments(-1)]
    public async Task TotalDigits_MustBePositive(int value) =>
        await Assert.That(() => new TotalDigitsFacet(value)).Throws<ArgumentOutOfRangeException>();

    [Test]
    public async Task FractionDigits_MustNotBeNegative()
    {
        await Assert.That(() => new FractionDigitsFacet(-1)).Throws<ArgumentOutOfRangeException>();
        await Assert.That(new FractionDigitsFacet(0).Value).IsEqualTo(0);
    }
}
