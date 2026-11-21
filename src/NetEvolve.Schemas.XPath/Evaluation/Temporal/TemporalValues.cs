namespace NetEvolve.Schemas.XPath.Evaluation.Temporal;

using System;
using System.Globalization;
using System.Numerics;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Datatypes.DateTime;
using NetEvolve.Schemas.Xml.Datatypes.Decimals;
using NetEvolve.Schemas.Xml.Datatypes.Duration;
using NetEvolve.Schemas.Xml.Datatypes.Floating;
using NetEvolve.Schemas.Xml.Datatypes.Ordering;
using NetEvolve.Schemas.XPath.Errors;

/// <summary>
/// The values behind the operators: a date or time value on the timeline with the implicit timezone, the addition of a duration to a
/// date, a time, or a dateTime, and the exact decimal of a double. The timeline and the addition are those of the datatype layer
/// (<see cref="DateTimeTimeline"/>); this class adds what XPath 2.0 defines on top: the implicit timezone in place of the plus or minus
/// 14 hour window, the year numbering of XML Schema 1.0, the range, and the date and time results.
/// </summary>
internal static class TemporalValues
{
    // The reference day of a time (Functions and Operators 1.0, 10.8): the same as in the timeline of the datatype layer.
    private const int ReferenceYear = 1972;
    private const int ReferenceMonth = 12;
    private const int ReferenceDay = 31;

    /// <summary>Maps a value to seconds on the timeline, in the implicit timezone when it has none.</summary>
    /// <param name="value">The value of any of the eight date and time types.</param>
    /// <param name="implicitTimezoneMinutes">The implicit timezone of the dynamic context in minutes from UTC.</param>
    /// <returns>The seconds; a value with a timezone is moved to UTC, a value without one is read in the implicit timezone.</returns>
    /// <remarks>
    /// The absent properties of a time and of the Gregorian types are filled as the datatype layer fills them (XML Schema 1.1, E.3.4):
    /// equal values map to equal seconds, and the order of two values of one type is the order of their seconds.
    /// </remarks>
    public static XsdDecimal Instant(in XsdDateTime value, int implicitTimezoneMinutes)
    {
        var offset = value.TimezoneOffsetMinutes;
        var mapped =
            value.Year < 0
                ? XsdDateTime.Create(
                    value.Kind,
                    TemporalRange.ToTimelineYear(value.Year),
                    value.Month,
                    value.Day,
                    value.Hour,
                    value.Minute,
                    value.Second,
                    offset
                )
                : value;
        return DateTimeTimeline.TimeOnTimeline(in mapped, offset.HasValue ? 0 : -implicitTimezoneMinutes);
    }

    /// <summary>Adds a duration to a date, a time, or a dateTime; the timezone of the value, or its absence, is kept.</summary>
    /// <param name="value">The <c>xs:dateTime</c>, <c>xs:date</c>, or <c>xs:time</c> value.</param>
    /// <param name="duration">The duration; a negative one subtracts.</param>
    /// <returns>
    /// The value of the same type: the months are added first with the day pinned to the month, then the seconds. A date is the date part
    /// of the dateTime at its first moment plus the duration, so a duration of less than a day beyond the first moment does not change it. A
    /// time wraps around the day.
    /// </returns>
    /// <exception cref="XPathException">The year of the result has more than 17 digits (FODT0001).</exception>
    public static XsdDateTime AddDuration(in XsdDateTime value, in XsdDuration duration)
    {
        var offset = value.TimezoneOffsetMinutes;
        if (value.Kind == XsdDateTimeKind.Time)
        {
            var moment = XsdDateTime.Create(
                XsdDateTimeKind.DateTime,
                ReferenceYear,
                ReferenceMonth,
                ReferenceDay,
                value.Hour,
                value.Minute,
                value.Second,
                offset
            );
            var time = DateTimeTimeline.Add(in duration, in moment);
            return XsdDateTime.Create(XsdDateTimeKind.Time, 0, 0, 0, time.Hour, time.Minute, time.Second, offset);
        }

        var isDate = value.Kind == XsdDateTimeKind.Date;
        var start = XsdDateTime.Create(
            XsdDateTimeKind.DateTime,
            TemporalRange.ToTimelineYear(value.Year),
            value.Month,
            value.Day,
            isDate ? 0 : value.Hour,
            isDate ? 0 : value.Minute,
            isDate ? XsdDecimal.Zero : value.Second,
            offset
        );
        var sum = DateTimeTimeline.Add(in duration, in start);
        var year = TemporalRange.FromTimelineYear(sum.Year);
        TemporalRange.CheckYear(year);
        return isDate
            ? XsdDateTime.Create(XsdDateTimeKind.Date, year, sum.Month, sum.Day, 0, 0, XsdDecimal.Zero, offset)
            : XsdDateTime.Create(
                XsdDateTimeKind.DateTime,
                year,
                sum.Month,
                sum.Day,
                sum.Hour,
                sum.Minute,
                sum.Second,
                offset
            );
    }

    /// <summary>Gets the opposite of a duration.</summary>
    /// <param name="duration">The duration.</param>
    /// <returns>The duration with both parts negated.</returns>
    public static XsdDuration Negate(in XsdDuration duration) => new(-duration.Months, duration.Seconds.Negate());

    /// <summary>Gets the decimal of a finite double the way its shortest form reads: <c>0.1e0</c> is the decimal 0.1, not its binary expansion.</summary>
    /// <param name="value">The finite double.</param>
    /// <returns>The decimal.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is an infinity.</exception>
    /// <remarks>
    /// The shortest form is decision D6 of the date, time, and duration operators (#300); <c>DecimalArithmetic.FromDouble</c> converts the
    /// exact binary value instead.
    /// </remarks>
    public static XsdDecimal ToDecimal(double value)
    {
        Argument.ThrowIfGreaterThan(Math.Abs(value), double.MaxValue, nameof(value));

        if (value == 0)
        {
            return XsdDecimal.Zero;
        }

        var digits = BinaryFloat.ShortestDigits(Math.Abs(value), false, out var exponent);
        var mantissa = BigInteger.Parse(digits, CultureInfo.InvariantCulture);
        return new XsdDecimal(value < 0 ? -mantissa : mantissa, digits.Length - 1 - exponent);
    }
}
