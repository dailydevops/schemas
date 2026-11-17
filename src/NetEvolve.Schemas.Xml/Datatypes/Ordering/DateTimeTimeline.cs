namespace NetEvolve.Schemas.Xml.Datatypes.Ordering;

using System;
using System.Numerics;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.DateTime;
using NetEvolve.Schemas.Xml.Datatypes.Decimals;
using NetEvolve.Schemas.Xml.Datatypes.Duration;

/// <summary>
/// The order of the date and time values on the timeline (XSD 1.1 Part 2, appendix E.3.4) and the addition of a duration
/// to a dateTime (appendix E.3.3). Absent properties of the Gregorian and time types are filled from 1972-12-31T00:00:00,
/// with the largest day of the month when only the day is absent.
/// </summary>
/// <remarks>
/// Limitation: the version does not change the order, so the year <c>-0001</c> and year 0 follow the 1.1 proleptic calendar
/// in version 1.0 as well. Values of different kinds are incomparable.
/// </remarks>
internal static class DateTimeTimeline
{
    private const int MaxOffsetMinutes = 14 * 60;
    private const int SecondsPerDay = 86400;
    private const int DaysPerCycle = 146097;

    /// <summary>Compares two values: equal timezone presence orders exactly, a missing timezone is bracketed by plus and minus 14 hours.</summary>
    internal static XsdOrder Compare(in XsdDateTime a, in XsdDateTime b)
    {
        if (a.Kind != b.Kind)
        {
            return XsdOrder.Incomparable;
        }

        var offsetA = a.TimezoneOffsetMinutes;
        var offsetB = b.TimezoneOffsetMinutes;
        if (offsetA.HasValue == offsetB.HasValue)
        {
            return From(TimeOnTimeline(in a, 0).CompareTo(TimeOnTimeline(in b, 0)));
        }

        // The value without timezone lies within 14 hours on either side of its reading as UTC.
        var zoneless = offsetA.HasValue ? b : a;
        var zoned = offsetA.HasValue ? a : b;
        var fixedTime = TimeOnTimeline(in zoned, 0);
        var earliest = TimeOnTimeline(in zoneless, -MaxOffsetMinutes);
        var latest = TimeOnTimeline(in zoneless, MaxOffsetMinutes);
        XsdOrder order;
        if (latest.CompareTo(fixedTime) < 0)
        {
            order = XsdOrder.Less;
        }
        else if (earliest.CompareTo(fixedTime) > 0)
        {
            order = XsdOrder.Greater;
        }
        else
        {
            return XsdOrder.Incomparable;
        }

        return offsetA.HasValue ? Reverse(order) : order;
    }

    /// <summary>Maps a value to seconds on the timeline; <paramref name="extraMinutes"/> is added for the imputed timezone of a value without one.</summary>
    internal static XsdDecimal TimeOnTimeline(in XsdDateTime value, int extraMinutes)
    {
        var year = value.Year;
        var month = value.Month;
        var day = value.Day;
        switch (value.Kind)
        {
            case XsdDateTimeKind.Time:
                year = 1972;
                month = 12;
                day = 31;
                break;
            case XsdDateTimeKind.GYear:
                month = 12;
                day = 31;
                break;
            case XsdDateTimeKind.GYearMonth:
                day = XsdDateTime.DaysInMonth(year, month);
                break;
            case XsdDateTimeKind.GMonth:
                year = 1972;
                day = XsdDateTime.DaysInMonth(year, month);
                break;
            case XsdDateTimeKind.GMonthDay:
                year = 1972;
                break;
            case XsdDateTimeKind.GDay:
                year = 1972;
                month = 12;
                break;
            default:
                break;
        }

        var minutes = ((long)value.Hour * 60) + value.Minute + extraMinutes - (value.TimezoneOffsetMinutes ?? 0);
        var whole = (DayNumber(year, month, day) * SecondsPerDay) + (minutes * 60);
        return new XsdDecimal(whole, 0).Add(value.Second);
    }

    /// <summary>Adds a duration to a dateTime: the months first with the day pinned to the month, then the seconds.</summary>
    internal static XsdDateTime Add(in XsdDuration duration, in XsdDateTime value)
    {
        var total = (((BigInteger)value.Year * 12) + (value.Month - 1)) + duration.Months;
        var year = FloorDiv(total, 12, out var monthIndex);
        var month = (int)monthIndex + 1;
        var yearLong = (long)year;
        var day = Math.Min(value.Day, XsdDateTime.DaysInMonth(yearLong, month));

        // Seconds of the day plus the duration, as an exact decimal; split into days and the rest.
        var seconds = new XsdDecimal(((long)value.Hour * 3600) + ((long)value.Minute * 60), 0)
            .Add(value.Second)
            .Add(duration.Seconds);
        var scale = Math.Max(seconds.Scale, 0);
        var unit = BigInteger.Pow(10, scale);
        var mantissa = seconds.Mantissa * BigInteger.Pow(10, Math.Max(-seconds.Scale, 0));
        var days = FloorDiv(mantissa, SecondsPerDay * unit, out var rest);
        var wholeSeconds = (int)(rest / unit);
        var fraction = rest - (wholeSeconds * unit);
        var second = new XsdDecimal(((wholeSeconds % 60) * unit) + fraction, scale);

        FromDayNumber(DayNumber(yearLong, month, day) + days, out var newYear, out var newMonth, out var newDay);
        return XsdDateTime.Create(
            XsdDateTimeKind.DateTime,
            newYear,
            newMonth,
            newDay,
            wholeSeconds / 3600,
            wholeSeconds % 3600 / 60,
            second,
            value.TimezoneOffsetMinutes
        );
    }

    // Days since 0001-01-01 in the proleptic calendar with a year 0.
    private static BigInteger DayNumber(long year, int month, int day)
    {
        BigInteger previous = year - 1;
        var days =
            (previous * 365)
            + FloorDiv(previous, 4, out _)
            - FloorDiv(previous, 100, out _)
            + FloorDiv(previous, 400, out _);
        for (var m = 1; m < month; m++)
        {
            days += XsdDateTime.DaysInMonth(year, m);
        }

        return days + (day - 1);
    }

    private static void FromDayNumber(BigInteger days, out long year, out int month, out int day)
    {
        var cycles = FloorDiv(days, DaysPerCycle, out var rest);
        var r = (int)rest;
        var centuries = Math.Min(r / 36524, 3);
        r -= centuries * 36524;
        var fours = r / 1461;
        r %= 1461;
        var ones = Math.Min(r / 365, 3);
        r -= ones * 365;
        year = (long)(1 + (cycles * 400)) + (centuries * 100) + (fours * 4) + ones;
        month = 1;
        while (r >= XsdDateTime.DaysInMonth(year, month))
        {
            r -= XsdDateTime.DaysInMonth(year, month);
            month++;
        }

        day = r + 1;
    }

    private static BigInteger FloorDiv(BigInteger value, BigInteger divisor, out BigInteger remainder)
    {
        var quotient = BigInteger.DivRem(value, divisor, out remainder);
        if (remainder.Sign < 0)
        {
            quotient -= 1;
            remainder += divisor;
        }

        return quotient;
    }

    internal static XsdOrder From(int comparison) =>
        comparison < 0 ? XsdOrder.Less
        : comparison > 0 ? XsdOrder.Greater
        : XsdOrder.Equal;

    private static XsdOrder Reverse(XsdOrder order) =>
        order == XsdOrder.Less ? XsdOrder.Greater
        : order == XsdOrder.Greater ? XsdOrder.Less
        : order;
}
