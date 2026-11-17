namespace NetEvolve.Schemas.Xml.Tests.Unit.Datatypes.Decimals;

using System;
using System.Globalization;
using System.Numerics;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Decimals;

public class XsdDecimalTests
{
    private const string Digits40 = "1234567890123456789012345678901234567890";
    private const string Digits39 = "123456789012345678901234567890123456789";

    private static XsdDecimal P(string lexical)
    {
        var result = XsdDecimal.Parse(lexical.AsSpan());
        return result.Success ? result.Value : throw new InvalidOperationException(lexical);
    }

    [Test]
    [Arguments(Digits40, Digits40)]
    [Arguments("-" + Digits40 + "." + Digits40 + "00", "-" + Digits40 + "." + Digits39)]
    [Arguments("+007.500", "7.5")]
    [Arguments(".5", "0.5")]
    [Arguments("5.", "5")]
    [Arguments("-0.000", "0")]
    [Arguments("0.0500", "0.05")]
    [Arguments("1200", "1200")]
    [Arguments("0001.10", "1.1")]
    public async Task Parse_ValidLexical_RoundTripsThroughTheCanonicalForm(string lexical, string canonical)
    {
        var result = XsdDecimal.Parse(lexical.AsSpan());

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Value.ToString()).IsEqualTo(canonical);
    }

    [Test]
    public async Task Parse_MoreThan29SignificantDigits_UsesTheBigIntegerPathWithoutLoss()
    {
        var value = P(Digits40 + ".5");

        await Assert.That(value.Mantissa).IsEqualTo(BigInteger.Parse(Digits40 + "5", CultureInfo.InvariantCulture));
        await Assert.That(value.Scale).IsEqualTo(1);
    }

    [Test]
    public async Task Parse_EmptyLoneSignTwoPoints_ReturnsInvalidLexicalForm()
    {
        var empty = XsdDecimal.Parse(ReadOnlySpan<char>.Empty);
        var twoPoints = XsdDecimal.Parse("1.2.3".AsSpan());

        await Assert.That(empty.Success).IsFalse();
        await Assert.That(empty.Error).IsEqualTo(DatatypeError.InvalidLexicalForm);
        await Assert.That(empty.ErrorOffset).IsEqualTo(0);
        await Assert.That(XsdDecimal.Parse("-".AsSpan()).Error).IsEqualTo(DatatypeError.InvalidLexicalForm);
        await Assert.That(XsdDecimal.Parse("+.".AsSpan()).Error).IsEqualTo(DatatypeError.InvalidLexicalForm);
        await Assert.That(twoPoints.Error).IsEqualTo(DatatypeError.InvalidLexicalForm);
        await Assert.That(twoPoints.ErrorOffset).IsEqualTo(3);
    }

    [Test]
    [Arguments("1e5", 1)]
    [Arguments("1E5", 1)]
    [Arguments("12 ", 2)]
    [Arguments(" 1", 0)]
    [Arguments("1,5", 1)]
    [Arguments("1.5x", 3)]
    [Arguments("--1", 1)]
    [Arguments("1-", 1)]
    [Arguments("INF", 0)]
    public async Task Parse_ExponentAndStrayCharacters_ReturnInvalidCharacterWithOffset(string lexical, int offset)
    {
        var result = XsdDecimal.Parse(lexical.AsSpan());

        await Assert.That(result.Error).IsEqualTo(DatatypeError.InvalidCharacter);
        await Assert.That(result.ErrorOffset).IsEqualTo(offset);
    }

    [Test]
    public async Task Parse_MoreDigitsThanTheCap_ReturnsDigitCapAtTheOffendingDigit()
    {
        var atCap = XsdDecimal.Parse(new string('1', 4096).AsSpan());
        var overCap = XsdDecimal.Parse(("-" + new string('1', 2048) + "." + new string('1', 2049)).AsSpan());
        var custom = XsdDecimal.Parse("123456".AsSpan(), 5);

        await Assert.That(atCap.Success).IsTrue();
        await Assert.That(overCap.Error).IsEqualTo(DatatypeError.DigitCap);
        await Assert.That(overCap.ErrorOffset).IsEqualTo(4098);
        await Assert.That(custom.Error).IsEqualTo(DatatypeError.DigitCap);
        await Assert.That(custom.ErrorOffset).IsEqualTo(5);
    }

    [Test]
    public async Task Parse_NonPositiveMaxDigits_Throws() =>
        await Assert.That(() => XsdDecimal.Parse("1".AsSpan(), 0)).Throws<ArgumentOutOfRangeException>();

    [Test]
    [Arguments("1", "1.0")]
    [Arguments("1", "1.00")]
    [Arguments("0", "-0.000")]
    [Arguments("100", "100.0")]
    [Arguments("-2.50", "-2.5")]
    public async Task Equality_IgnoresTrailingZerosAndHashCodesMatch(string a, string b)
    {
        var x = P(a);
        var y = P(b);

        await Assert.That(x == y).IsTrue();
        await Assert.That(x.Equals((object)y)).IsTrue();
        await Assert.That(x.GetHashCode()).IsEqualTo(y.GetHashCode());
        await Assert.That(x.CompareTo(y)).IsEqualTo(0);
    }

    [Test]
    public async Task Equality_DifferentValues_AreNotEqual()
    {
        await Assert.That(P("1") != P("1.01")).IsTrue();
        await Assert.That(P("1").Equals((object)"1")).IsFalse();
        await Assert.That(P(Digits40) != P(Digits40 + "1")).IsTrue();
    }

    [Test]
    [Arguments("-1", "1", -1)]
    [Arguments("1", "-1", 1)]
    [Arguments("0", "-0.001", 1)]
    [Arguments("-0.001", "0", -1)]
    [Arguments("0.5", "0.25", 1)]
    [Arguments("0.25", "0.5", -1)]
    [Arguments("-0.5", "-0.25", -1)]
    [Arguments("-0.25", "-0.5", 1)]
    [Arguments("10", "9.99", 1)]
    [Arguments("1000", "999.9999", 1)]
    [Arguments("0.001", "0.0009", 1)]
    [Arguments("1.234", "1.2341", -1)]
    [Arguments("12345678901234567890123456789012345678901", "12345678901234567890123456789012345678900.99", 1)]
    [Arguments("-12345678901234567890123456789012345678901", "-12345678901234567890123456789012345678900.99", -1)]
    public async Task CompareTo_OrdersAcrossScalesAndSigns(string a, string b, int expected)
    {
        var x = P(a);
        var y = P(b);

        await Assert.That(Math.Sign(x.CompareTo(y))).IsEqualTo(expected);
        await Assert.That(Math.Sign(y.CompareTo(x))).IsEqualTo(-expected);
        await Assert.That(x < y).IsEqualTo(expected < 0);
        await Assert.That(x <= y).IsEqualTo(expected <= 0);
        await Assert.That(x > y).IsEqualTo(expected > 0);
        await Assert.That(x >= y).IsEqualTo(expected >= 0);
    }

    [Test]
    [Arguments("0.1", "0.2", "0.3")]
    [Arguments("1", "-1", "0")]
    [Arguments("99999999999999999999", "1", "100000000000000000000")]
    [Arguments("1.5", "-2.25", "-0.75")]
    [Arguments("100", "0.001", "100.001")]
    [Arguments("12345678901234567890123456789012345678901.5", "0.5", "12345678901234567890123456789012345678902")]
    [Arguments("-12345678901234567890123456789012345678901", "12345678901234567890123456789012345678900.25", "-0.75")]
    public async Task Add_IsExact(string a, string b, string sum)
    {
        await Assert.That(P(a).Add(P(b)).ToString()).IsEqualTo(sum);
        await Assert.That(P(b).Add(P(a))).IsEqualTo(P(sum));
    }

    [Test]
    [Arguments("0.3", "0.1", "0.2")]
    [Arguments("1", "1.00", "0")]
    [Arguments("0", "1", "-1")]
    [Arguments("12345678901234567890123456789012345678901", "0.01", "12345678901234567890123456789012345678900.99")]
    [Arguments("-5", "12345678901234567890123456789012345678901", "-12345678901234567890123456789012345678906")]
    public async Task Subtract_IsExact(string a, string b, string difference) =>
        await Assert.That(P(a).Subtract(P(b)).ToString()).IsEqualTo(difference);

    [Test]
    [Arguments("0.1", "0.1", "0.01")]
    [Arguments("-1.5", "2", "-3")]
    [Arguments("0", "-12345678901234567890123456789012345678901", "0")]
    [Arguments("100", "100", "10000")]
    [Arguments("4294967296", "4294967296", "18446744073709551616")]
    [Arguments(
        "12345678901234567890123456789",
        "98765432109876543210987654321",
        "1219326311370217952261850327336229233322374638011112635269"
    )]
    [Arguments("-0.00000000000000000000000000003", "30000000000000000000000000000", "-0.9")]
    public async Task Multiply_IsExact(string a, string b, string product)
    {
        await Assert.That(P(a).Multiply(P(b)).ToString()).IsEqualTo(product);
        await Assert.That(P(b).Multiply(P(a))).IsEqualTo(P(product));
    }

    [Test]
    public async Task Negate_FlipsTheSignAndKeepsZero()
    {
        await Assert.That(P("1.5").Negate().ToString()).IsEqualTo("-1.5");
        await Assert.That(P("-" + Digits40).Negate().ToString()).IsEqualTo(Digits40);
        await Assert.That(P("0").Negate()).IsEqualTo(XsdDecimal.Zero);
        await Assert.That(P("0.0").Negate().Sign).IsEqualTo(0);
    }

    [Test]
    [Arguments("0", 1, 0)]
    [Arguments("0.000", 1, 0)]
    [Arguments("123", 3, 0)]
    [Arguments("100", 3, 0)]
    [Arguments("1200.00", 4, 0)]
    [Arguments("0.5", 1, 1)]
    [Arguments("0.05", 2, 2)]
    [Arguments("0.0500", 2, 2)]
    [Arguments("12.340", 4, 2)]
    [Arguments("-123.456", 6, 3)]
    [Arguments("0.001", 3, 3)]
    public async Task Digits_AreReportedAsTheTotalDigitsAndFractionDigitsFacetsDefineThem(
        string lexical,
        int total,
        int fraction
    )
    {
        var value = P(lexical);

        await Assert.That(value.TotalDigits).IsEqualTo(total);
        await Assert.That(value.FractionDigits).IsEqualTo(fraction);
    }

    [Test]
    public async Task Digits_OfAValueBeyondTheInlinePath_AreCounted()
    {
        var value = P(Digits40 + ".1234567890123456789012345678901234567891");

        await Assert.That(value.TotalDigits).IsEqualTo(80);
        await Assert.That(value.FractionDigits).IsEqualTo(40);
    }

    [Test]
    public async Task Constructor_NormalizesMantissaAndScale()
    {
        var value = new XsdDecimal(new BigInteger(12000), 0);

        await Assert.That(value.Mantissa).IsEqualTo(new BigInteger(12));
        await Assert.That(value.Scale).IsEqualTo(-3);
        await Assert.That(value.ToString()).IsEqualTo("12000");
        await Assert.That(new XsdDecimal(BigInteger.Zero, 7)).IsEqualTo(XsdDecimal.Zero);
        await Assert.That(XsdDecimal.Zero.Scale).IsEqualTo(0);
    }

    [Test]
    public async Task Results_BackOnTheInlinePath_EqualTheParsedValue()
    {
        var big = P("123456789012345678901234567890");
        var back = big.Subtract(P("123456789012345678901234567889"));

        await Assert.That(back).IsEqualTo(P("1"));
        await Assert.That(back.GetHashCode()).IsEqualTo(P("1.0").GetHashCode());
    }

    [Test]
    public async Task MaxUnsignedLongMagnitude_StaysInlineAndRoundTrips()
    {
        var max = P("18446744073709551615");
        var over = P("18446744073709551616");

        await Assert.That(max.ToString()).IsEqualTo("18446744073709551615");
        await Assert.That(over.ToString()).IsEqualTo("18446744073709551616");
        await Assert.That(max.Add(P("1"))).IsEqualTo(over);
        await Assert.That(max.CompareTo(over)).IsEqualTo(-1);
    }
}
