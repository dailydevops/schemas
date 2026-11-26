namespace NetEvolve.Schemas.XPath.Functions.Numerics;

using System;
using System.Numerics;
using NetEvolve.Schemas.Xml.Datatypes.Decimals;
using NetEvolve.Schemas.Xml.Datatypes.Decimals.Arithmetic;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Evaluation.Arithmetic;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The value rules of <c>fn:abs</c>, <c>fn:ceiling</c>, <c>fn:floor</c>, <c>fn:round</c> (with and without a precision), and <c>fn:round-half-to-even</c>
/// (Functions and Operators 1.0, 6.4, and 3.1, 4.4): each result has the type of the argument, an <c>xs:integer</c> or <c>xs:decimal</c> is exact at any
/// width, and a floating-point zero keeps the sign of the argument, so that rounding -0.3 gives negative zero.
/// </summary>
/// <remarks>
/// Steady state allocates nothing for the 64-bit integer, <c>xs:float</c>, and <c>xs:double</c> paths, and for an argument that the
/// function leaves as it is; a decimal that changes and an integer beyond 64 bits allocate, as the data model documents. A float is
/// rounded as a double and narrowed again, which is exact, because every float is a double. <c>fn:round-half-to-even</c> casts a float or
/// double to <c>xs:decimal</c> through its exact binary value, rounds, and casts back, as the specification states, so the result for
/// 0.125 rounded to two digits is 0.12 and for the float nearest to -0.05 rounded to one digit is -0.1.
/// </remarks>
internal static class NumericRounding
{
    private enum Mode
    {
        Ceiling = 0,
        Floor = 1,
        Round = 2,
    }

    /// <summary>Gets the absolute value of a number.</summary>
    /// <param name="item">The number.</param>
    /// <returns>The magnitude, in the type of the argument.</returns>
    /// <exception cref="XPathException">The item is not a number (XPTY0004).</exception>
    public static XdmItem Abs(in XdmItem item)
    {
        if (item.TryGetInteger(out var integer))
        {
            return integer >= 0 ? item
                : integer == long.MinValue ? XdmItem.FromBigInteger(-new BigInteger(integer))
                : XdmItem.FromInteger(-integer);
        }

        if (item.TryGetBigInteger(out var wide))
        {
            return wide.Sign >= 0 ? item : XdmItem.FromBigInteger(-wide);
        }

        if (item.TryGetDecimal(out var number))
        {
            return number.Sign >= 0 ? item : XdmItem.FromDecimal(number.Negate());
        }

        if (item.TryGetFloat(out var single))
        {
            return XdmItem.FromFloat(Math.Abs(single));
        }

        return item.TryGetDouble(out var floating)
            ? XdmItem.FromDouble(Math.Abs(floating))
            : throw XPathErrors.Exception(XPathErrorCodes.XPTY0004);
    }

    /// <summary>Gets the smallest integer that is not less than a number, in the type of the number.</summary>
    /// <param name="item">The number.</param>
    /// <returns>The ceiling.</returns>
    /// <exception cref="XPathException">The item is not a number (XPTY0004).</exception>
    public static XdmItem Ceiling(in XdmItem item) => ToIntegral(item, Mode.Ceiling);

    /// <summary>Gets the largest integer that is not greater than a number, in the type of the number.</summary>
    /// <param name="item">The number.</param>
    /// <returns>The floor.</returns>
    /// <exception cref="XPathException">The item is not a number (XPTY0004).</exception>
    public static XdmItem Floor(in XdmItem item) => ToIntegral(item, Mode.Floor);

    /// <summary>Rounds a number to the closest integer, a tie toward positive infinity: 2.5 gives 3 and -2.5 gives -2.</summary>
    /// <param name="item">The number.</param>
    /// <returns>The rounded number, in the type of the argument.</returns>
    /// <exception cref="XPathException">The item is not a number (XPTY0004).</exception>
    public static XdmItem Round(in XdmItem item) => ToIntegral(item, Mode.Round);

    /// <summary>Rounds a number to a precision, a tie to the even neighbor: 2.5 gives 2 and 3.5 gives 4.</summary>
    /// <param name="item">The number.</param>
    /// <param name="precision">The number of fraction digits kept; negative rounds to a power of ten.</param>
    /// <returns>The rounded number, in the type of the argument.</returns>
    /// <exception cref="XPathException">The item is not a number (XPTY0004), or the result is beyond the scale range of the decimal (FOAR0002).</exception>
    public static XdmItem RoundHalfToEven(in XdmItem item, long precision) =>
        RoundTo(in item, precision, halfEven: true);

    /// <summary>Rounds a number to a precision, a tie toward positive infinity (<c>fn:round</c> with a precision): 2.5 gives 3 and -2.5 gives -2, and 1.125 with a precision of 2 gives 1.13.</summary>
    /// <param name="item">The number.</param>
    /// <param name="precision">The number of fraction digits kept; negative rounds to a power of ten.</param>
    /// <returns>The rounded number, in the type of the argument.</returns>
    /// <exception cref="XPathException">The item is not a number (XPTY0004), or the result is beyond the scale range of the decimal (FOAR0002).</exception>
    public static XdmItem Round(in XdmItem item, long precision) => RoundTo(in item, precision, halfEven: false);

    private static XdmItem RoundTo(in XdmItem item, long precision, bool halfEven)
    {
        // A precision beyond the 32-bit range keeps or clears every digit of any value, so the clamp changes no result.
        var digits = (int)Math.Max(int.MinValue, Math.Min(int.MaxValue, precision));
        if (IsInteger(in item))
        {
            return digits >= 0 ? item : XdmItem.FromBigInteger(RoundInteger(in item, digits, halfEven));
        }

        if (item.TryGetDecimal(out var number))
        {
            return number.Scale <= digits ? item : XdmItem.FromDecimal(Quantize(number, digits, halfEven));
        }

        if (item.TryGetFloat(out var single))
        {
            return XdmItem.FromFloat((float)RoundBinary(single, digits, single: true, halfEven));
        }

        return item.TryGetDouble(out var floating)
            ? XdmItem.FromDouble(RoundBinary(floating, digits, single: false, halfEven))
            : throw XPathErrors.Exception(XPathErrorCodes.XPTY0004);
    }

    private static XdmItem ToIntegral(in XdmItem item, Mode mode)
    {
        // An integer is its own ceiling, floor, and rounding, at any width.
        if (IsInteger(in item))
        {
            return item;
        }

        if (item.TryGetDecimal(out var number))
        {
            return number.Scale <= 0 ? item : XdmItem.FromDecimal(ToIntegral(number, mode));
        }

        if (item.TryGetFloat(out var single))
        {
            return XdmItem.FromFloat((float)ToIntegral(single, mode));
        }

        return item.TryGetDouble(out var floating)
            ? XdmItem.FromDouble(ToIntegral(floating, mode))
            : throw XPathErrors.Exception(XPathErrorCodes.XPTY0004);
    }

    private static XsdDecimal ToIntegral(XsdDecimal number, Mode mode) =>
        mode switch
        {
            Mode.Ceiling => DecimalArithmetic.Ceiling(number),
            Mode.Floor => DecimalArithmetic.Floor(number),
            _ => DecimalArithmetic.Round(number),
        };

    private static double ToIntegral(double value, Mode mode)
    {
        double result;
        switch (mode)
        {
            case Mode.Ceiling:
                result = Math.Ceiling(value);
                break;
            case Mode.Floor:
                result = Math.Floor(value);
                break;
            default:
                // The fraction of a double is exact, so the tie test is exact: Math.Floor(value + 0.5) is wrong for 0.49999999999999994.
                // NaN and the infinities fall through unchanged, because the comparison with NaN is false.
                var floor = Math.Floor(value);
                result = value - floor >= 0.5 ? floor + 1 : floor;
                break;
        }

        // A zero result takes the sign of the argument: -0.3 gives -0, and a positive zero stays positive.
        return result == 0 && BitConverter.DoubleToInt64Bits(value) < 0
            ? BitConverter.Int64BitsToDouble(long.MinValue)
            : result;
    }

    private static double RoundBinary(double value, int digits, bool single, bool halfEven)
    {
        // NaN, the infinities, and the zeros are their own result.
        if (double.IsNaN(value) || double.IsInfinity(value) || value == 0)
        {
            return value;
        }

        var exact = DecimalArithmetic.FromDouble(value).Value;
        var rounded = exact.Scale <= digits ? exact : Quantize(exact, digits, halfEven);
        var result = single ? NumericPromotion.DecimalToNearestSingle(rounded) : DecimalArithmetic.ToDouble(rounded);
        return result == 0 && value < 0 ? BitConverter.Int64BitsToDouble(long.MinValue) : result;
    }

    private static BigInteger RoundInteger(in XdmItem item, int digits, bool halfEven)
    {
        _ = item.TryGetBigInteger(out var value);
        var rounded = Quantize(new XsdDecimal(value, 0), digits, halfEven);
        return DecimalArithmetic.ToBigInteger(rounded).Value;
    }

    // An integer or a type derived from it, inline or beyond 64 bits (an item of the decimal family that carries the type).
    private static bool IsInteger(in XdmItem item) => item.TryGetBigInteger(out _);

    private static XsdDecimal Quantize(XsdDecimal number, int digits, bool halfEven)
    {
        try
        {
            return halfEven
                ? DecimalArithmetic.RoundHalfToEven(number, digits)
                : DecimalArithmetic.Round(number, digits);
        }
        catch (OverflowException)
        {
            // The decimal reports a scale beyond the 32-bit range with an OverflowException.
            throw XPathErrors.Exception(XPathErrorCodes.FOAR0002);
        }
    }
}
