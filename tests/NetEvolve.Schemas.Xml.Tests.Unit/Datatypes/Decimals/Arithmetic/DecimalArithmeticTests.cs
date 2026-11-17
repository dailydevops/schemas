namespace NetEvolve.Schemas.Xml.Tests.Unit.Datatypes.Decimals.Arithmetic;

using System;
using System.Globalization;
using System.Numerics;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Decimals;
using NetEvolve.Schemas.Xml.Datatypes.Decimals.Arithmetic;

public class DecimalArithmeticTests
{
    private static XsdDecimal P(string lexical)
    {
        var result = XsdDecimal.Parse(lexical.AsSpan());
        return result.Success ? result.Value : throw new InvalidOperationException(lexical);
    }

    [Test]
    [Arguments("1", "3", 5, "0.33333")]
    [Arguments("2", "3", 5, "0.66667")]
    [Arguments("1", "3", 18, "0.333333333333333333")]
    [Arguments("1", "4", 18, "0.25")]
    [Arguments("10", "4", 18, "2.5")]
    [Arguments("1.5", "0.5", 18, "3")]
    [Arguments("-1", "3", 2, "-0.33")]
    [Arguments("0.125", "1", 2, "0.12")]
    [Arguments("0.375", "1", 2, "0.38")]
    [Arguments("1234", "3", -2, "400")]
    [Arguments("0", "7", 18, "0")]
    [Arguments("1", "3", 0, "0")]
    public async Task Divide_WithFractionDigits_RoundsHalfToEvenAndHasNoPaddingDigits(
        string a,
        string b,
        int fractionDigits,
        string expected
    )
    {
        var result = DecimalArithmetic.Divide(P(a), P(b), fractionDigits);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Error).IsEqualTo(DatatypeError.None);
        await Assert.That(result.Value.ToString()).IsEqualTo(expected);
    }

    [Test]
    public async Task Divide_WithoutFractionDigits_UsesEighteenDigits()
    {
        var result = DecimalArithmetic.Divide(P("2"), P("3"));

        await Assert.That(result.Value.ToString()).IsEqualTo("0.666666666666666667");
    }

    [Test]
    public async Task Divide_ByZero_ReturnsDivisionByZero()
    {
        var result = DecimalArithmetic.Divide(P("1"), XsdDecimal.Zero);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Error).IsEqualTo(DatatypeError.DivisionByZero);
    }

    [Test]
    public async Task Divide_ScaleBeyondThe32BitRange_ReturnsOverflow()
    {
        var result = DecimalArithmetic.Divide(new XsdDecimal(BigInteger.One, int.MinValue), P("1"), int.MaxValue);

        await Assert.That(result.Error).IsEqualTo(DatatypeError.Overflow);
    }

    [Test]
    [Arguments("7", "2", "3", "1")]
    [Arguments("-7", "2", "-3", "-1")]
    [Arguments("7", "-2", "-3", "1")]
    [Arguments("-7", "-2", "3", "-1")]
    [Arguments("7.5", "2", "3", "1.5")]
    [Arguments("-7.5", "2", "-3", "-1.5")]
    [Arguments("1", "3", "0", "1")]
    [Arguments("-1", "3", "0", "-1")]
    [Arguments("5.5", "0.5", "11", "0")]
    [Arguments("10", "0.3", "33", "0.1")]
    public async Task IntegerDivideAndRemainder_TruncateTowardZeroAndTakeTheSignOfTheDividend(
        string a,
        string b,
        string quotient,
        string remainder
    )
    {
        var integerDivide = DecimalArithmetic.IntegerDivide(P(a), P(b));
        var modulo = DecimalArithmetic.Remainder(P(a), P(b));

        await Assert.That(integerDivide.Success).IsTrue();
        await Assert.That(integerDivide.Value).IsEqualTo(BigInteger.Parse(quotient, CultureInfo.InvariantCulture));
        await Assert.That(modulo.Success).IsTrue();
        await Assert.That(modulo.Value.ToString()).IsEqualTo(remainder);
    }

    [Test]
    public async Task IntegerDivideAndRemainder_ByZero_ReturnDivisionByZero()
    {
        var integerDivide = DecimalArithmetic.IntegerDivide(P("1"), P("0.00"));
        var modulo = DecimalArithmetic.Remainder(P("1"), XsdDecimal.Zero);

        await Assert.That(integerDivide.Error).IsEqualTo(DatatypeError.DivisionByZero);
        await Assert.That(modulo.Error).IsEqualTo(DatatypeError.DivisionByZero);
        await Assert.That(modulo.Success).IsFalse();
    }

    [Test]
    public async Task IntegerDivideAndRemainder_ScaleDifferenceBeyondThe32BitRange_ReturnOverflow()
    {
        var a = new XsdDecimal(BigInteger.One, int.MaxValue);
        var b = new XsdDecimal(BigInteger.One, int.MinValue);

        await Assert.That(DecimalArithmetic.IntegerDivide(a, b).Error).IsEqualTo(DatatypeError.Overflow);
        await Assert.That(DecimalArithmetic.Remainder(a, b).Error).IsEqualTo(DatatypeError.Overflow);
    }

    [Test]
    [Arguments("2.5", 0, "3")]
    [Arguments("-2.5", 0, "-2")]
    [Arguments("2.4", 0, "2")]
    [Arguments("-2.6", 0, "-3")]
    [Arguments("1.125", 2, "1.13")]
    [Arguments("-1.125", 2, "-1.12")]
    [Arguments("1250", -2, "1300")]
    [Arguments("-1250", -2, "-1200")]
    [Arguments("5", 3, "5")]
    [Arguments("0.001", -1, "0")]
    [Arguments("-0.001", 0, "0")]
    [Arguments("0", 0, "0")]
    public async Task Round_HalfUpTowardPositiveInfinity(string value, int precision, string expected) =>
        await Assert.That(DecimalArithmetic.Round(P(value), precision).ToString()).IsEqualTo(expected);

    [Test]
    [Arguments("2.5", 0, "2")]
    [Arguments("3.5", 0, "4")]
    [Arguments("-2.5", 0, "-2")]
    [Arguments("-3.5", 0, "-4")]
    [Arguments("2.6", 0, "3")]
    [Arguments("1.125", 2, "1.12")]
    [Arguments("1.135", 2, "1.14")]
    [Arguments("1250", -2, "1200")]
    [Arguments("1350", -2, "1400")]
    [Arguments("-1250", -2, "-1200")]
    [Arguments("1251", -2, "1300")]
    [Arguments("0.0005", -3, "0")]
    [Arguments("1234567890123456789012345678901234567890.5", 0, "1234567890123456789012345678901234567890")]
    public async Task RoundHalfToEven_RoundsTiesToTheEvenDigit(string value, int precision, string expected) =>
        await Assert.That(DecimalArithmetic.RoundHalfToEven(P(value), precision).ToString()).IsEqualTo(expected);

    [Test]
    [Arguments("2.5", "2", "3")]
    [Arguments("-2.5", "-3", "-2")]
    [Arguments("2", "2", "2")]
    [Arguments("-0.1", "-1", "0")]
    [Arguments("0.1", "0", "1")]
    [Arguments("0", "0", "0")]
    [Arguments("-1000", "-1000", "-1000")]
    public async Task FloorAndCeiling_RoundTowardTheNegativeAndPositiveInfinity(
        string value,
        string floor,
        string ceiling
    )
    {
        await Assert.That(DecimalArithmetic.Floor(P(value)).ToString()).IsEqualTo(floor);
        await Assert.That(DecimalArithmetic.Ceiling(P(value)).ToString()).IsEqualTo(ceiling);
    }

    [Test]
    [Arguments("-2.5", "2.5")]
    [Arguments("2.5", "2.5")]
    [Arguments("0", "0")]
    public async Task Abs_ReturnsTheMagnitude(string value, string expected) =>
        await Assert.That(DecimalArithmetic.Abs(P(value)).ToString()).IsEqualTo(expected);

    [Test]
    public async Task FromBigIntegerAndToBigInteger_RoundTripAndTruncateTowardZero()
    {
        var big = BigInteger.Parse("-123456789012345678901234567890", CultureInfo.InvariantCulture);

        await Assert
            .That(DecimalArithmetic.FromBigInteger(big).ToString())
            .IsEqualTo("-123456789012345678901234567890");
        await Assert.That(DecimalArithmetic.ToBigInteger(DecimalArithmetic.FromBigInteger(big)).Value).IsEqualTo(big);
        await Assert.That(DecimalArithmetic.ToBigInteger(P("-2.9")).Value).IsEqualTo(new BigInteger(-2));
        await Assert.That(DecimalArithmetic.ToBigInteger(P("1200")).Value).IsEqualTo(new BigInteger(1200));
        await Assert.That(DecimalArithmetic.ToBigInteger(P("0.4")).Value).IsEqualTo(BigInteger.Zero);
    }

    [Test]
    public async Task ToBigInteger_ScaleBeyondThe32BitRange_ReturnsOverflow()
    {
        var result = DecimalArithmetic.ToBigInteger(new XsdDecimal(BigInteger.One, int.MinValue));

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Error).IsEqualTo(DatatypeError.Overflow);
    }

    [Test]
    [Arguments(0.5, "0.5")]
    [Arguments(0.1, "0.1000000000000000055511151231257827021181583404541015625")]
    [Arguments(-0.1, "-0.1000000000000000055511151231257827021181583404541015625")]
    [Arguments(1.0, "1")]
    [Arguments(-0.0, "0")]
    [Arguments(0.0, "0")]
    [Arguments(1e20, "100000000000000000000")]
    [Arguments(123456789.125, "123456789.125")]
    public async Task FromDouble_IsExact(double value, string expected)
    {
        var result = DecimalArithmetic.FromDouble(value);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Value.ToString()).IsEqualTo(expected);
    }

    [Test]
    public async Task FromDouble_ExtremesAreExact()
    {
        var max = DecimalArithmetic.FromDouble(double.MaxValue);
        var min = DecimalArithmetic.FromDouble(double.Epsilon);

        await Assert.That(max.Value.ToString()).StartsWith("17976931348623157");
        await Assert.That(max.Value.Scale).IsLessThanOrEqualTo(0);
        await Assert.That(DecimalArithmetic.ToDouble(max.Value)).IsEqualTo(double.MaxValue);
        await Assert.That(min.Value.Scale).IsEqualTo(1074);
        await Assert.That(DecimalArithmetic.ToDouble(min.Value)).IsEqualTo(double.Epsilon);
    }

    [Test]
    [Arguments(double.NaN)]
    [Arguments(double.PositiveInfinity)]
    [Arguments(double.NegativeInfinity)]
    public async Task FromDouble_NaNAndInfinities_AreRejected(double value)
    {
        var result = DecimalArithmetic.FromDouble(value);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Error).IsEqualTo(DatatypeError.InvalidNumericOperand);
    }

    [Test]
    [Arguments("0.5", 0.5)]
    [Arguments("-1234.5", -1234.5)]
    [Arguments("0", 0.0)]
    [Arguments("0.1", 0.1)]
    public async Task ToDouble_RoundsToNearest(string value, double expected) =>
        await Assert.That(DecimalArithmetic.ToDouble(P(value))).IsEqualTo(expected);

    [Test]
    public async Task ToDouble_BeyondTheDoubleRange_IsInfinity()
    {
        var huge = new XsdDecimal(BigInteger.One, -400);

        await Assert.That(double.IsPositiveInfinity(DecimalArithmetic.ToDouble(huge))).IsTrue();
        await Assert.That(double.IsNegativeInfinity(DecimalArithmetic.ToDouble(huge.Negate()))).IsTrue();
    }

    [Test]
    [Arguments("79228162514264337593543950335")]
    [Arguments("-79228162514264337593543950335")]
    [Arguments("0.1234567890123456789012345678")]
    [Arguments("1200")]
    [Arguments("0")]
    public async Task FromDecimalAndToDecimal_RoundTripInRange(string lexical)
    {
        var expected = decimal.Parse(lexical, CultureInfo.InvariantCulture);

        var converted = DecimalArithmetic.ToDecimal(P(lexical));
        var back = DecimalArithmetic.FromDecimal(expected);

        await Assert.That(converted.Success).IsTrue();
        await Assert.That(converted.Value).IsEqualTo(expected);
        await Assert.That(back.ToString()).IsEqualTo(P(lexical).ToString());
    }

    [Test]
    public async Task FromDecimal_IgnoresTrailingZerosAndKeepsTheSign()
    {
        await Assert.That(DecimalArithmetic.FromDecimal(1.500m).ToString()).IsEqualTo("1.5");
        await Assert
            .That(DecimalArithmetic.FromDecimal(decimal.MinValue).ToString())
            .IsEqualTo("-79228162514264337593543950335");
        await Assert.That(DecimalArithmetic.FromDecimal(0.0m)).IsEqualTo(XsdDecimal.Zero);
    }

    [Test]
    [Arguments("79228162514264337593543950336")]
    [Arguments("-79228162514264337593543950336")]
    [Arguments("1000000000000000000000000000000")]
    [Arguments("0.00000000000000000000000000001")]
    [Arguments("0.12345678901234567890123456789")]
    public async Task ToDecimal_BeyondRangeOrPrecision_ReturnsOverflow(string lexical)
    {
        var result = DecimalArithmetic.ToDecimal(P(lexical));

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Error).IsEqualTo(DatatypeError.Overflow);
    }

    [Test]
    public async Task ToDecimal_HugeNegativeScale_ReturnsOverflowWithoutExpanding()
    {
        var result = DecimalArithmetic.ToDecimal(new XsdDecimal(BigInteger.One, int.MinValue));

        await Assert.That(result.Error).IsEqualTo(DatatypeError.Overflow);
    }

    [Test]
    public async Task ArithmeticResult_SuccessAndFailureCarryValueAndError()
    {
        var ok = new ArithmeticResult<int>(5);
        var failed = new ArithmeticResult<int>(DatatypeError.Overflow);

        await Assert.That(ok.Success).IsTrue();
        await Assert.That(ok.Value).IsEqualTo(5);
        await Assert.That(ok.Error).IsEqualTo(DatatypeError.None);
        await Assert.That(failed.Success).IsFalse();
        await Assert.That(failed.Value).IsEqualTo(0);
        await Assert.That(failed.Error).IsEqualTo(DatatypeError.Overflow);
    }
}
