namespace NetEvolve.Schemas.XPath.Functions.Nodes;

using System;
using System.Globalization;
using NetEvolve.Schemas.Xml.Datatypes.Duration;
using NetEvolve.Schemas.Xml.Datatypes.Floating;
using NetEvolve.Schemas.Xml.Datatypes.Strings;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The string value of an atomic value (Functions and Operators 1.0, 17.1.2): the canonical lexical form of the datatype, except for
/// <c>xs:double</c> and <c>xs:float</c>, whose magnitude from 1e-6 up to 1e6 (exclusive) is written in plain notation, any other one
/// in the scientific notation of the canonical form, and whose special values are <c>NaN</c>, <c>INF</c>, <c>-INF</c>, and <c>-0</c>.
/// </summary>
/// <remarks>
/// Limitations: the casting table of XP-22 (#265) owns the cast to <c>xs:string</c> and has not landed, so this class carries its own
/// rule for the one conversion that <c>fn:string</c> needs and is to be replaced by the casting table (the digits are the shortest
/// ones that round-trip, taken from the canonical form of the datatype layer); an item without a value type yet (<c>xs:QName</c>,
/// the binary and the Gregorian types, #545, #301) raises XPTY0004.
/// </remarks>
internal static class AtomicString
{
    /// <summary>Gets the string value of an atomic item.</summary>
    /// <param name="item">The item, not a node.</param>
    /// <returns>The string value.</returns>
    /// <exception cref="XPathException">The item has no value type yet (XPTY0004).</exception>
    public static string Of(in XdmItem item)
    {
        if (item.TryGetString(out var text) || item.TryGetUntypedAtomic(out text) || item.TryGetAnyUri(out text))
        {
            return text!;
        }

        if (item.TryGetBoolean(out var boolean))
        {
            return XsdBoolean.ToCanonical(boolean);
        }

        if (item.TryGetInteger(out var integer))
        {
            return integer.ToString(CultureInfo.InvariantCulture);
        }

        if (item.TryGetDecimal(out var number))
        {
            return number.ToString();
        }

        if (item.TryGetDouble(out var wide))
        {
            return Floating(wide, single: false);
        }

        if (item.TryGetFloat(out var narrow))
        {
            return Floating(narrow, single: true);
        }

        if (item.TryGetDateTime(out var temporal))
        {
            return temporal.ToString();
        }

        if (item.TryGetDuration(out var duration))
        {
            return duration.ToString(
                item.Kind switch
                {
                    XdmItemKind.YearMonthDuration => DurationKind.YearMonth,
                    XdmItemKind.DayTimeDuration => DurationKind.DayTime,
                    _ => DurationKind.Duration,
                }
            );
        }

        throw XPathErrors.Exception(XPathErrorCodes.XPTY0004);
    }

    private static string Floating(double value, bool single)
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
        return magnitude is >= 1e-6 and < 1e6 ? Plain(canonical) : canonical;
    }

    // The canonical form is [-]d.dddE[-]n with the shortest digits; the plain form moves the point by the exponent.
    private static string Plain(string canonical)
    {
        var e = canonical.AsSpan().IndexOf('E');
        var negative = canonical[0] == '-';

        // The digits without the point, the padding zero of a single digit and any trailing zero dropped; at most 17 are shortest.
        Span<char> digits = stackalloc char[24];
        var count = 0;
        for (var i = negative ? 1 : 0; i < e; i++)
        {
            if (canonical[i] != '.')
            {
                digits[count++] = canonical[i];
            }
        }

        while (count > 1 && digits[count - 1] == '0')
        {
            count--;
        }

        var exponentNegative = canonical[e + 1] == '-';
        var exponent = 0;
        for (var i = exponentNegative ? e + 2 : e + 1; i < canonical.Length; i++)
        {
            exponent = (exponent * 10) + (canonical[i] - '0');
        }

        return Layout(negative, digits.Slice(0, count), exponentNegative ? -exponent : exponent);
    }

    private static string Layout(bool negative, ReadOnlySpan<char> digits, int exponent)
    {
        Span<char> result = stackalloc char[48];
        var length = 0;
        if (negative)
        {
            result[length++] = '-';
        }

        if (exponent < 0)
        {
            result[length++] = '0';
            result[length++] = '.';
            for (var i = 1; i < -exponent; i++)
            {
                result[length++] = '0';
            }

            digits.CopyTo(result.Slice(length));
            length += digits.Length;
        }
        else
        {
            for (var i = 0; i <= exponent; i++)
            {
                result[length++] = i < digits.Length ? digits[i] : '0';
            }

            if (digits.Length > exponent + 1)
            {
                result[length++] = '.';
                digits.Slice(exponent + 1).CopyTo(result.Slice(length));
                length += digits.Length - exponent - 1;
            }
        }

        return result.Slice(0, length).ToString();
    }
}
