namespace NetEvolve.Schemas.XPath.Functions.DateTime;

using System;
using System.Numerics;
using NetEvolve.Schemas.Xml.Datatypes.DateTime;
using NetEvolve.Schemas.Xml.Datatypes.Decimals;
using NetEvolve.Schemas.Xml.Datatypes.Decimals.Arithmetic;
using NetEvolve.Schemas.Xml.Datatypes.Duration;
using NetEvolve.Schemas.Xml.Datatypes.Ordering;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The arithmetic behind the date and time functions, on the exact value types of the Xml package: the components of a duration,
/// the timezone of a value as a duration, the move of a value to another timezone, and the value of the current date and time.
/// </summary>
/// <remarks>
/// Limitations: the current date and time has the 100 nanosecond precision of <see cref="DateTimeOffset"/>, and a component that
/// does not fit the 64-bit <c>xs:integer</c> of the item model raises FOAR0002. A move of a value across the year -1 follows the
/// proleptic calendar with a year 0 of <c>DateTimeTimeline</c> (the same limitation as there).
/// </remarks>
internal static class DateTimeValues
{
    private const int MaxTimezoneMinutes = 14 * 60;
    private const int SecondsPerMinute = 60;
    private const int SecondsPerHour = 3600;
    private const int SecondsPerDay = 86400;
    private const int TickScale = 7;

    // The reference day of a time, as in the timeline of the Xml package: it never shows, because only the time of day is kept.
    private const int ReferenceYear = 1972;
    private const int ReferenceMonth = 12;
    private const int ReferenceDay = 31;

    private static readonly XsdDecimal MaxTimezoneSeconds = new(MaxTimezoneMinutes * SecondsPerMinute, 0);
    private static readonly XsdDecimal MinTimezoneSeconds = MaxTimezoneSeconds.Negate();
    private static readonly XsdDecimal OneMinute = new(SecondsPerMinute, 0);

    /// <summary>Gets the timezone of a value as an <c>xs:dayTimeDuration</c> item.</summary>
    /// <param name="value">The value.</param>
    /// <returns>The item; <see langword="default"/> when the value has no timezone.</returns>
    internal static XdmItem Timezone(in XsdDateTime value) =>
        value.TimezoneOffsetMinutes is { } minutes ? TimezoneItem(minutes) : default;

    /// <summary>Creates the <c>xs:dayTimeDuration</c> item of a timezone offset.</summary>
    /// <param name="minutes">The offset in minutes.</param>
    /// <returns>The item.</returns>
    internal static XdmItem TimezoneItem(int minutes) =>
        XdmItem.FromDuration(
            new XsdDuration(0, new XsdDecimal(minutes * (long)SecondsPerMinute, 0)),
            DurationKind.DayTime
        );

    /// <summary>Gets the days of the canonical form of a duration, truncated toward zero.</summary>
    /// <param name="value">The duration.</param>
    /// <returns>The days.</returns>
    /// <exception cref="XPathException">The number of days does not fit an <c>xs:integer</c> (FOAR0002).</exception>
    internal static XdmItem Days(in XsdDuration value) => Quotient(value.Seconds, SecondsPerDay);

    /// <summary>Gets the hours of the canonical form of a duration, truncated toward zero.</summary>
    /// <param name="value">The duration.</param>
    /// <returns>The hours, less than 24 in magnitude.</returns>
    /// <exception cref="XPathException">The duration cannot be divided (FOAR0002).</exception>
    internal static XdmItem Hours(in XsdDuration value) =>
        Quotient(Remainder(value.Seconds, SecondsPerDay), SecondsPerHour);

    /// <summary>Gets the minutes of the canonical form of a duration, truncated toward zero.</summary>
    /// <param name="value">The duration.</param>
    /// <returns>The minutes, less than 60 in magnitude.</returns>
    /// <exception cref="XPathException">The duration cannot be divided (FOAR0002).</exception>
    internal static XdmItem Minutes(in XsdDuration value) =>
        Quotient(Remainder(value.Seconds, SecondsPerHour), SecondsPerMinute);

    /// <summary>Gets the seconds of the canonical form of a duration, with the fraction.</summary>
    /// <param name="value">The duration.</param>
    /// <returns>The seconds, less than 60 in magnitude and signed like the duration.</returns>
    /// <exception cref="XPathException">The duration cannot be divided (FOAR0002).</exception>
    internal static XsdDecimal Seconds(in XsdDuration value) => Remainder(value.Seconds, SecondsPerMinute);

    /// <summary>Reads a timezone argument: a whole number of minutes from -PT14H to PT14H.</summary>
    /// <param name="duration">The <c>xs:dayTimeDuration</c> value.</param>
    /// <param name="minutes">The offset in minutes.</param>
    /// <returns><see langword="true"/> when the duration is a valid timezone.</returns>
    internal static bool TryGetTimezoneMinutes(in XsdDuration duration, out int minutes)
    {
        minutes = 0;
        var seconds = duration.Seconds;
        if (seconds.CompareTo(MinTimezoneSeconds) < 0 || seconds.CompareTo(MaxTimezoneSeconds) > 0)
        {
            return false;
        }

        var rest = DecimalArithmetic.Remainder(seconds, OneMinute);
        if (!rest.Success || rest.Value.Sign != 0)
        {
            return false;
        }

        minutes = (int)(DecimalArithmetic.ToBigInteger(seconds).Value / SecondsPerMinute);
        return true;
    }

    /// <summary>Moves a value to another timezone: a value without one gains it, a value with one keeps its instant, and no timezone removes it.</summary>
    /// <param name="value">The <c>xs:dateTime</c>, <c>xs:date</c>, or <c>xs:time</c> value.</param>
    /// <param name="timezone">The offset in minutes, <see langword="null"/> for no timezone.</param>
    /// <returns>The adjusted value of the same type.</returns>
    internal static XsdDateTime Adjust(in XsdDateTime value, int? timezone)
    {
        if (value.TimezoneOffsetMinutes is not { } current || timezone is not { } target)
        {
            return WithTimezone(in value, timezone);
        }

        return current == target ? value : WithTimezone(Shift(in value, target - current), target);
    }

    /// <summary>Creates the value of the current date and time of the dynamic context.</summary>
    /// <param name="now">The current date and time in the implicit timezone.</param>
    /// <param name="kind"><see cref="XsdDateTimeKind.DateTime"/>, <see cref="XsdDateTimeKind.Date"/>, or <see cref="XsdDateTimeKind.Time"/>.</param>
    /// <returns>The value, with the timezone of <paramref name="now"/>.</returns>
    internal static XsdDateTime Current(DateTimeOffset now, XsdDateTimeKind kind)
    {
        var offset = (int)now.Offset.TotalMinutes;
        var second = new XsdDecimal(
            ((long)now.Second * TimeSpan.TicksPerSecond) + (now.Ticks % TimeSpan.TicksPerSecond),
            TickScale
        );
        return kind switch
        {
            XsdDateTimeKind.Date => XsdDateTime.Create(
                XsdDateTimeKind.Date,
                now.Year,
                now.Month,
                now.Day,
                0,
                0,
                XsdDecimal.Zero,
                offset
            ),
            XsdDateTimeKind.Time => XsdDateTime.Create(
                XsdDateTimeKind.Time,
                0,
                0,
                0,
                now.Hour,
                now.Minute,
                second,
                offset
            ),
            _ => XsdDateTime.Create(
                XsdDateTimeKind.DateTime,
                now.Year,
                now.Month,
                now.Day,
                now.Hour,
                now.Minute,
                second,
                offset
            ),
        };
    }

    private static XsdDateTime WithTimezone(in XsdDateTime value, int? timezone) =>
        XsdDateTime.Create(
            value.Kind,
            value.Year,
            value.Month,
            value.Day,
            value.Hour,
            value.Minute,
            value.Second,
            timezone
        );

    // Moves the local value by a number of minutes; the timezone of the result is not meant to be read. A date is moved as its
    // first moment and keeps the day it falls on; a time wraps around the day.
    private static XsdDateTime Shift(in XsdDateTime value, int minutes)
    {
        var shift = new XsdDuration(0, new XsdDecimal(minutes * (long)SecondsPerMinute, 0));
        switch (value.Kind)
        {
            case XsdDateTimeKind.Date:
                var start = XsdDateTime.Create(
                    XsdDateTimeKind.DateTime,
                    value.Year,
                    value.Month,
                    value.Day,
                    0,
                    0,
                    XsdDecimal.Zero,
                    null
                );
                var day = DateTimeTimeline.Add(in shift, in start);
                return XsdDateTime.Create(
                    XsdDateTimeKind.Date,
                    day.Year,
                    day.Month,
                    day.Day,
                    0,
                    0,
                    XsdDecimal.Zero,
                    null
                );
            case XsdDateTimeKind.Time:
                var moment = XsdDateTime.Create(
                    XsdDateTimeKind.DateTime,
                    ReferenceYear,
                    ReferenceMonth,
                    ReferenceDay,
                    value.Hour,
                    value.Minute,
                    value.Second,
                    null
                );
                var time = DateTimeTimeline.Add(in shift, in moment);
                return XsdDateTime.Create(XsdDateTimeKind.Time, 0, 0, 0, time.Hour, time.Minute, time.Second, null);
            default:
                return DateTimeTimeline.Add(in shift, in value);
        }
    }

    private static XsdDecimal Remainder(XsdDecimal dividend, int divisor)
    {
        var result = DecimalArithmetic.Remainder(dividend, new XsdDecimal(divisor, 0));
        return result.Success ? result.Value : throw XPathErrors.Exception(XPathErrorCodes.FOAR0002);
    }

    private static XdmItem Quotient(XsdDecimal dividend, int divisor)
    {
        var result = DecimalArithmetic.IntegerDivide(dividend, new XsdDecimal(divisor, 0));
        return result.Success && result.Value >= long.MinValue && result.Value <= long.MaxValue
            ? XdmItem.FromInteger((long)result.Value)
            : throw XPathErrors.Exception(XPathErrorCodes.FOAR0002);
    }
}
