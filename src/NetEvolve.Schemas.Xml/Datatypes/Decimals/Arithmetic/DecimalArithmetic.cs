namespace NetEvolve.Schemas.Xml.Datatypes.Decimals.Arithmetic;

using System;
using System.Globalization;
using System.Numerics;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;

/// <summary>
/// Division, rounding, and numeric conversions for <see cref="XsdDecimal"/> (XQuery 1.0 and XPath 2.0 Functions and Operators,
/// sections 6.2 and 6.4). Invalid input never throws: a fallible operation returns an <see cref="ArithmeticResult{TValue}"/> with a coded error.
/// </summary>
/// <remarks>
/// Limitations: the result precision of a decimal division is implementation-defined in the specification; <see cref="Divide"/> rounds
/// half to even at an explicit number of fraction digits, <see cref="DefaultFractionDigits"/> by default. Operations align operands by
/// scaling with a power of ten, so the cost grows with the scale difference; callers handling untrusted scales must bound them.
/// A scale difference beyond the 32-bit range is reported as <see cref="DatatypeError.Overflow"/>.
/// <see cref="ToDecimal"/> converts exactly or fails; it never rounds. <see cref="ToDouble"/> relies on the correctly rounded
/// parser of the runtime.
/// </remarks>
public static class DecimalArithmetic
{
    /// <summary>The number of fraction digits <see cref="Divide"/> uses by default.</summary>
    public const int DefaultFractionDigits = 18;

    private const int MaxDoubleExponent = 400;

    private static readonly BigInteger DecimalMagnitudeMax = (BigInteger.One << 96) - 1;

    private enum RoundingMode
    {
        HalfUp,
        HalfEven,
        Floor,
        Ceiling,
    }

    /// <summary>Divides two values (<c>op:numeric-divide</c>), rounding half to even at a number of fraction digits.</summary>
    /// <param name="dividend">The dividend.</param>
    /// <param name="divisor">The divisor.</param>
    /// <param name="fractionDigits">The number of fraction digits of the quotient before normalization; negative rounds to a power of ten. An exact quotient gets no padding digits.</param>
    /// <returns>The quotient, <see cref="DatatypeError.DivisionByZero"/>, or <see cref="DatatypeError.Overflow"/>.</returns>
    public static ArithmeticResult<XsdDecimal> Divide(
        XsdDecimal dividend,
        XsdDecimal divisor,
        int fractionDigits = DefaultFractionDigits
    )
    {
        if (divisor.Sign == 0)
        {
            return new ArithmeticResult<XsdDecimal>(DatatypeError.DivisionByZero);
        }

        if (dividend.Sign == 0)
        {
            return new ArithmeticResult<XsdDecimal>(XsdDecimal.Zero);
        }

        // quotient x 10^fractionDigits = dividendMantissa x 10^e / divisorMantissa
        var exponent = (long)fractionDigits + divisor.Scale - dividend.Scale;
        var numerator = dividend.Mantissa;
        var denominator = divisor.Mantissa;
        if (exponent >= 0)
        {
            if (exponent > int.MaxValue)
            {
                return new ArithmeticResult<XsdDecimal>(DatatypeError.Overflow);
            }

            numerator *= BigInteger.Pow(10, (int)exponent);
        }
        else
        {
            if (-exponent > int.MaxValue)
            {
                return new ArithmeticResult<XsdDecimal>(DatatypeError.Overflow);
            }

            denominator *= BigInteger.Pow(10, (int)-exponent);
        }

        var quotient = BigInteger.DivRem(numerator, denominator, out var remainder);
        var comparison = (BigInteger.Abs(remainder) * 2).CompareTo(BigInteger.Abs(denominator));
        if (comparison > 0 || (comparison == 0 && !quotient.IsEven))
        {
            quotient += numerator.Sign * denominator.Sign;
        }

        return Make(quotient, fractionDigits);
    }

    /// <summary>Divides and truncates toward zero (<c>op:numeric-integer-divide</c>).</summary>
    /// <param name="dividend">The dividend.</param>
    /// <param name="divisor">The divisor.</param>
    /// <returns>The integer quotient, <see cref="DatatypeError.DivisionByZero"/>, or <see cref="DatatypeError.Overflow"/>.</returns>
    public static ArithmeticResult<BigInteger> IntegerDivide(XsdDecimal dividend, XsdDecimal divisor)
    {
        if (divisor.Sign == 0)
        {
            return new ArithmeticResult<BigInteger>(DatatypeError.DivisionByZero);
        }

        return TryAlign(dividend, divisor, out var a, out var b, out _)
            ? new ArithmeticResult<BigInteger>(BigInteger.Divide(a, b))
            : new ArithmeticResult<BigInteger>(DatatypeError.Overflow);
    }

    /// <summary>Gets the remainder of a truncating division, with the sign of the dividend (<c>op:numeric-mod</c>).</summary>
    /// <param name="dividend">The dividend.</param>
    /// <param name="divisor">The divisor.</param>
    /// <returns>The remainder, <see cref="DatatypeError.DivisionByZero"/>, or <see cref="DatatypeError.Overflow"/>.</returns>
    public static ArithmeticResult<XsdDecimal> Remainder(XsdDecimal dividend, XsdDecimal divisor)
    {
        if (divisor.Sign == 0)
        {
            return new ArithmeticResult<XsdDecimal>(DatatypeError.DivisionByZero);
        }

        return TryAlign(dividend, divisor, out var a, out var b, out var scale)
            ? Make(BigInteger.Remainder(a, b), scale)
            : new ArithmeticResult<XsdDecimal>(DatatypeError.Overflow);
    }

    /// <summary>Rounds to a precision, ties toward positive infinity (<c>fn:round</c>): 2.5 gives 3 and -2.5 gives -2.</summary>
    /// <param name="value">The value.</param>
    /// <param name="precision">The number of fraction digits kept; negative rounds to a power of ten.</param>
    /// <returns>The rounded value.</returns>
    /// <exception cref="OverflowException">The normalized scale does not fit a 32-bit integer.</exception>
    public static XsdDecimal Round(XsdDecimal value, int precision = 0) =>
        Quantize(value, precision, RoundingMode.HalfUp);

    /// <summary>Rounds to a precision, ties to the even digit (<c>fn:round-half-to-even</c>): 2.5 gives 2 and 3.5 gives 4.</summary>
    /// <param name="value">The value.</param>
    /// <param name="precision">The number of fraction digits kept; negative rounds to a power of ten.</param>
    /// <returns>The rounded value.</returns>
    /// <exception cref="OverflowException">The normalized scale does not fit a 32-bit integer.</exception>
    public static XsdDecimal RoundHalfToEven(XsdDecimal value, int precision = 0) =>
        Quantize(value, precision, RoundingMode.HalfEven);

    /// <summary>Gets the largest integer not greater than the value (<c>fn:floor</c>).</summary>
    /// <param name="value">The value.</param>
    /// <returns>The floor.</returns>
    public static XsdDecimal Floor(XsdDecimal value) => Quantize(value, 0, RoundingMode.Floor);

    /// <summary>Gets the smallest integer not less than the value (<c>fn:ceiling</c>).</summary>
    /// <param name="value">The value.</param>
    /// <returns>The ceiling.</returns>
    public static XsdDecimal Ceiling(XsdDecimal value) => Quantize(value, 0, RoundingMode.Ceiling);

    /// <summary>Gets the absolute value (<c>fn:abs</c>).</summary>
    /// <param name="value">The value.</param>
    /// <returns>The magnitude.</returns>
    public static XsdDecimal Abs(XsdDecimal value) => value.Sign < 0 ? value.Negate() : value;

    /// <summary>Converts an integer exactly.</summary>
    /// <param name="value">The integer.</param>
    /// <returns>The decimal.</returns>
    public static XsdDecimal FromBigInteger(BigInteger value) => new(value, 0);

    /// <summary>Converts to an integer, truncating toward zero.</summary>
    /// <param name="value">The decimal.</param>
    /// <returns>The integer, or <see cref="DatatypeError.Overflow"/> when a negative scale beyond the 32-bit range cannot be expanded.</returns>
    public static ArithmeticResult<BigInteger> ToBigInteger(XsdDecimal value)
    {
        var scale = value.Scale;
        if (scale <= 0)
        {
            return -(long)scale > int.MaxValue
                ? new ArithmeticResult<BigInteger>(DatatypeError.Overflow)
                : new ArithmeticResult<BigInteger>(value.Mantissa * BigInteger.Pow(10, -scale));
        }

        return scale >= MantissaDigits(value)
            ? new ArithmeticResult<BigInteger>(BigInteger.Zero)
            : new ArithmeticResult<BigInteger>(BigInteger.Divide(value.Mantissa, BigInteger.Pow(10, scale)));
    }

    /// <summary>Converts a <see cref="double"/> exactly, from its IEEE 754 bits.</summary>
    /// <param name="value">The double.</param>
    /// <returns>The decimal (negative zero gives zero), or <see cref="DatatypeError.InvalidNumericOperand"/> for NaN and the infinities.</returns>
    public static ArithmeticResult<XsdDecimal> FromDouble(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            return new ArithmeticResult<XsdDecimal>(DatatypeError.InvalidNumericOperand);
        }

        var bits = BitConverter.DoubleToInt64Bits(value);
        var biasedExponent = (int)((bits >> 52) & 0x7FF);
        var fraction = bits & 0xFFFFFFFFFFFFFL;
        var significand = biasedExponent == 0 ? fraction : fraction | (1L << 52);
        if (significand == 0)
        {
            return new ArithmeticResult<XsdDecimal>(XsdDecimal.Zero);
        }

        var mantissa = bits < 0 ? -new BigInteger(significand) : new BigInteger(significand);
        var exponent = (biasedExponent == 0 ? 1 : biasedExponent) - 1075;

        // significand x 2^exponent; for a negative exponent multiply by 5^-exponent and divide by 10^-exponent.
        return new ArithmeticResult<XsdDecimal>(
            exponent >= 0
                ? new XsdDecimal(mantissa << exponent, 0)
                : new XsdDecimal(mantissa * BigInteger.Pow(5, -exponent), -exponent)
        );
    }

    /// <summary>Converts to the nearest <see cref="double"/>, ties to even; beyond the double range the result is an infinity or a zero.</summary>
    /// <param name="value">The decimal.</param>
    /// <returns>The double.</returns>
    public static double ToDouble(XsdDecimal value)
    {
        if (value.Sign == 0)
        {
            return 0d;
        }

        // value lies in [10^(exponent-1), 10^exponent); far outside the double range, skip formatting a huge string.
        var exponent = MantissaDigits(value) - (long)value.Scale;
        if (exponent > MaxDoubleExponent)
        {
            return value.Sign < 0 ? double.NegativeInfinity : double.PositiveInfinity;
        }

        if (exponent < -MaxDoubleExponent)
        {
            return value.Sign < 0 ? -0d : 0d;
        }

        return double.Parse(value.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture);
    }

    /// <summary>Converts a <see cref="decimal"/> exactly.</summary>
    /// <param name="value">The System.Decimal.</param>
    /// <returns>The decimal.</returns>
    public static XsdDecimal FromDecimal(decimal value)
    {
        var bits = decimal.GetBits(value);
        var mantissa =
            (new BigInteger((uint)bits[2]) << 64) + new BigInteger(((ulong)(uint)bits[1] << 32) | (uint)bits[0]);
        return new XsdDecimal(bits[3] < 0 ? -mantissa : mantissa, (bits[3] >> 16) & 0xFF);
    }

    /// <summary>Converts to <see cref="decimal"/> exactly: it never rounds.</summary>
    /// <param name="value">The decimal.</param>
    /// <returns>The System.Decimal, or <see cref="DatatypeError.Overflow"/> when the value is beyond its range or needs more than 28 fraction digits.</returns>
    public static ArithmeticResult<decimal> ToDecimal(XsdDecimal value)
    {
        var mantissa = value.Mantissa;
        var scale = value.Scale;
        if (scale < 0)
        {
            // 10^29 exceeds the 96-bit range for every nonzero mantissa.
            if (scale < -28)
            {
                return new ArithmeticResult<decimal>(DatatypeError.Overflow);
            }

            mantissa *= BigInteger.Pow(10, -scale);
            scale = 0;
        }

        var magnitude = BigInteger.Abs(mantissa);
        if (scale > 28 || magnitude > DecimalMagnitudeMax)
        {
            return new ArithmeticResult<decimal>(DatatypeError.Overflow);
        }

        return new ArithmeticResult<decimal>(
            new decimal(
                unchecked((int)(uint)(magnitude & uint.MaxValue)),
                unchecked((int)(uint)((magnitude >> 32) & uint.MaxValue)),
                unchecked((int)(uint)(magnitude >> 64)),
                mantissa.Sign < 0,
                (byte)scale
            )
        );
    }

    private static ArithmeticResult<XsdDecimal> Make(BigInteger mantissa, int scale)
    {
        try
        {
            return new ArithmeticResult<XsdDecimal>(new XsdDecimal(mantissa, scale));
        }
        catch (OverflowException)
        {
            return new ArithmeticResult<XsdDecimal>(DatatypeError.Overflow);
        }
    }

    // Both mantissas at the larger scale, so that a / b = a' / b'.
    private static bool TryAlign(
        XsdDecimal a,
        XsdDecimal b,
        out BigInteger alignedA,
        out BigInteger alignedB,
        out int scale
    )
    {
        scale = Math.Max(a.Scale, b.Scale);
        var shiftA = (long)scale - a.Scale;
        var shiftB = (long)scale - b.Scale;
        alignedA = default;
        alignedB = default;
        if (shiftA > int.MaxValue || shiftB > int.MaxValue)
        {
            return false;
        }

        alignedA = a.Mantissa * BigInteger.Pow(10, (int)shiftA);
        alignedB = b.Mantissa * BigInteger.Pow(10, (int)shiftB);
        return true;
    }

    private static int MantissaDigits(XsdDecimal value) =>
        BigInteger.Abs(value.Mantissa).ToString(CultureInfo.InvariantCulture).Length;

    private static XsdDecimal Quantize(XsdDecimal value, int precision, RoundingMode mode)
    {
        if (value.Sign == 0 || value.Scale <= precision)
        {
            return value;
        }

        var drop = (long)value.Scale - precision;
        var magnitude = BigInteger.Abs(value.Mantissa);
        BigInteger quotient;
        int half;
        if (drop > MantissaDigits(value))
        {
            // The magnitude is below one unit and below one half of it.
            quotient = BigInteger.Zero;
            half = -1;
        }
        else
        {
            var unit = BigInteger.Pow(10, (int)drop);
            quotient = BigInteger.DivRem(magnitude, unit, out var remainder);
            half = remainder.IsZero ? -2 : (remainder * 2).CompareTo(unit);
        }

        // half: -2 exact, -1 below a half, 0 a tie, 1 above a half
        var increment = mode switch
        {
            RoundingMode.HalfUp => half > 0 || (half == 0 && value.Sign > 0),
            RoundingMode.HalfEven => half > 0 || (half == 0 && !quotient.IsEven),
            RoundingMode.Floor => value.Sign < 0 && half != -2,
            _ => value.Sign > 0 && half != -2,
        };

        if (increment)
        {
            quotient += BigInteger.One;
        }

        return new XsdDecimal(value.Sign < 0 ? -quotient : quotient, precision);
    }
}
