namespace NetEvolve.Schemas.Xml.Datatypes.Floating;

using System;
using System.Globalization;
using System.Numerics;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Decimals;

/// <summary>Framework-independent parsing and canonical output of <c>xs:float</c> and <c>xs:double</c> (XSD 1.1 Part 2, sections 3.3.4 and 3.3.5).</summary>
/// <remarks>
/// Parsing is correctly rounded (ties to even) and overflow becomes infinity. A mantissa that fits 64 bits with a decimal exponent of at
/// most 19 in magnitude is rounded without allocating; every other form uses <see cref="BigInteger"/> arithmetic. Limitations: the mantissa is scanned by
/// <see cref="XsdDecimal"/>, so it is capped at <see cref="XsdDecimal.DefaultMaxDigits"/> digits (DT0010); an exponent beyond
/// 1,000,000 saturates, which cannot change the result because it lies far outside the range of both types. The canonical form is the
/// shortest decimal string that round-trips, and of the shortest candidates the one nearest to the value.
/// </remarks>
public static class XsdFloatingPoint
{
    private const int ExponentCap = 1_000_000;

    /// <summary>Parses an <c>xs:double</c> lexical form.</summary>
    /// <param name="lexical">The whitespace-normalized lexical form.</param>
    /// <param name="version">The XSD version whose lexical rules apply.</param>
    /// <returns>The value or the error with its offset.</returns>
    public static ParseResult<double> ParseDouble(ReadOnlySpan<char> lexical, XsdVersion version)
    {
        var error = Parse(lexical, version, false, out var value, out var offset);
        return error == DatatypeError.None ? new ParseResult<double>(value) : new ParseResult<double>(error, offset);
    }

    /// <summary>Parses an <c>xs:float</c> lexical form.</summary>
    /// <param name="lexical">The whitespace-normalized lexical form.</param>
    /// <param name="version">The XSD version whose lexical rules apply.</param>
    /// <returns>The value or the error with its offset.</returns>
    public static ParseResult<float> ParseSingle(ReadOnlySpan<char> lexical, XsdVersion version)
    {
        var error = Parse(lexical, version, true, out var value, out var offset);
        return error == DatatypeError.None
            ? new ParseResult<float>((float)value)
            : new ParseResult<float>(error, offset);
    }

    /// <summary>Gets the canonical form of a double.</summary>
    /// <param name="value">The value.</param>
    /// <returns>The canonical form.</returns>
    public static string ToCanonicalString(double value) => Format(value, false);

    /// <summary>Gets the canonical form of a float.</summary>
    /// <param name="value">The value.</param>
    /// <returns>The canonical form.</returns>
    public static string ToCanonicalString(float value) => Format(value, true);

    // For a float the value is the exactly equal double.
    internal static DatatypeError Parse(
        ReadOnlySpan<char> lexical,
        XsdVersion version,
        bool single,
        out double value,
        out int offset
    )
    {
        value = 0;
        offset = 0;
        if (lexical.SequenceEqual("NaN".AsSpan()))
        {
            value = double.NaN;
            return DatatypeError.None;
        }

        if (
            lexical.SequenceEqual("INF".AsSpan())
            || (version == XsdVersion.V11 && lexical.SequenceEqual("+INF".AsSpan()))
        )
        {
            value = double.PositiveInfinity;
            return DatatypeError.None;
        }

        if (lexical.SequenceEqual("-INF".AsSpan()))
        {
            value = double.NegativeInfinity;
            return DatatypeError.None;
        }

        if (lexical.SequenceEqual("+INF".AsSpan()))
        {
            return DatatypeError.InvalidLexicalForm;
        }

        var split = lexical.IndexOfAny('e', 'E');
        var mantissa = XsdDecimal.Parse(split < 0 ? lexical : lexical.Slice(0, split));
        if (!mantissa.Success)
        {
            offset = mantissa.ErrorOffset;
            return mantissa.Error;
        }

        var exponent = 0;
        if (split >= 0)
        {
            var error = ParseExponent(lexical, split + 1, out exponent, out offset);
            if (error != DatatypeError.None)
            {
                return error;
            }
        }

        var magnitude = Magnitude(mantissa.Value, exponent, single);
        value = lexical.Length > 0 && lexical[0] == '-' ? -magnitude : magnitude;
        return DatatypeError.None;
    }

    private static double Magnitude(XsdDecimal mantissa, int exponent, bool single)
    {
        if (mantissa.Sign == 0)
        {
            return 0.0;
        }

        var scaleExponent = exponent - mantissa.Scale;
        return
            mantissa.TryGetInlineMagnitude(out var inline)
            && BinaryFloat.TryRound(inline, scaleExponent, single, out var fast)
            ? fast
            : BinaryFloat.Round(BigInteger.Abs(mantissa.Mantissa), scaleExponent, single);
    }

    private static DatatypeError ParseExponent(ReadOnlySpan<char> lexical, int start, out int exponent, out int offset)
    {
        exponent = 0;
        offset = lexical.Length;
        var negative = start < lexical.Length && lexical[start] == '-';
        var i = start < lexical.Length && (negative || lexical[start] == '+') ? start + 1 : start;
        if (i == lexical.Length)
        {
            return DatatypeError.InvalidLexicalForm;
        }

        for (; i < lexical.Length; i++)
        {
            var c = lexical[i];
            if (c is < '0' or > '9')
            {
                offset = i;
                return DatatypeError.InvalidCharacter;
            }

            exponent = Math.Min((exponent * 10) + (c - '0'), ExponentCap);
        }

        exponent = negative ? -exponent : exponent;
        return DatatypeError.None;
    }

    private static string Format(double value, bool single)
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

        var sign = negative ? "-" : string.Empty;
        if (value == 0)
        {
            return sign + "0.0E0";
        }

        var digits = BinaryFloat.ShortestDigits(Math.Abs(value), single, out var exponent);
        var fraction = digits.Length > 1 ? digits.Substring(1) : "0";
        return sign + digits[0] + "." + fraction + "E" + exponent.ToString(CultureInfo.InvariantCulture);
    }
}
