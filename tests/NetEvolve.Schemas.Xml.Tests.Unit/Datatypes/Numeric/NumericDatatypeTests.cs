namespace NetEvolve.Schemas.Xml.Tests.Unit.Datatypes.Numeric;

using System.Globalization;
using System.Numerics;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Core;
using NetEvolve.Schemas.Xml.Datatypes.Decimals;
using NetEvolve.Schemas.Xml.Datatypes.Numeric;

public class NumericDatatypeTests
{
    private static IDatatypeDescriptor Get(string name, XsdVersion version = XsdVersion.V11) =>
        BuiltInDatatypes.For(version).Get(name);

    private static string Shift(string value, int delta) =>
        (BigInteger.Parse(value, CultureInfo.InvariantCulture) + delta).ToString(CultureInfo.InvariantCulture);

    private static IDatatypeDescriptor<XsdInteger> Integer(string name, XsdVersion version = XsdVersion.V11) =>
        (IDatatypeDescriptor<XsdInteger>)Get(name, version);

    [Test]
    [Arguments(XsdVersion.V10)]
    [Arguments(XsdVersion.V11)]
    public async Task Registry_ContainsDecimalAndTheIntegerFamily_InBothVersions(XsdVersion version)
    {
        string[] names =
        [
            "decimal",
            "integer",
            "nonPositiveInteger",
            "negativeInteger",
            "long",
            "int",
            "short",
            "byte",
            "nonNegativeInteger",
            "unsignedLong",
            "unsignedInt",
            "unsignedShort",
            "unsignedByte",
            "positiveInteger",
        ];

        var registry = BuiltInDatatypes.For(version);
        foreach (var name in names)
        {
            await Assert.That(registry.Get(name).Name).IsEqualTo(name);
            await Assert.That(registry.Get(name).LexicalMode).IsEqualTo(version);
        }
    }

    [Test]
    [Arguments("nonPositiveInteger", null, "0")]
    [Arguments("negativeInteger", null, "-1")]
    [Arguments("long", "-9223372036854775808", "9223372036854775807")]
    [Arguments("int", "-2147483648", "2147483647")]
    [Arguments("short", "-32768", "32767")]
    [Arguments("byte", "-128", "127")]
    [Arguments("nonNegativeInteger", "0", null)]
    [Arguments("unsignedLong", "0", "18446744073709551615")]
    [Arguments("unsignedInt", "0", "4294967295")]
    [Arguments("unsignedShort", "0", "65535")]
    [Arguments("unsignedByte", "0", "255")]
    [Arguments("positiveInteger", "1", null)]
    public async Task Range_AcceptsTheBoundsAndRejectsOneBelowAndOneAbove(string name, string? min, string? max)
    {
        var type = Get(name);

        if (min is not null)
        {
            await Assert.That(type.Validate(min).IsValid).IsTrue();
            var below = type.Validate(Shift(min, -1));
            await Assert.That(below.IsValid).IsFalse();
            await Assert.That(below.FailedFacet).IsEqualTo(FacetKind.MinInclusive);
        }

        if (max is not null)
        {
            await Assert.That(type.Validate(max).IsValid).IsTrue();
            var above = type.Validate(Shift(max, 1));
            await Assert.That(above.IsValid).IsFalse();
            await Assert.That(above.FailedFacet).IsEqualTo(FacetKind.MaxInclusive);
        }
    }

    [Test]
    public async Task Integer_AcceptsValuesFarBeyond64Bits()
    {
        var digits = new string('9', 200);

        var result = Integer("integer").Parse("-" + digits);

        await Assert.That(result.Success).IsTrue();
        await Assert
            .That(result.Value)
            .IsEqualTo(new XsdInteger(-BigInteger.Parse(digits, CultureInfo.InvariantCulture)));
    }

    [Test]
    [Arguments("1.0", 1)]
    [Arguments("1.", 1)]
    [Arguments(".5", 0)]
    [Arguments("1e3", 1)]
    [Arguments("0x10", 1)]
    [Arguments("1 2", 1)]
    public async Task Integer_RejectsADecimalPointAndOtherCharacters(string lexical, int offset)
    {
        var result = Integer("integer").Parse(lexical);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Error).IsEqualTo(DatatypeError.InvalidCharacter);
        await Assert.That(result.ErrorOffset).IsEqualTo(offset);
    }

    [Test]
    [Arguments("")]
    [Arguments("+")]
    [Arguments("-")]
    [Arguments("+-1")]
    public async Task Integer_RejectsAnEmptyOrSignOnlyForm(string lexical)
    {
        var result = Integer("integer").Parse(lexical);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Error).IsNotEqualTo(DatatypeError.None);
    }

    [Test]
    public async Task Integer_CapsTheNumberOfDigits()
    {
        var result = Integer("integer").Parse(new string('1', XsdDecimal.DefaultMaxDigits + 1));

        await Assert.That(result.Error).IsEqualTo(DatatypeError.DigitCap);
        await Assert.That(Integer("integer").Parse(new string('1', XsdDecimal.DefaultMaxDigits)).Success).IsTrue();
    }

    [Test]
    [Arguments("+7", "7")]
    [Arguments("007", "7")]
    [Arguments("+007", "7")]
    [Arguments("-007", "-7")]
    [Arguments("-0", "0")]
    [Arguments("+0000", "0")]
    [Arguments("  12 ", "12")]
    [Arguments("1000", "1000")]
    public async Task Integer_CanonicalForm_HasNoPlusSignAndNoLeadingZeros(string lexical, string canonical)
    {
        var result = Integer("integer").Parse(lexical);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(XsdNumeric.ToCanonicalString(result.Value)).IsEqualTo(canonical);
    }

    [Test]
    [Arguments("+007", "7")]
    [Arguments("-0", "0")]
    [Arguments("0127", "127")]
    public async Task BoundedTypes_CanonicalForm_HasNoPlusSignAndNoLeadingZeros(string lexical, string canonical)
    {
        foreach (var name in new[] { "long", "int", "short", "byte", "nonNegativeInteger", "unsignedByte" })
        {
            var result = Integer(name).Parse(lexical);

            await Assert.That(result.Success).IsTrue();
            await Assert.That(XsdNumeric.ToCanonicalString(result.Value)).IsEqualTo(canonical);
        }
    }

    [Test]
    [Arguments("5", "5.0")]
    [Arguments("+5", "5.0")]
    [Arguments("5.", "5.0")]
    [Arguments(".5", "0.5")]
    [Arguments("007.500", "7.5")]
    [Arguments("-0", "0.0")]
    [Arguments("0.000", "0.0")]
    [Arguments("100", "100.0")]
    [Arguments("-12.340", "-12.34")]
    public async Task Decimal_CanonicalForm_AlwaysHasADecimalPointAndAFractionDigit(string lexical, string canonical)
    {
        var result = ((IDatatypeDescriptor<XsdDecimal>)Get("decimal")).Parse(lexical);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(XsdNumeric.ToCanonicalString(result.Value)).IsEqualTo(canonical);
    }

    [Test]
    [Arguments("1e3")]
    [Arguments("1.2.3")]
    [Arguments("")]
    [Arguments("INF")]
    public async Task Decimal_RejectsFormsOutsideTheLexicalSpace(string lexical) =>
        await Assert.That(Get("decimal").Validate(lexical).IsValid).IsFalse();

    [Test]
    public async Task Decimal_OrdersAndComparesByValue()
    {
        var type = (IDatatypeDescriptor<XsdDecimal>)Get("decimal");
        var one = type.Parse("1.0").Value;

        await Assert.That(type.Compare(one, type.Parse("1").Value)).IsEqualTo(XsdOrder.Equal);
        await Assert.That(type.Compare(one, type.Parse("1.5").Value)).IsEqualTo(XsdOrder.Less);
        await Assert.That(type.Compare(type.Parse("2").Value, one)).IsEqualTo(XsdOrder.Greater);
        await Assert.That(type.IsIdentical(one, type.Parse("01.00").Value)).IsTrue();
        await Assert.That(type.IsOrdered).IsTrue();
    }

    [Test]
    public async Task Integer_OrdersAndComparesByValue()
    {
        var type = Integer("integer");

        var zero = new XsdInteger(BigInteger.Zero);
        var one = new XsdInteger(BigInteger.One);

        await Assert.That(type.Compare(one, zero)).IsEqualTo(XsdOrder.Greater);
        await Assert.That(type.Compare(zero, one)).IsEqualTo(XsdOrder.Less);
        await Assert.That(type.Compare(one, one)).IsEqualTo(XsdOrder.Equal);
        await Assert.That(type.IsIdentical(one, one)).IsTrue();
        var oneAgain = new XsdInteger(BigInteger.One);
        await Assert.That(one > zero && zero < one && one >= oneAgain && one <= oneAgain && one != zero).IsTrue();
        await Assert.That(one.Equals((object)new XsdInteger(BigInteger.One))).IsTrue();
        await Assert.That(one.GetHashCode()).IsEqualTo(new XsdInteger(BigInteger.One).GetHashCode());
        await Assert.That(type.IsOrdered).IsTrue();
    }

    [Test]
    [Arguments("0")]
    [Arguments("-0")]
    [Arguments("+0")]
    public async Task NegativeInteger_RejectsZeroAndNegativeZero(string lexical)
    {
        var result = Get("negativeInteger").Validate(lexical);

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.FailedFacet).IsEqualTo(FacetKind.MaxInclusive);
        await Assert.That(Get("negativeInteger").Validate("-1").IsValid).IsTrue();
    }

    [Test]
    public async Task NonNegativeInteger_AcceptsNegativeZero()
    {
        await Assert.That(Get("nonNegativeInteger").Validate("-0").IsValid).IsTrue();
        await Assert.That(Get("nonNegativeInteger").Validate("-1").IsValid).IsFalse();
        await Assert.That(Get("positiveInteger").Validate("-0").IsValid).IsFalse();
    }

    [Test]
    [Arguments("integer", "decimal")]
    [Arguments("nonPositiveInteger", "integer")]
    [Arguments("negativeInteger", "nonPositiveInteger")]
    [Arguments("long", "integer")]
    [Arguments("int", "long")]
    [Arguments("short", "int")]
    [Arguments("byte", "short")]
    [Arguments("nonNegativeInteger", "integer")]
    [Arguments("unsignedLong", "nonNegativeInteger")]
    [Arguments("unsignedInt", "unsignedLong")]
    [Arguments("unsignedShort", "unsignedInt")]
    [Arguments("unsignedByte", "unsignedShort")]
    [Arguments("positiveInteger", "nonNegativeInteger")]
    public async Task DerivationChain_MatchesTheSpecificationHierarchy(string name, string baseName)
    {
        var type = Get(name);

        await Assert.That(type.BaseType).IsNotNull();
        await Assert.That(type.BaseType!.Name).IsEqualTo(baseName);
        await Assert.That(type.BaseType).IsSameReferenceAs(Get(baseName));
        await Assert.That(type.Variety).IsEqualTo(DatatypeVariety.Atomic);
    }

    [Test]
    public async Task Decimal_IsThePrimitiveRootOfTheFamily() => await Assert.That(Get("decimal").BaseType).IsNull();

    [Test]
    public async Task Parse_FacetViolation_ReportsAFacetViolation()
    {
        var result = Integer("byte").Parse("128");

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Error).IsEqualTo(DatatypeError.FacetViolation);
    }

    [Test]
    public async Task Parse_LargeValues_MapExactly()
    {
        await Assert.That(Integer("long").Parse("9223372036854775807").Value).IsEqualTo(new XsdInteger(long.MaxValue));
        await Assert
            .That(Integer("unsignedLong").Parse("18446744073709551615").Value)
            .IsEqualTo(new XsdInteger(ulong.MaxValue));
        await Assert
            .That(Integer("integer").Parse("1" + new string('0', 30)).Value)
            .IsEqualTo(new XsdInteger(BigInteger.Pow(10, 30)));
    }
}
