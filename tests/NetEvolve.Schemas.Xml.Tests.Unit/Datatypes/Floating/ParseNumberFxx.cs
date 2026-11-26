namespace NetEvolve.Schemas.Xml.Tests.Unit.Datatypes.Floating;

using System;
using System.Globalization;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Floating;

/// <summary>
/// Checks one line of the parse-number-fxx test data: the hexadecimal bit patterns of the expected float16, float32 and float64
/// followed by the input string at the column offsets 0, 5, 14 and 31. The float16 column is not used.
/// </summary>
internal static class ParseNumberFxx
{
    private const int Float32Start = 5;
    private const int Float64Start = 14;
    private const int InputStart = 31;

    /// <summary>Compares the parsers with the expected bits of one line.</summary>
    /// <param name="line">The data line.</param>
    /// <param name="version">The XSD version of the lexical rules.</param>
    /// <returns>The outcome; inputs outside the <c>xs:double</c> lexical space (hex floats, <c>inf</c>, <c>nan</c>) must be rejected.</returns>
    internal static ParseNumberFxxOutcome Check(ReadOnlySpan<char> line, XsdVersion version)
    {
        if (
            line.Length <= InputStart
            || line[Float32Start - 1] != ' '
            || line[Float64Start - 1] != ' '
            || line[InputStart - 1] != ' '
            || !uint.TryParse(
                line.Slice(Float32Start, 8),
                NumberStyles.AllowHexSpecifier,
                CultureInfo.InvariantCulture,
                out var expected32
            )
            || !ulong.TryParse(
                line.Slice(Float64Start, 16),
                NumberStyles.AllowHexSpecifier,
                CultureInfo.InvariantCulture,
                out var expected64
            )
        )
        {
            return ParseNumberFxxOutcome.Malformed;
        }

        var input = line.Slice(InputStart);
        var d = XsdFloatingPoint.ParseDouble(input, version);
        var f = XsdFloatingPoint.ParseSingle(input, version);
        if (!IsLexical(input))
        {
            return d.Success || f.Success ? ParseNumberFxxOutcome.Mismatch : ParseNumberFxxOutcome.Rejected;
        }

        return
            d.Success
            && f.Success
            && BitConverter.DoubleToInt64Bits(d.Value) == unchecked((long)expected64)
            && BitConverter.SingleToInt32Bits(f.Value) == unchecked((int)expected32)
            ? ParseNumberFxxOutcome.Passed
            : ParseNumberFxxOutcome.Mismatch;
    }

    /// <summary>Tests the <c>xs:decimal</c> mantissa with an optional integer exponent, the lexical space of finite <c>xs:double</c> and <c>xs:float</c> values.</summary>
    internal static bool IsLexical(ReadOnlySpan<char> input)
    {
        var i = 0;
        if (i < input.Length && input[i] is '+' or '-')
        {
            i++;
        }

        var digits = Digits(input, ref i);
        if (i < input.Length && input[i] == '.')
        {
            i++;
            digits += Digits(input, ref i);
        }

        if (digits == 0)
        {
            return false;
        }

        if (i == input.Length)
        {
            return true;
        }

        if (input[i] is not ('e' or 'E'))
        {
            return false;
        }

        i++;
        if (i < input.Length && input[i] is '+' or '-')
        {
            i++;
        }

        return Digits(input, ref i) > 0 && i == input.Length;
    }

    private static int Digits(ReadOnlySpan<char> input, ref int i)
    {
        var start = i;
        while (i < input.Length && input[i] is >= '0' and <= '9')
        {
            i++;
        }

        return i - start;
    }
}
