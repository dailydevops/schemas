namespace NetEvolve.Schemas.XPath.Evaluation.Arithmetic;

using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Decimals.Arithmetic;
using NetEvolve.Schemas.Xml.Datatypes.Floating;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The numeric type promotion of XPath 2.0, B.1, in one place for the arithmetic and the function conversion: <c>xs:float</c> to
/// <c>xs:double</c>, and <c>xs:decimal</c> and <c>xs:integer</c> (inline or beyond 64 bits, an item of the decimal family) to
/// <c>xs:float</c> or <c>xs:double</c>. A value is rounded once: a decimal or a wide integer is promoted to <c>xs:float</c> through
/// its exact lexical form and not through a double.
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
            value = XsdFloatingPoint.ParseSingle(number.ToString(), XsdVersion.V10).Value;
            return true;
        }

        return false;
    }
}
