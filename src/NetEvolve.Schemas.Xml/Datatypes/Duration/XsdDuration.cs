namespace NetEvolve.Schemas.Xml.Datatypes.Duration;

using System;
using System.Globalization;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Decimals;

/// <summary>
/// The value of <c>duration</c>, <c>yearMonthDuration</c>, and <c>dayTimeDuration</c> (XSD 1.1 Part 2, section 3.3.6): a month
/// count and an exact decimal second count that always share one sign.
/// </summary>
/// <remarks>
/// Limitation: each integer component of a lexical form is capped at 17 digits (<see cref="DatatypeError.YearOrComponentCap"/>),
/// so the month count fits a <see cref="long"/>. The partial order lives in the ordering namespace (DT-54).
/// </remarks>
[StructLayout(LayoutKind.Auto)]
public readonly struct XsdDuration : IEquatable<XsdDuration>
{
    private const int SecondsPerMinute = 60;
    private const int SecondsPerHour = 3600;
    private const int SecondsPerDay = 86400;

    internal XsdDuration(long months, XsdDecimal seconds)
    {
        Months = months;
        Seconds = seconds;
    }

    /// <summary>Gets the signed number of months: twelve per year.</summary>
    public long Months { get; }

    /// <summary>Gets the signed number of seconds: 86400 per day, 3600 per hour, 60 per minute, plus the fraction.</summary>
    public XsdDecimal Seconds { get; }

    /// <summary>Parses a duration without whitespace handling.</summary>
    /// <param name="lexical">The lexical form.</param>
    /// <param name="kind">The lexical space to accept.</param>
    /// <returns>The value, or the error with its offset.</returns>
    public static ParseResult<XsdDuration> Parse(ReadOnlySpan<char> lexical, DurationKind kind = DurationKind.Duration)
    {
        var error = DurationLexical.Scan(lexical, kind, out var parts, out var offset);
        return error == DatatypeError.None
            ? new ParseResult<XsdDuration>(Map(lexical, parts))
            : new ParseResult<XsdDuration>(error, offset);
    }

    /// <summary>Maps a form that passed the lexical scan.</summary>
    internal static XsdDuration Map(ReadOnlySpan<char> lexical, DurationLexical.Parts p)
    {
        var months = (p.Years * 12) + p.Months;
        var mantissa =
            (new BigInteger(p.Days) * SecondsPerDay)
            + (new BigInteger(p.Hours) * SecondsPerHour)
            + (new BigInteger(p.Minutes) * SecondsPerMinute)
            + p.Seconds;
        var scale = p.FractionLength;
        if (scale > 0)
        {
            mantissa = (mantissa * BigInteger.Pow(10, scale)) + Fraction(lexical.Slice(p.FractionStart, scale));
        }

        return p.Negative
            ? new XsdDuration(-months, new XsdDecimal(-mantissa, scale))
            : new XsdDuration(months, new XsdDecimal(mantissa, scale));
    }

    /// <summary>Gets the canonical lexical form of the <c>duration</c> type.</summary>
    /// <returns>The canonical form.</returns>
    public override string ToString() => ToString(DurationKind.Duration);

    /// <summary>Gets the canonical lexical form; the kind only decides the spelling of zero.</summary>
    /// <param name="kind"><c>P0M</c> for <see cref="DurationKind.YearMonth"/>, <c>PT0S</c> otherwise.</param>
    /// <returns>The canonical form.</returns>
    public string ToString(DurationKind kind)
    {
        var builder = new StringBuilder();
        if (Months < 0 || Seconds.Sign < 0)
        {
            builder.Append('-');
        }

        builder.Append('P');
        var months = Math.Abs(Months);
        Append(builder, months / 12, 'Y');
        Append(builder, months % 12, 'M');

        var mantissa = BigInteger.Abs(Seconds.Mantissa);
        var scale = Seconds.Scale;
        var fraction = string.Empty;
        BigInteger whole;
        if (scale <= 0)
        {
            whole = mantissa * BigInteger.Pow(10, -scale);
        }
        else
        {
            whole = BigInteger.DivRem(mantissa, BigInteger.Pow(10, scale), out var remainder);
            fraction = remainder.ToString(CultureInfo.InvariantCulture).PadLeft(scale, '0');
        }

        var days = BigInteger.DivRem(whole, SecondsPerDay, out var rest);
        Append(builder, days, 'D');
        var daySeconds = (int)rest;
        if (daySeconds > 0 || fraction.Length > 0)
        {
            builder.Append('T');
            Append(builder, daySeconds / SecondsPerHour, 'H');
            Append(builder, daySeconds % SecondsPerHour / SecondsPerMinute, 'M');
            var seconds = daySeconds % SecondsPerMinute;
            if (seconds > 0 || fraction.Length > 0)
            {
                builder.Append(seconds.ToString(CultureInfo.InvariantCulture));
                if (fraction.Length > 0)
                {
                    builder.Append('.').Append(fraction);
                }

                builder.Append('S');
            }
        }

        if (builder[builder.Length - 1] == 'P')
        {
            builder.Append(kind == DurationKind.YearMonth ? "0M" : "T0S");
        }

        return builder.ToString();
    }

    /// <inheritdoc />
    public bool Equals(XsdDuration other) => Months == other.Months && Seconds.Equals(other.Seconds);

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is XsdDuration other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => unchecked((Months.GetHashCode() * 397) ^ Seconds.GetHashCode());

    /// <summary>Determines whether two durations are equal by value.</summary>
    /// <param name="left">The first value.</param>
    /// <param name="right">The second value.</param>
    /// <returns><see langword="true"/> when equal.</returns>
    public static bool operator ==(XsdDuration left, XsdDuration right) => left.Equals(right);

    /// <summary>Determines whether two durations differ by value.</summary>
    /// <param name="left">The first value.</param>
    /// <param name="right">The second value.</param>
    /// <returns><see langword="true"/> when not equal.</returns>
    public static bool operator !=(XsdDuration left, XsdDuration right) => !left.Equals(right);

    private static void Append(StringBuilder builder, BigInteger value, char designator)
    {
        if (!value.IsZero)
        {
            builder.Append(value.ToString(CultureInfo.InvariantCulture)).Append(designator);
        }
    }

    // The fraction digits in chunks of 18, so that every chunk fits an unsigned 64-bit integer.
    private static BigInteger Fraction(ReadOnlySpan<char> digits)
    {
        var result = BigInteger.Zero;
        for (var i = 0; i < digits.Length; i += 18)
        {
            var length = Math.Min(18, digits.Length - i);
            ulong chunk = 0;
            for (var j = 0; j < length; j++)
            {
                chunk = (chunk * 10) + (ulong)(digits[i + j] - '0');
            }

            result = (result * BigInteger.Pow(10, length)) + chunk;
        }

        return result;
    }
}
