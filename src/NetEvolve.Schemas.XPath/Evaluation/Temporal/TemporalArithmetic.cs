namespace NetEvolve.Schemas.XPath.Evaluation.Temporal;

using System;
using System.Numerics;
using NetEvolve.Schemas.Xml.Datatypes.Decimals;
using NetEvolve.Schemas.Xml.Datatypes.Decimals.Arithmetic;
using NetEvolve.Schemas.Xml.Datatypes.Duration;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Evaluation.Arithmetic;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The arithmetic of durations, dates, and times (Functions and Operators 1.0, 10.6 and 10.8): the sum and the difference of two
/// durations of one type, a duration times or divided by a number, a duration divided by a duration of its type, a duration added to or
/// subtracted from a date, a time, or a dateTime, and the difference of two moments.
/// </summary>
/// <remarks>
/// <para>
/// Every method is the implementation of one operator mapping row and is registered in the operator table by
/// <see cref="TemporalOperators"/>; none is called by name from the arithmetic folder. A result outside the range
/// (<see cref="TemporalRange"/>) is FODT0001 for a date or time and for the difference of two moments, and FODT0002 for a duration. The multiplication and the division by a
/// number promote the number to <c>xs:double</c> (XPath 2.0, B.1), read it as the decimal of its shortest form, round the months of a
/// year-month duration half up as <c>fn:round</c> does, and keep the seconds of a day-time duration to 18 fraction digits rounded half
/// to even, as the division of decimals does (#100 AP-7).
/// </para>
/// <para>
/// Decision: a zero divisor, and a multiplication by an infinity, raise FODT0002, because the result is out of range; NaN raises
/// FOCA0005; a division by an infinity is a zero duration. Limitation: the factor is exact to its shortest form of 17 digits, not to
/// its binary value.
/// </para>
/// </remarks>
internal static class TemporalArithmetic
{
    private const int FractionDigits = DecimalArithmetic.DefaultFractionDigits;

    /// <summary>Adds two durations of one type: <c>op:add-yearMonthDurations</c> and <c>op:add-dayTimeDurations</c>.</summary>
    /// <param name="left">The left duration.</param>
    /// <param name="right">The right duration, of the type of <paramref name="left"/>.</param>
    /// <returns>The sum, of the type of the operands.</returns>
    /// <exception cref="XPathException">The sum is beyond the range (FODT0002), or an operand is no duration (XPTY0004).</exception>
    public static XdmItem AddDurations(in XdmItem left, in XdmItem right) => Combine(in left, in right, false);

    /// <summary>Subtracts a duration from one of the same type: <c>op:subtract-yearMonthDurations</c> and <c>op:subtract-dayTimeDurations</c>.</summary>
    /// <param name="left">The left duration.</param>
    /// <param name="right">The right duration, of the type of <paramref name="left"/>.</param>
    /// <returns>The difference, of the type of the operands.</returns>
    /// <exception cref="XPathException">The difference is beyond the range (FODT0002), or an operand is no duration (XPTY0004).</exception>
    public static XdmItem SubtractDurations(in XdmItem left, in XdmItem right) => Combine(in left, in right, true);

    /// <summary>Adds a duration to a date, a time, or a dateTime, in either order of the operands.</summary>
    /// <param name="left">The moment or the duration.</param>
    /// <param name="right">The duration or the moment.</param>
    /// <returns>The moment moved by the duration, of the type of the moment.</returns>
    /// <exception cref="XPathException">The year of the result has more than 17 digits (FODT0001), or the operands are not a moment and a duration (XPTY0004).</exception>
    public static XdmItem AddToMoment(in XdmItem left, in XdmItem right) =>
        left.TryGetDateTime(out _) ? MoveMoment(in left, in right, false) : MoveMoment(in right, in left, false);

    /// <summary>Subtracts a duration from a date, a time, or a dateTime.</summary>
    /// <param name="left">The moment.</param>
    /// <param name="right">The duration.</param>
    /// <returns>The moment moved back by the duration, of the type of the moment.</returns>
    /// <exception cref="XPathException">The year of the result has more than 17 digits (FODT0001), or the operands are not a moment and a duration (XPTY0004).</exception>
    public static XdmItem SubtractFromMoment(in XdmItem left, in XdmItem right) => MoveMoment(in left, in right, true);

    /// <summary>Subtracts two dates, two times, or two dateTimes: <c>op:subtract-dates</c>, <c>op:subtract-times</c>, and <c>op:subtract-dateTimes</c>.</summary>
    /// <param name="left">The minuend.</param>
    /// <param name="right">The subtrahend, of the type of <paramref name="left"/>.</param>
    /// <param name="context">The dynamic context, which gives the implicit timezone of a value without a timezone.</param>
    /// <returns>The <c>xs:dayTimeDuration</c> between the two moments.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> is <see langword="null"/>.</exception>
    /// <exception cref="XPathException">The difference is beyond the range (FODT0001, as the QT3 sets expect for an operation on dates and times), or an operand is no date or time value (XPTY0004).</exception>
    public static XdmItem SubtractMoments(in XdmItem left, in XdmItem right, XPathEvaluationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!left.TryGetTemporal(out var a) || !right.TryGetTemporal(out var b))
        {
            throw XPathErrors.Exception(XPathErrorCodes.XPTY0004);
        }

        var timezone = context.ImplicitTimezoneMinutes;
        try
        {
            var difference = TemporalValues.Instant(in a, timezone).Subtract(TemporalValues.Instant(in b, timezone));
            return DayTime(TemporalRange.CheckSeconds(difference, XPathErrorCodes.FODT0001));
        }
        catch (OverflowException)
        {
            throw XPathErrors.Exception(XPathErrorCodes.FODT0001);
        }
    }

    /// <summary>Multiplies a duration by a number, in either order of the operands.</summary>
    /// <param name="left">The duration or the number.</param>
    /// <param name="right">The number or the duration.</param>
    /// <returns>The duration, of the type of the duration operand.</returns>
    /// <exception cref="XPathException">The number is NaN (FOCA0005), or the product is beyond the range (FODT0002), or the operands are not a duration and a number (XPTY0004).</exception>
    public static XdmItem MultiplyDuration(in XdmItem left, in XdmItem right) =>
        IsDuration(in left) ? Scale(in left, in right, false) : Scale(in right, in left, false);

    /// <summary>Divides a duration by a number.</summary>
    /// <param name="left">The duration.</param>
    /// <param name="right">The number.</param>
    /// <returns>The duration, of the type of <paramref name="left"/>.</returns>
    /// <exception cref="XPathException">The number is NaN (FOCA0005), or zero, or the quotient is beyond the range (FODT0002), or the operands are not a duration and a number (XPTY0004).</exception>
    public static XdmItem DivideDuration(in XdmItem left, in XdmItem right) => Scale(in left, in right, true);

    /// <summary>Divides a duration by a duration of the same type: <c>op:divide-yearMonthDuration-by-yearMonthDuration</c> and <c>op:divide-dayTimeDuration-by-dayTimeDuration</c>.</summary>
    /// <param name="left">The dividend.</param>
    /// <param name="right">The divisor, of the type of <paramref name="left"/>.</param>
    /// <returns>The quotient as an <c>xs:decimal</c>.</returns>
    /// <exception cref="XPathException">The divisor is zero (FOAR0001), or the quotient overflows (FOAR0002), or an operand is no duration (XPTY0004).</exception>
    public static XdmItem DivideDurations(in XdmItem left, in XdmItem right)
    {
        if (!left.TryGetDuration(out var a) || !right.TryGetDuration(out var b))
        {
            throw XPathErrors.Exception(XPathErrorCodes.XPTY0004);
        }

        var yearMonth = left.Kind == XdmItemKind.YearMonthDuration;
        var dividend = yearMonth ? new XsdDecimal(a.Months, 0) : a.Seconds;
        var divisor = yearMonth ? new XsdDecimal(b.Months, 0) : b.Seconds;
        if (divisor.Sign == 0)
        {
            throw XPathErrors.Exception(XPathErrorCodes.FOAR0001);
        }

        var quotient = DecimalArithmetic.Divide(dividend, divisor);
        return quotient.Success
            ? XdmItem.FromDecimal(quotient.Value)
            : throw XPathErrors.Exception(XPathErrorCodes.FOAR0002);
    }

    private static XdmItem Combine(in XdmItem left, in XdmItem right, bool subtract)
    {
        if (!left.TryGetDuration(out var a) || !right.TryGetDuration(out var b))
        {
            throw XPathErrors.Exception(XPathErrorCodes.XPTY0004);
        }

        try
        {
            if (left.Kind == XdmItemKind.YearMonthDuration)
            {
                BigInteger months = a.Months;
                months = subtract ? months - b.Months : months + b.Months;
                return YearMonth(TemporalRange.CheckMonths(months));
            }

            var seconds = subtract ? a.Seconds.Subtract(b.Seconds) : a.Seconds.Add(b.Seconds);
            return DayTime(TemporalRange.CheckSeconds(seconds, XPathErrorCodes.FODT0002));
        }
        catch (OverflowException)
        {
            throw XPathErrors.Exception(XPathErrorCodes.FODT0002);
        }
    }

    private static XdmItem MoveMoment(in XdmItem moment, in XdmItem duration, bool subtract)
    {
        if (!moment.TryGetDateTime(out var value) || !duration.TryGetDuration(out var span))
        {
            throw XPathErrors.Exception(XPathErrorCodes.XPTY0004);
        }

        try
        {
            return XdmItem.FromDateTime(
                TemporalValues.AddDuration(in value, subtract ? TemporalValues.Negate(in span) : span)
            );
        }
        catch (OverflowException)
        {
            throw XPathErrors.Exception(XPathErrorCodes.FODT0001);
        }
    }

    private static XdmItem Scale(in XdmItem duration, in XdmItem number, bool divide)
    {
        if (!duration.TryGetDuration(out var span) || !NumericPromotion.TryToDouble(in number, out var factor))
        {
            throw XPathErrors.Exception(XPathErrorCodes.XPTY0004);
        }

        if (double.IsNaN(factor))
        {
            throw XPathErrors.Exception(XPathErrorCodes.FOCA0005);
        }

        var yearMonth = duration.Kind == XdmItemKind.YearMonthDuration;
        if (double.IsInfinity(factor))
        {
            // A division by an infinity is zero; a multiplication by one is out of range.
            return divide
                ? (yearMonth ? YearMonth(0) : DayTime(XsdDecimal.Zero))
                : throw XPathErrors.Exception(XPathErrorCodes.FODT0002);
        }

        if (divide && factor == 0)
        {
            throw XPathErrors.Exception(XPathErrorCodes.FODT0002);
        }

        try
        {
            var exact = TemporalValues.ToDecimal(factor);
            if (yearMonth)
            {
                var months = new XsdDecimal(span.Months, 0);
                var scaled = divide ? Quotient(months, exact) : months.Multiply(exact);
                var whole = DecimalArithmetic.ToBigInteger(DecimalArithmetic.Round(scaled));
                return whole.Success
                    ? YearMonth(TemporalRange.CheckMonths(whole.Value))
                    : throw XPathErrors.Exception(XPathErrorCodes.FODT0002);
            }

            var seconds = divide
                ? Quotient(span.Seconds, exact)
                : DecimalArithmetic.RoundHalfToEven(span.Seconds.Multiply(exact), FractionDigits);
            return DayTime(TemporalRange.CheckSeconds(seconds, XPathErrorCodes.FODT0002));
        }
        catch (OverflowException)
        {
            throw XPathErrors.Exception(XPathErrorCodes.FODT0002);
        }
    }

    private static XsdDecimal Quotient(XsdDecimal dividend, XsdDecimal divisor)
    {
        var quotient = DecimalArithmetic.Divide(dividend, divisor);
        return quotient.Success ? quotient.Value : throw XPathErrors.Exception(XPathErrorCodes.FODT0002);
    }

    private static bool IsDuration(in XdmItem item) =>
        item.Kind is XdmItemKind.YearMonthDuration or XdmItemKind.DayTimeDuration;

    private static XdmItem YearMonth(long months) =>
        XdmItem.FromDuration(new XsdDuration(months, XsdDecimal.Zero), DurationKind.YearMonth);

    private static XdmItem DayTime(XsdDecimal seconds) =>
        XdmItem.FromDuration(new XsdDuration(0, seconds), DurationKind.DayTime);
}
