namespace NetEvolve.Schemas.XPath.Formatting.Numbers;

using System;
using System.Globalization;
using System.Numerics;
using NetEvolve.Schemas.Xml.Datatypes.Floating;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Functions.Strings;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The formatting phase of <c>format-number</c> (XSLT 2.0, 16.4.4; Functions and Operators 3.1, 4.7.5): it writes the formatted number into a
/// <see cref="TextBuilder"/>, whose buffer is the caller's stack scratch space first and a pooled array after that.
/// </summary>
/// <remarks>
/// <para>
/// The digits of a number are exact for <c>xs:decimal</c> and <c>xs:integer</c> at any width, and the shortest decimal that round-trips for
/// <c>xs:float</c> and <c>xs:double</c>; they are held in one digit string, which is the only allocation besides the result. A percent or
/// per-mille sign multiplies a decimal or integer exactly (the decimal point moves) and a float or double in its own type, so an overflow
/// is infinity and not an error. The mantissa of an exponent is scaled on the exact digits, not by a division.
/// </para>
/// <para>
/// Limitations: NaN is the value of the NaN property of the decimal format, not the pattern separator property that the text of 4.7.5 names
/// (an editorial slip: the same sentence calls it the NaN symbol in XSLT 2.0). A mantissa that rounds up to the next power of ten is not
/// scaled again (9.96 with <c>0.0e0</c> gives <c>10.0e0</c>), as the conformance cases expect.
/// </para>
/// </remarks>
internal static class NumberFormatter
{
    /// <summary>Formats a number.</summary>
    /// <param name="item">The number: <c>xs:decimal</c>, <c>xs:integer</c> (also beyond 64 bits), <c>xs:float</c>, or <c>xs:double</c>.</param>
    /// <param name="picture">The analyzed picture.</param>
    /// <param name="format">The decimal format.</param>
    /// <param name="output">The text builder that receives the result.</param>
    /// <exception cref="ArgumentNullException"><paramref name="picture"/> or <paramref name="format"/> is <see langword="null"/>.</exception>
    /// <exception cref="XPathException">The item is not a number (XPTY0004), or the result is longer than the string limit (XP0006).</exception>
    public static void Format(in XdmItem item, NumberPicture picture, DecimalFormatModel format, ref TextBuilder output)
    {
        ArgumentNullException.ThrowIfNull(picture);
        ArgumentNullException.ThrowIfNull(format);

        if (item.TryGetFloat(out var single))
        {
            FormatFloating(single, true, picture, format, ref output);
        }
        else if (item.TryGetDouble(out var value))
        {
            FormatFloating(value, false, picture, format, ref output);
        }
        else if (item.TryGetBigInteger(out var integer))
        {
            FormatExact(integer, 0, picture, format, ref output);
        }
        else if (item.TryGetDecimal(out var number))
        {
            FormatExact(number.Mantissa, number.Scale, picture, format, ref output);
        }
        else
        {
            throw XPathErrors.Exception(XPathErrorCodes.XPTY0004);
        }
    }

    private static void FormatFloating(
        double value,
        bool single,
        NumberPicture picture,
        DecimalFormatModel format,
        ref TextBuilder output
    )
    {
        if (double.IsNaN(value))
        {
            output.Append(format.NaN);
            return;
        }

        // Negative zero takes the negative sub-picture.
        var negative = BitConverter.DoubleToInt64Bits(value) < 0;
        var sub = negative ? picture.Negative : picture.Positive;

        // The adjusted number keeps the type of the input: a float product is exact in a double and narrowed once, and an overflow is infinity.
        var adjusted = value * sub.Multiplier;
        if (single)
        {
            adjusted = (float)adjusted;
        }

        if (double.IsInfinity(adjusted))
        {
            output.Append(sub.Prefix);
            output.Append(format.Infinity);
            output.Append(sub.Suffix);
            return;
        }

        var digits =
            adjusted == 0
                ? new NumberDigits(string.Empty, 0)
                : new NumberDigits(
                    BinaryFloat.ShortestDigits(Math.Abs(adjusted), single, out var exponent),
                    exponent + 1
                );
        Write(digits, sub, format, ref output);
    }

    private static void FormatExact(
        BigInteger mantissa,
        int scale,
        NumberPicture picture,
        DecimalFormatModel format,
        ref TextBuilder output
    )
    {
        var sub = mantissa.Sign < 0 ? picture.Negative : picture.Positive;
        var digits = new NumberDigits(string.Empty, 0);
        if (!mantissa.IsZero)
        {
            var text = BigInteger.Abs(mantissa).ToString(CultureInfo.InvariantCulture);
            var trimmed = text.TrimEnd('0');

            // A percent or per-mille sign multiplies by a power of ten: the decimal point moves, the digits stay.
            var shift =
                sub.Multiplier == 100 ? 2
                : sub.Multiplier == 1000 ? 3
                : 0;
            digits = new NumberDigits(trimmed, checked(text.Length - scale + shift));
        }

        Write(digits, sub, format, ref output);
    }

    private static void Write(NumberDigits number, SubPicture sub, DecimalFormatModel format, ref TextBuilder output)
    {
        var exponent = 0;
        if (sub.HasExponent && number.Digits.Length > 0)
        {
            // The mantissa lies between 10^(N-1) and 10^N for the scaling factor N: the point moves to N places from the left.
            exponent = number.Point - sub.ScalingFactor;
            number = new NumberDigits(number.Digits, sub.ScalingFactor);
        }

        // A mantissa that rounds up to 10^N stays so: 0.99999999 with 0.0e0 is 10.0e-1 (the QT3 cases numberformat301 to 304).
        number = number.Round(sub.MaximumFractionSize);
        output.Append(sub.Prefix);
        WriteInteger(number, sub, format, ref output);
        WriteFraction(number, sub, format, ref output);
        if (sub.HasExponent)
        {
            WriteExponent(exponent, sub.MinimumExponentSize, format, ref output);
        }

        output.Append(sub.Suffix);
    }

    // The integer part: at least the minimum size, with the grouping separators after the digits that have a number of digits to their right.
    private static void WriteInteger(
        NumberDigits number,
        SubPicture sub,
        DecimalFormatModel format,
        ref TextBuilder output
    )
    {
        var integerDigits = Math.Max(number.Point, 0);
        var integerSize = Math.Max(integerDigits, sub.MinimumIntegerSize);
        var padding = integerSize - integerDigits;
        for (var i = 0; i < integerSize; i++)
        {
            output.Append((char)(format.ZeroDigit + (i < padding ? 0 : DigitAt(number.Digits, i - padding))));
            var right = integerSize - 1 - i;
            if (right > 0)
            {
                for (var n = sub.GroupingSeparatorsAfter(right); n > 0; n--)
                {
                    output.Append(format.GroupingSeparator);
                }
            }
        }
    }

    // The fraction part: the digits behind the point, at least the minimum size; the separator is written when a digit follows it.
    private static void WriteFraction(
        NumberDigits number,
        SubPicture sub,
        DecimalFormatModel format,
        ref TextBuilder output
    )
    {
        var fractionSize = Math.Max(Math.Max(number.Digits.Length - number.Point, 0), sub.MinimumFractionSize);
        if (fractionSize > 0)
        {
            output.Append(format.DecimalSeparator);
        }

        for (var j = 0; j < fractionSize; j++)
        {
            if (j > 0)
            {
                for (var n = sub.GroupingSeparatorsBefore(j); n > 0; n--)
                {
                    output.Append(format.GroupingSeparator);
                }
            }

            output.Append((char)(format.ZeroDigit + DigitAt(number.Digits, number.Point + j)));
        }
    }

    // The digit at an index of the digit string; zero before the first digit and behind the last one.
    private static int DigitAt(string digits, int index) =>
        index >= 0 && index < digits.Length ? digits[index] - '0' : 0;

    private static void WriteExponent(int exponent, int minimumSize, DecimalFormatModel format, ref TextBuilder output)
    {
        output.Append(format.ExponentSeparator);
        if (exponent < 0)
        {
            output.Append(format.MinusSign);
        }

        // The magnitude as a long: the negation of the smallest int does not fit an int.
        var magnitude = Math.Abs((long)exponent);
        Span<char> text = stackalloc char[20];
        var length = 0;
        do
        {
            text[length++] = (char)(format.ZeroDigit + (int)(magnitude % 10));
            magnitude /= 10;
        } while (magnitude > 0);

        for (var padding = minimumSize - length; padding > 0; padding--)
        {
            output.Append(format.ZeroDigit);
        }

        while (length > 0)
        {
            output.Append(text[--length]);
        }
    }
}
