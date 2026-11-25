namespace NetEvolve.Schemas.XPath.Formatting.Dates;

using System;
using System.Globalization;
using System.Numerics;
using System.Text;
using NetEvolve.Schemas.Xml.Datatypes.DateTime;
using NetEvolve.Schemas.XPath.Formatting.Integers;

/// <summary>
/// Writes a date, a time, or a date and time in the markers of a <see cref="DatePicture"/>: the components of the Gregorian calendar,
/// with the English names of the months and the days, the AM/PM marker, the era, and the time zone as an offset. The integer
/// presentation is written by <see cref="IntegerFormatter"/>. The output does not depend on the culture or the time zone data of the
/// operating system.
/// </summary>
/// <remarks>
/// Limitations: English and the Gregorian calendar only. The week of the year, the week of the month, and the day of the week follow
/// the ISO 8601 rules (Monday is the first day) in every calendar. The year is written without its sign, the era says whether it is
/// before the common era. A time zone name is the offset in the form <c>GMT+hh:mm</c> (<c>GMT</c> for UTC), because no time zone data
/// is used, which the specification permits.
/// </remarks>
internal static class DateFormatter
{
    private static readonly string[] Months =
    [
        "January",
        "February",
        "March",
        "April",
        "May",
        "June",
        "July",
        "August",
        "September",
        "October",
        "November",
        "December",
    ];

    private static readonly string[] Days =
    [
        "Monday",
        "Tuesday",
        "Wednesday",
        "Thursday",
        "Friday",
        "Saturday",
        "Sunday",
    ];

    // The components of a time and of a date; a date and time has all of them, the time zone belongs to every type.
    private const string TimeComponents = "HhPmsf";
    private const string DateComponents = "YMDdFWwCE";

    /// <summary>Formats a value.</summary>
    /// <param name="value">The value.</param>
    /// <param name="picture">The parsed picture.</param>
    /// <param name="calendar"><c>ISO</c> or <c>AD</c>: the calendar that the component <c>C</c> writes.</param>
    /// <param name="xslt20">Whether the XSLT 2.0 error codes apply.</param>
    /// <returns>The text.</returns>
    /// <exception cref="Errors.XPathException">A component is not available for the type of the value (FOFD1350, XTDE1350).</exception>
    public static string Format(in XsdDateTime value, DatePicture picture, string calendar, bool xslt20)
    {
        var builder = new StringBuilder();
        foreach (var part in picture.Parts)
        {
            if (part is string literal)
            {
                _ = builder.Append(literal);
                continue;
            }

            var marker = (DatePicture.Marker)part;
            if (!IsAvailable(value.Kind, marker.Component))
            {
                throw DateFormatErrors.ComponentNotAvailable(xslt20, marker.Component, TypeName(value.Kind));
            }

            _ = builder.Append(Write(in value, marker, calendar));
        }

        return builder.ToString();
    }

    private static bool IsAvailable(XsdDateTimeKind kind, char component) =>
        component is 'Z' or 'z'
        || kind == XsdDateTimeKind.DateTime
        || (kind == XsdDateTimeKind.Date ? DateComponents : TimeComponents).AsSpan().IndexOf(component) >= 0;

    private static string TypeName(XsdDateTimeKind kind) =>
        kind switch
        {
            XsdDateTimeKind.Date => "xs:date",
            XsdDateTimeKind.Time => "xs:time",
            _ => "xs:dateTime",
        };

    private static string Write(in XsdDateTime value, DatePicture.Marker marker, string calendar)
    {
        switch (marker.Component)
        {
            case 'Y':
                return Year(value.Year, marker);
            case 'M':
                return marker.Name != DatePicture.NameStyle.None
                    ? Name(Months[value.Month - 1], marker)
                    : Number(value.Month, marker);
            case 'D':
                return Number(value.Day, marker);
            case 'd':
                return Number(DayOfYear(value.Year, value.Month, value.Day), marker);
            case 'F':
                var weekday = DayOfWeek(value.Year, value.Month, value.Day);
                return marker.Name != DatePicture.NameStyle.None
                    ? Name(Days[weekday - 1], marker)
                    : Number(weekday, marker);
            case 'W':
                return Number(WeekOfYear(value.Year, value.Month, value.Day), marker);
            case 'w':
                return Number(WeekOfMonth(value.Year, value.Month, value.Day), marker);
            case 'H':
                return Number(value.Hour, marker);
            case 'h':
                return Number(value.Hour % 12 == 0 ? 12 : value.Hour % 12, marker);
            case 'P':
                return Name(value.Hour < 12 ? "Am" : "Pm", marker);
            case 'm':
                return Number(value.Minute, marker);
            case 's':
                return Number(WholeSeconds(value), marker);
            case 'f':
                return Fraction(value, marker);
            case 'E':
                return Name(value.Year < 0 ? "BC" : "AD", marker);
            case 'C':
                return Name(calendar, marker);
            default:
                return Zone(value.TimezoneOffsetMinutes, marker);
        }
    }

    private static string Number(long value, DatePicture.Marker marker) =>
        IntegerFormatter.Format(value, marker.Integer!);

    // The year is written without its sign; a digit pattern with two or more digit signs and no width modifier keeps the rightmost digits.
    private static string Year(long year, DatePicture.Marker marker)
    {
        var magnitude = BigInteger.Abs(year);
        var picture = marker.Integer!;
        var max = marker.Max;
        if (!marker.HasWidth && picture.DigitSigns >= 2)
        {
            max = picture.DigitSigns;
        }

        if (picture.Sequence == IntegerSequence.Decimal && max > 0)
        {
            var limit = BigInteger.Pow(10, max);
            magnitude %= limit;
        }

        return IntegerFormatter.Format(magnitude, picture);
    }

    // The digits of the fraction; the digit pattern or the width modifier sets how many are written.
    private static string Fraction(in XsdDateTime value, DatePicture.Marker marker)
    {
        var text = value.Second.ToString();
        var dot = text.AsSpan().IndexOf('.');
        var digits = dot < 0 ? "0" : text.Substring(dot + 1);
        var picture = marker.Integer!;
        var decimalPicture = picture.Sequence == IntegerSequence.Decimal;
        int min = marker.Min,
            max = marker.Max;
        if (decimalPicture && !marker.HasWidth && picture.DigitSigns >= 2)
        {
            min = picture.MinimumDigits;
            max = picture.DigitSigns;
        }

        if (max > 0 && digits.Length > max)
        {
            digits = digits.Substring(0, max);
        }

        digits = digits.PadRight(min, '0');
        if (!decimalPicture || picture.ZeroDigit == '0')
        {
            return digits;
        }

        var builder = new StringBuilder(digits.Length * 2);
        foreach (var c in digits)
        {
            _ = builder.Append(char.ConvertFromUtf32(picture.ZeroDigit + c - '0'));
        }

        return builder.ToString();
    }

    private static long WholeSeconds(in XsdDateTime value)
    {
        var text = value.Second.ToString();
        var dot = text.AsSpan().IndexOf('.');
        return long.Parse(dot < 0 ? text : text.Substring(0, dot), CultureInfo.InvariantCulture);
    }

    // A name longer than the maximum width is abbreviated to three letters and cut to the width.
    private static string Name(string name, DatePicture.Marker marker)
    {
        if (marker.Max > 0 && name.Length > marker.Max)
        {
            name = name.Substring(0, Math.Min(3, name.Length));
            if (name.Length > marker.Max)
            {
                name = name.Substring(0, marker.Max);
            }
        }

        return marker.Name switch
        {
            DatePicture.NameStyle.Upper => name.ToUpperInvariant(),
            DatePicture.NameStyle.Lower => Lower(name),
            _ => name,
        };
    }

    // The names are ASCII, so the culture never matters.
    private static string Lower(string text)
    {
        var chars = text.ToCharArray();
        for (var i = 0; i < chars.Length; i++)
        {
            if (chars[i] is >= 'A' and <= 'Z')
            {
                chars[i] = (char)(chars[i] + 32);
            }
        }

        return new string(chars);
    }

    private static string Zone(int? offset, DatePicture.Marker marker)
    {
        if (offset is not { } minutes)
        {
            return marker.Military ? "J" : string.Empty;
        }

        var prefix = marker.Component == 'z' || marker.Name != DatePicture.NameStyle.None ? "GMT" : string.Empty;
        if (marker.Name != DatePicture.NameStyle.None && minutes == 0)
        {
            return "GMT";
        }

        if (marker.Military && minutes % 60 == 0 && Math.Abs(minutes) <= 12 * 60)
        {
            return MilitaryZone(minutes / 60);
        }

        if (marker.Traditional && minutes == 0 && marker.Component == 'Z')
        {
            return "Z";
        }

        var pattern = marker.Name != DatePicture.NameStyle.None ? DatePicture.ZonePattern.Parse("01:01") : marker.Zone!;
        return prefix + Offset(minutes, pattern);
    }

    // Z for UTC, A to I and K to M for +1 to +12 (J is local time), N to Y for -1 to -12.
    private static string MilitaryZone(int hours) =>
        hours switch
        {
            0 => "Z",
            > 0 and <= 9 => ((char)('A' + hours - 1)).ToString(),
            > 9 => ((char)('K' + hours - 10)).ToString(),
            _ => ((char)('N' - hours - 1)).ToString(),
        };

    private static string Offset(int minutes, DatePicture.ZonePattern pattern)
    {
        var magnitude = Math.Abs(minutes);
        var builder = new StringBuilder(10);
        _ = builder.Append(minutes < 0 ? '-' : '+');
        AppendDigits(builder, magnitude / 60, pattern.HoursWidth, pattern.Zero);
        if (magnitude % 60 != 0 || pattern.MinutesRequired)
        {
            _ = builder.Append(pattern.Separator);
            AppendDigits(builder, magnitude % 60, 2, pattern.Zero);
        }

        return builder.ToString();
    }

    private static void AppendDigits(StringBuilder builder, int value, int width, int zero)
    {
        var digits = value.ToString(CultureInfo.InvariantCulture).PadLeft(width, '0');
        foreach (var c in digits)
        {
            _ = builder.Append(char.ConvertFromUtf32(zero + c - '0'));
        }
    }

    private static int DaysInMonth(long year, int month) =>
        month switch
        {
            2 => IsLeap(year) ? 29 : 28,
            4 or 6 or 9 or 11 => 30,
            _ => 31,
        };

    private static bool IsLeap(long year) => year % 4 == 0 && (year % 100 != 0 || year % 400 == 0);

    private static int DayOfYear(long year, int month, int day)
    {
        var total = day;
        for (var m = 1; m < month; m++)
        {
            total += DaysInMonth(year, m);
        }

        return total;
    }

    // The days since 1970-01-01 of a proleptic Gregorian date.
    private static long DaysFromCivil(long year, int month, int day)
    {
        year -= month <= 2 ? 1 : 0;
        var era = (year >= 0 ? year : year - 399) / 400;
        var yearOfEra = year - (era * 400);
        var dayOfYear = ((153 * (month + (month > 2 ? -3 : 9))) + 2) / 5 + day - 1;
        var dayOfEra = (yearOfEra * 365) + (yearOfEra / 4) - (yearOfEra / 100) + dayOfYear;
        return (era * 146097) + dayOfEra - 719468;
    }

    // Monday is 1 and Sunday is 7; 1970-01-01 was a Thursday.
    private static int DayOfWeek(long year, int month, int day)
    {
        var days = DaysFromCivil(year, month, day);
        return (int)((((days % 7) + 7 + 3) % 7) + 1);
    }

    private static int WeeksInYear(long year) => (DayOfYear(year, 12, 28) - DayOfWeek(year, 12, 28) + 10) / 7;

    private static int WeekOfYear(long year, int month, int day)
    {
        var week = (DayOfYear(year, month, day) - DayOfWeek(year, month, day) + 10) / 7;
        if (week < 1)
        {
            return WeeksInYear(year - 1);
        }

        return week > WeeksInYear(year) ? 1 : week;
    }

    // Week 1 of a month is the first week, starting on Monday, with at least four days in the month; the days before it belong to the
    // last week of the previous month, and the last days of a month that start a week of the next month stay in this month's last week.
    private static int WeekOfMonth(long year, int month, int day)
    {
        var first = DayOfWeek(year, month, 1);
        var start = 8 - first >= 4 ? 2 - first : 9 - first;
        if (day >= start)
        {
            return ((day - start) / 7) + 1;
        }

        var previousMonth = month == 1 ? 12 : month - 1;
        var previousYear = month == 1 ? year - 1 : year;
        return WeekOfMonth(previousYear, previousMonth, DaysInMonth(previousYear, previousMonth));
    }
}
