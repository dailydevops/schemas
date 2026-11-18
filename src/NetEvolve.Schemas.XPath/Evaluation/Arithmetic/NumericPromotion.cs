namespace NetEvolve.Schemas.XPath.Evaluation.Arithmetic;

using System.Numerics;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Decimals;
using NetEvolve.Schemas.Xml.Datatypes.Decimals.Arithmetic;
using NetEvolve.Schemas.Xml.Datatypes.Floating;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The numeric type promotion of XPath 2.0, B.1, in one place for the arithmetic and the function conversion: <c>xs:float</c> to
/// <c>xs:double</c>, and <c>xs:decimal</c> and <c>xs:integer</c> (inline or beyond 64 bits, an item of the decimal family) to
/// <c>xs:float</c> or <c>xs:double</c>. A value is rounded once: a decimal or a wide integer is promoted to <c>xs:float</c> through
/// its exact lexical form and not through a double. A decimal beyond the digit cap of the datatype layer is rounded from its exact value
/// as well, so a promotion has no failure: it yields the nearest float, which is an infinity or a zero outside the float range.
/// </summary>
internal static class NumericPromotion
{
    /// <summary>Promotes a numeric item to <c>xs:double</c>.</summary>
    /// <param name="item">The item.</param>
    /// <param name="value">The value, zero when the item is not numeric.</param>
    /// <returns><see langword="true"/> when the item is a double, float, decimal, or integer.</returns>
    internal static bool TryToDouble(in XdmItem item, out double value)
    {
        if (item.TryGetDouble(out value))
        {
            return true;
        }

        if (item.TryGetFloat(out var single))
        {
            value = single;
            return true;
        }

        if (item.TryGetInteger(out var integer))
        {
            value = integer;
            return true;
        }

        // The decimal accessor reads an integer beyond 64 bits as well.
        if (item.TryGetDecimal(out var number))
        {
            value = DecimalArithmetic.ToDouble(number);
            return true;
        }

        return false;
    }

    /// <summary>Rounds the exact value of a decimal once to <c>xs:float</c>, through its lexical form and not through a double: the one step that the promotion and the cast of a decimal to a float share.</summary>
    /// <param name="number">The decimal.</param>
    /// <returns>The float; the result is a failure when the lexical form is beyond the digit cap of the datatype layer, and its value is then zero.</returns>
    internal static ParseResult<float> DecimalToSingle(XsdDecimal number) =>
        XsdFloatingPoint.ParseSingle(number.ToString(), XsdVersion.V10);

    /// <summary>Promotes a decimal to the nearest <c>xs:float</c>: promotion is not a cast and has no failure status.</summary>
    /// <param name="number">The decimal.</param>
    /// <returns>The float, rounded once from the exact value; an infinity or a signed zero when the value is outside the float range.</returns>
    /// <remarks>
    /// The common case is the step of the cast. A lexical form beyond the digit cap is rounded from the mantissa and the scale instead,
    /// without a lexical form and so without the cap.
    /// </remarks>
    internal static float DecimalToNearestSingle(XsdDecimal number)
    {
        var parsed = DecimalToSingle(number);
        if (parsed.Success)
        {
            return parsed.Value;
        }

        var magnitude = (float)
            BinaryFloat.Round(
                BigInteger.Abs(number.Mantissa),
                number.Scale == int.MinValue ? int.MaxValue : -number.Scale,
                single: true
            );
        return number.Sign < 0 ? -magnitude : magnitude;
    }

    /// <summary>Promotes a numeric item that is not a double to <c>xs:float</c>.</summary>
    /// <param name="item">The item.</param>
    /// <param name="value">The value, zero when the item is not a float, decimal, or integer.</param>
    /// <returns><see langword="true"/> when the item is a float, decimal, or integer; a double is not promoted down.</returns>
    internal static bool TryToSingle(in XdmItem item, out float value)
    {
        if (item.TryGetFloat(out value))
        {
            return true;
        }

        if (item.TryGetInteger(out var integer))
        {
            value = integer;
            return true;
        }

        if (item.TryGetDecimal(out var number))
        {
            value = DecimalToNearestSingle(number);
            return true;
        }

        return false;
    }
}
