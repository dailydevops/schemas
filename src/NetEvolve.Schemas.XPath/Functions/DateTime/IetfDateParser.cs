namespace NetEvolve.Schemas.XPath.Functions.DateTime;

using System;
using System.Globalization;

/// <summary>
/// The grammar of <c>fn:parse-ietf-date</c> (Functions and Operators 3.1, 9.7.1), read from a span without the culture of the
/// base class library: an optional day name, then a date with a time or the <c>asctime</c> form. Names are case-insensitive, a
/// year of two digits is 19xx, a missing time zone is UTC. The result is the lexical form of an <c>xs:dateTime</c>; the range of
/// every component is checked by the parser of that type.
/// </summary>
internal static class IetfDateParser
{
    // The long names first, so that Monday is not read as Mon.
    private static readonly string[] DayNames =
    [
        "Monday",
        "Tuesday",
        "Wednesday",
        "Thursday",
        "Friday",
        "Saturday",
        "Sunday",
        "Mon",
        "Tue",
        "Wed",
        "Thu",
        "Fri",
        "Sat",
        "Sun",
    ];

    private static readonly string[] MonthNames =
    [
        "Jan",
        "Feb",
        "Mar",
        "Apr",
        "May",
        "Jun",
        "Jul",
        "Aug",
        "Sep",
        "Oct",
        "Nov",
        "Dec",
    ];

    // The longest name first, so that UTC is not read as UT.
    private static readonly string[] ZoneNames =
    [
        "UTC",
        "GMT",
        "UT",
        "EST",
        "EDT",
        "CST",
        "CDT",
        "MST",
        "MDT",
        "PST",
        "PDT",
    ];

    private static readonly int[] ZoneHours = [0, 0, 0, -5, -4, -6, -5, -7, -6, -8, -7];

    /// <summary>Reads the input.</summary>
    /// <param name="input">The text.</param>
    /// <param name="lexical">The lexical form of the <c>xs:dateTime</c>, with a time zone.</param>
    /// <returns><see langword="true"/> when the input matches the grammar.</returns>
    internal static bool TryParse(ReadOnlySpan<char> input, out string lexical)
    {
        lexical = string.Empty;
        var pos = 0;
        _ = SkipSpace(input, ref pos);
        if (TryName(input, ref pos, DayNames, out _))
        {
            _ = Skip(input, ref pos, ',');
            if (!SkipSpace(input, ref pos))
            {
                return false;
            }
        }

        int day,
            month,
            year;
        string time;
        if (pos < input.Length && IsDigit(input[pos]))
        {
            if (
                !TryDigits(input, ref pos, 1, 2, out day)
                || !SkipSeparator(input, ref pos)
                || !TryName(input, ref pos, MonthNames, out month)
                || !SkipSeparator(input, ref pos)
                || !TryYear(input, ref pos, out year)
                || !SkipSpace(input, ref pos)
                || !TryTime(input, ref pos, out time)
            )
            {
                return false;
            }
        }
        else if (
            !TryName(input, ref pos, MonthNames, out month)
            || !SkipSeparator(input, ref pos)
            || !TryDigits(input, ref pos, 1, 2, out day)
            || !SkipSpace(input, ref pos)
            || !TryTime(input, ref pos, out time)
            || !SkipSpace(input, ref pos)
            || !TryYear(input, ref pos, out year)
        )
        {
            return false;
        }

        _ = SkipSpace(input, ref pos);
        if (pos != input.Length)
        {
            return false;
        }

        lexical = string.Format(CultureInfo.InvariantCulture, "{0:D4}-{1:D2}-{2:D2}T{3}", year, month + 1, day, time);
        return true;
    }

    // time ::= hours ":" minutes (":" seconds)? (S? timezone)?
    private static bool TryTime(ReadOnlySpan<char> s, ref int pos, out string time)
    {
        time = string.Empty;
        var seconds = "00";
        var fraction = string.Empty;
        if (
            !TryDigits(s, ref pos, 1, 2, out var hours)
            || !Skip(s, ref pos, ':')
            || !TryDigits(s, ref pos, 2, 2, out var minutes)
        )
        {
            return false;
        }

        if (Skip(s, ref pos, ':'))
        {
            var start = pos;
            if (!TryDigits(s, ref pos, 2, 2, out _))
            {
                return false;
            }

            seconds = s.Slice(start, 2).ToString();
            if (pos + 1 < s.Length && s[pos] == '.' && IsDigit(s[pos + 1]))
            {
                var fractionStart = pos;
                pos++;
                while (pos < s.Length && IsDigit(s[pos]))
                {
                    pos++;
                }

                fraction = s.Slice(fractionStart, pos - fractionStart).ToString();
            }
        }

        var zone = "Z";
        var save = pos;
        _ = SkipSpace(s, ref pos);
        if (pos < s.Length && (s[pos] is '+' or '-' || IsLetter(s[pos])))
        {
            if (!TryZone(s, ref pos, out zone))
            {
                return false;
            }
        }
        else
        {
            pos = save;
        }

        time = string.Format(
            CultureInfo.InvariantCulture,
            "{0:D2}:{1:D2}:{2}{3}{4}",
            hours,
            minutes,
            seconds,
            fraction,
            zone
        );
        return true;
    }

    // timezone ::= tzname | tzoffset (S? "(" S? tzname S? ")")?
    private static bool TryZone(ReadOnlySpan<char> s, ref int pos, out string zone)
    {
        zone = "Z";
        if (TryName(s, ref pos, ZoneNames, out var index))
        {
            zone = FormatOffset(Math.Abs(ZoneHours[index]), 0, ZoneHours[index] < 0);
            return true;
        }

        var negative = s[pos] == '-';
        if (!negative && s[pos] != '+')
        {
            return false;
        }

        pos++;
        var start = pos;
        if (!TryDigits(s, ref pos, 1, 4, out var value))
        {
            return false;
        }

        int hours,
            minutes;
        var count = pos - start;
        if (Skip(s, ref pos, ':'))
        {
            if (count > 2 || !TryDigits(s, ref pos, 2, 2, out minutes))
            {
                return false;
            }

            hours = value;
        }
        else if (count <= 2)
        {
            (hours, minutes) = (value, 0);
        }
        else
        {
            // tzhours is one digit when three digits follow; with four digits the hours have two.
            (hours, minutes) = (value / 100, value % 100);
        }

        zone = FormatOffset(hours, minutes, negative);
        var save = pos;
        _ = SkipSpace(s, ref pos);
        if (pos < s.Length && s[pos] == '(')
        {
            pos++;
            _ = SkipSpace(s, ref pos);
            if (!TryName(s, ref pos, ZoneNames, out _))
            {
                return false;
            }

            _ = SkipSpace(s, ref pos);
            return Skip(s, ref pos, ')');
        }

        pos = save;
        return true;
    }

    private static string FormatOffset(int hours, int minutes, bool negative) =>
        string.Format(CultureInfo.InvariantCulture, "{0}{1:D2}:{2:D2}", negative ? '-' : '+', hours, minutes);

    // year ::= digit digit (digit digit)?
    private static bool TryYear(ReadOnlySpan<char> s, ref int pos, out int year)
    {
        var start = pos;
        if (!TryDigits(s, ref pos, 2, 4, out year) || pos - start == 3 || (pos < s.Length && IsDigit(s[pos])))
        {
            return false;
        }

        if (pos - start == 2)
        {
            year += 1900;
        }

        return true;
    }

    // Reads min..max digits and stops at the first character that is not a digit.
    private static bool TryDigits(ReadOnlySpan<char> s, ref int pos, int min, int max, out int value)
    {
        value = 0;
        var count = 0;
        while (count < max && pos + count < s.Length && IsDigit(s[pos + count]))
        {
            value = (value * 10) + (s[pos + count] - '0');
            count++;
        }

        if (count < min)
        {
            return false;
        }

        pos += count;
        return true;
    }

    // Matches one of the names, ignoring case; the index is that of the name in the list.
    private static bool TryName(ReadOnlySpan<char> s, ref int pos, string[] names, out int index)
    {
        for (index = 0; index < names.Length; index++)
        {
            if (s.Slice(pos).StartsWith(names[index].AsSpan(), StringComparison.OrdinalIgnoreCase))
            {
                pos += names[index].Length;
                return true;
            }
        }

        return false;
    }

    // dsep ::= S | (S? "-" S?)
    private static bool SkipSeparator(ReadOnlySpan<char> s, ref int pos)
    {
        var space = SkipSpace(s, ref pos);
        var dash = Skip(s, ref pos, '-');
        if (dash)
        {
            _ = SkipSpace(s, ref pos);
        }

        return space || dash;
    }

    private static bool SkipSpace(ReadOnlySpan<char> s, ref int pos)
    {
        var start = pos;
        while (pos < s.Length && s[pos] is ' ' or '\t' or '\n' or '\r')
        {
            pos++;
        }

        return pos > start;
    }

    private static bool Skip(ReadOnlySpan<char> s, ref int pos, char c)
    {
        if (pos < s.Length && s[pos] == c)
        {
            pos++;
            return true;
        }

        return false;
    }

    private static bool IsDigit(char c) => c is >= '0' and <= '9';

    private static bool IsLetter(char c) => c is >= 'a' and <= 'z' or >= 'A' and <= 'Z';
}
