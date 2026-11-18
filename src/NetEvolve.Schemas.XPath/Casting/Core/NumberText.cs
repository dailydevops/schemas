namespace NetEvolve.Schemas.XPath.Casting.Core;

using System;
using System.Globalization;
using NetEvolve.Schemas.Xml.Datatypes.Floating;
using NetEvolve.Schemas.Xml.Datatypes.Strings;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The string value of an atomic value of the seven types (Functions and Operators, 17.1.2): a double or float whose magnitude is at
/// least 1e-6 and below 1e6 uses plain notation, any other one the scientific notation of the canonical form with at least one fraction
/// digit, and the special values are <c>NaN</c>, <c>INF</c>, <c>-INF</c>, and <c>-0</c>; a decimal has no trailing zeros and no point
/// for an integer value.
/// </summary>
/// <remarks>
/// The digits of a double or float are the shortest that round-trip, taken from the canonical form of the datatype layer. Limitation:
/// the plain range of a float is judged on the float value, so <c>xs:float('0.000001')</c> uses plain notation although the float
/// lies a hair below 1e-6 as a double.
/// </remarks>
internal static class NumberText
{
    /// <summary>Gets the string value of an atomic item.</summary>
    /// <param name="item">The item.</param>
    /// <param name="text">The string value; <see langword="null"/> when the item is not a string, untyped, numeric, or boolean value.</param>
    /// <returns><see langword="true"/> when the item has one of the seven types.</returns>
    public static bool TryFormat(in XdmItem item, out string? text)
    {
        if (item.TryGetString(out text) || item.TryGetUntypedAtomic(out text))
        {
            return true;
        }

        if (item.TryGetBoolean(out var boolean))
        {
            text = XsdBoolean.ToCanonical(boolean);
        }
        else if (item.TryGetInteger(out var integer))
        {
            text = integer.ToString(CultureInfo.InvariantCulture);
        }
        else if (item.TryGetDecimal(out var number))
        {
            text = number.ToString();
        }
        else if (item.TryGetDouble(out var wide))
        {
            text = FormatFloating(wide, single: false);
        }
        else if (item.TryGetFloat(out var narrow))
        {
            text = FormatFloating(narrow, single: true);
        }
        else
        {
            text = null;
        }

        return text is not null;
    }

    /// <summary>Formats a double or a float.</summary>
    /// <param name="value">The value; for a float, the exactly equal double.</param>
    /// <param name="single">Whether the value is a float, which has fewer digits.</param>
    /// <returns>The string value.</returns>
    public static string FormatFloating(double value, bool single)
    {
        if (double.IsNaN(value))
        {
            return "NaN";
        }

        var negative = BitConverter.DoubleToInt64Bits(value) < 0;
        if (double.IsInfinity(value))
        {
            return negative ? "-INF" : "INF";
        }

        if (value == 0)
        {
            return negative ? "-0" : "0";
        }

        var canonical = single
            ? XsdFloatingPoint.ToCanonicalString((float)value)
            : XsdFloatingPoint.ToCanonicalString(value);
        var magnitude = Math.Abs(value);
        var plain = single ? (float)magnitude is >= 1e-6f and < 1e6f : magnitude is >= 1e-6 and < 1e6;
        return plain ? ToPlain(canonical) : canonical;
    }

    // The canonical form is [-]d.dddE[-]n with the shortest digits; the plain form moves the point by the exponent.
    private static string ToPlain(string canonical)
    {
        var span = canonical.AsSpan();
        var negative = span[0] == '-';
        if (negative)
        {
            span = span.Slice(1);
        }

        var e = span.IndexOf('E');
        var exponent = 0;
        var exponentNegative = span[e + 1] == '-';
        for (var i = exponentNegative ? e + 2 : e + 1; i < span.Length; i++)
        {
            exponent = (exponent * 10) + (span[i] - '0');
        }

        exponent = exponentNegative ? -exponent : exponent;

        // The digits without the point; the padding zero of a single digit and any trailing zero are dropped. The shortest digits of a
        // double are at most 17, and the plain range keeps the exponent between -6 and 5, so the buffers below always suffice.
        Span<char> digits = stackalloc char[24];
        var count = 0;
        for (var i = 0; i < e; i++)
        {
            if (span[i] != '.')
            {
                digits[count++] = span[i];
            }
        }

        while (count > 1 && digits[count - 1] == '0')
        {
            count--;
        }

        Span<char> result = stackalloc char[48];
        var length = 0;
        if (negative)
        {
            result[length++] = '-';
        }

        length += WritePlain(digits.Slice(0, count), exponent, result.Slice(length));
        return result.Slice(0, length).ToString();
    }

    // The point moves by the exponent: leading zeros for a negative one, trailing zeros when the digits end before the point.
    private static int WritePlain(ReadOnlySpan<char> digits, int exponent, Span<char> result)
    {
        var length = 0;
        if (exponent < 0)
        {
            result[length++] = '0';
            result[length++] = '.';
            for (var i = -exponent - 1; i > 0; i--)
            {
                result[length++] = '0';
            }

            digits.CopyTo(result.Slice(length));
            return length + digits.Length;
        }

        var whole = Math.Min(digits.Length, exponent + 1);
        digits.Slice(0, whole).CopyTo(result);
        length = whole;
        for (var i = exponent + 1 - whole; i > 0; i--)
        {
            result[length++] = '0';
        }

        if (whole < digits.Length)
        {
            result[length++] = '.';
            digits.Slice(whole).CopyTo(result.Slice(length));
            length += digits.Length - whole;
        }

        return length;
    }
}
