namespace NetEvolve.Schemas.Xml.Datatypes.Numeric;

using System;
using NetEvolve.Schemas.Xml.Datatypes.Decimals;

/// <summary>Canonical forms of <c>xs:decimal</c> and <c>xs:integer</c> values (XSD 1.1 Part 2, sections 3.3.3 and 3.4.13).</summary>
public static class XsdNumeric
{
    /// <summary>Gets the canonical form of a decimal: always a decimal point and at least one fraction digit, no plus sign, no superfluous zeros.</summary>
    /// <param name="value">The value.</param>
    /// <returns>The canonical form.</returns>
    public static string ToCanonicalString(XsdDecimal value)
    {
        var text = value.ToString();
        return text.AsSpan().IndexOf('.') < 0 ? text + ".0" : text;
    }

    /// <summary>Gets the canonical form of an integer: no decimal point, no plus sign, no leading zeros, and <c>0</c> for zero.</summary>
    /// <param name="value">The value.</param>
    /// <returns>The canonical form.</returns>
    public static string ToCanonicalString(XsdInteger value) => value.ToString();
}
