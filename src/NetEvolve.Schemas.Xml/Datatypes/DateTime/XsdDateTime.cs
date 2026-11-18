namespace NetEvolve.Schemas.Xml.Datatypes.DateTime;

using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Decimals;

/// <summary>
/// A value of <c>xs:dateTime</c>, <c>xs:date</c>, or <c>xs:time</c> on the seven-property model: year, month, day, hour, minute,
/// second (a decimal), and an optional timezone offset. The properties are exact, so the year range, the fraction precision,
/// and the missing timezone of the value space are all kept.
/// </summary>
/// <remarks>
/// Limitations: a year has at most 17 digits (DT0011); a fraction is capped by the digit cap of <see cref="XsdDecimal"/> (DT0010);
/// leap days are decided by the year number, so in version 1.0 the year <c>-0001</c> follows the same rule as in version 1.1.
/// The timeline order, equality across timezones, and duration arithmetic live in the ordering namespace (DT-54).
/// The value of <c>date</c> has a zero time of day and the value of <c>time</c> has zero year, month, and day.
/// </remarks>
[StructLayout(LayoutKind.Auto)]
public readonly struct XsdDateTime : IEquatable<XsdDateTime>
{
    private const int MaxYearDigits = 17;
    private const int MaxOffsetMinutes = 14 * 60;

    private readonly int _offset;
    private readonly bool _hasOffset;

    private XsdDateTime(
        XsdDateTimeKind kind,
        long year,
        int month,
        int day,
        int hour,
        int minute,
        XsdDecimal second,
        bool hasOffset,
        int offset
    )
    {
        Kind = kind;
        Year = year;
        Month = month;
        Day = day;
        Hour = hour;
        Minute = minute;
        Second = second;
        _hasOffset = hasOffset;
        _offset = offset;
    }

    /// <summary>Creates a value from already validated properties; the caller keeps the properties in range.</summary>
    internal static XsdDateTime Create(
        XsdDateTimeKind kind,
        long year,
        int month,
        int day,
        int hour,
        int minute,
        XsdDecimal second,
        int? offset
    ) => new(kind, year, month, day, hour, minute, second, offset.HasValue, offset.GetValueOrDefault());

    /// <summary>Gets the type the value belongs to.</summary>
    public XsdDateTimeKind Kind { get; }

    /// <summary>Gets the year; negative before the common era, and zero for a <c>time</c>.</summary>
    public long Year { get; }

    /// <summary>Gets the month, 1 to 12; zero for a <c>time</c>.</summary>
    public int Month { get; }

    /// <summary>Gets the day of the month; zero for a <c>time</c>.</summary>
    public int Day { get; }

    /// <summary>Gets the hour, 0 to 23; <c>24:00:00</c> is already mapped to the next day.</summary>
    public int Hour { get; }

    /// <summary>Gets the minute, 0 to 59.</summary>
    public int Minute { get; }

    /// <summary>Gets the second, at least 0 and less than 60, with the fraction kept exactly.</summary>
    public XsdDecimal Second { get; }

    /// <summary>Gets the timezone offset in minutes, <see langword="null"/> when the value has no timezone.</summary>
    public int? TimezoneOffsetMinutes => _hasOffset ? _offset : null;

    /// <summary>Parses a lexical form of one of the three types; the form must already be whitespace-collapsed.</summary>
    /// <param name="lexical">The lexical form.</param>
    /// <param name="kind">The type whose lexical space applies.</param>
    /// <param name="version">The XSD version; year 0 exists only in <see cref="XsdVersion.V11"/>.</param>
    /// <returns>The value, or the error with the offset.</returns>
    public static ParseResult<XsdDateTime> Parse(ReadOnlySpan<char> lexical, XsdDateTimeKind kind, XsdVersion version)
    {
        var pos = 0;
        var leading = ScanLeading(
            lexical,
            kind,
            version,
            ref pos,
            out var year,
            out var month,
            out var day,
            out var at
        );
        if (leading != DatatypeError.None)
        {
            return new(leading, at);
        }

        var hour = 0;
        var minute = 0;
        var second = XsdDecimal.Zero;
        if (kind is XsdDateTimeKind.DateTime or XsdDateTimeKind.Time)
        {
            var error = ScanTime(lexical, ref pos, out hour, out minute, out second, out var errorOffset);
            if (error != DatatypeError.None)
            {
                return new(error, errorOffset);
            }
        }

        var hasOffset = false;
        var offset = 0;
        if (pos < lexical.Length)
        {
            if (!ScanOffset(lexical, ref pos, out offset) || pos != lexical.Length)
            {
                return new(DatatypeError.InvalidLexicalForm, pos);
            }

            hasOffset = true;
        }

        if (hour == 24)
        {
            hour = 0;
            if (kind == XsdDateTimeKind.DateTime)
            {
                NextDay(version, ref year, ref month, ref day);
            }
        }

        return new(new XsdDateTime(kind, year, month, day, hour, minute, second, hasOffset, offset));
    }

    /// <summary>Gets the canonical lexical form: the year with at least four digits, the shortest fraction, and <c>Z</c> for a zero offset.</summary>
    /// <returns>The canonical form.</returns>
    public override string ToString()
    {
        var builder = new StringBuilder(32);
        AppendDate(builder);
        if (Kind is XsdDateTimeKind.DateTime or XsdDateTimeKind.Time)
        {
            _ = AppendTwo(builder, Hour).Append(':');
            _ = AppendTwo(builder, Minute).Append(':');
            var second = Second.ToString();
            var fraction = Second.FractionDigits;
            var wholeDigits = second.Length - (fraction > 0 ? fraction + 1 : 0);
            _ = builder.Append('0', wholeDigits == 1 ? 1 : 0).Append(second);
        }

        AppendOffset(builder);
        return builder.ToString();
    }

    private void AppendDate(StringBuilder builder)
    {
        if (Kind >= XsdDateTimeKind.GMonth)
        {
            _ = builder.Append(Kind == XsdDateTimeKind.GDay ? "---" : "--");
            if (Kind != XsdDateTimeKind.GDay)
            {
                _ = AppendTwo(builder, Month);
            }

            if (Kind == XsdDateTimeKind.GMonthDay)
            {
                _ = builder.Append('-');
            }

            if (Kind != XsdDateTimeKind.GMonth)
            {
                _ = AppendTwo(builder, Day);
            }
        }
        else if (Kind != XsdDateTimeKind.Time)
        {
            if (Year < 0)
            {
                _ = builder.Append('-');
            }

            var year = Math.Abs(Year).ToString(CultureInfo.InvariantCulture);
            _ = builder.Append('0', Math.Max(0, 4 - year.Length)).Append(year);
            if (Kind != XsdDateTimeKind.GYear)
            {
                _ = AppendTwo(builder.Append('-'), Month);
            }

            if (Kind is XsdDateTimeKind.DateTime or XsdDateTimeKind.Date)
            {
                _ = AppendTwo(builder.Append('-'), Day);
            }

            if (Kind == XsdDateTimeKind.DateTime)
            {
                _ = builder.Append('T');
            }
        }
    }

    private void AppendOffset(StringBuilder builder)
    {
        if (_hasOffset)
        {
            if (_offset == 0)
            {
                _ = builder.Append('Z');
            }
            else
            {
                var magnitude = Math.Abs(_offset);
                _ = builder.Append(_offset < 0 ? '-' : '+');
                _ = AppendTwo(builder, magnitude / 60).Append(':');
                _ = AppendTwo(builder, magnitude % 60);
            }
        }
    }

    /// <inheritdoc />
    public bool Equals(XsdDateTime other) =>
        Kind == other.Kind
        && Year == other.Year
        && Month == other.Month
        && Day == other.Day
        && Hour == other.Hour
        && Minute == other.Minute
        && Second.Equals(other.Second)
        && _hasOffset == other._hasOffset
        && _offset == other._offset;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is XsdDateTime other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hash = ((int)Kind * 397) ^ Year.GetHashCode();
        hash = unchecked((hash * 397) ^ ((Month * 31) + Day));
        hash = unchecked((hash * 397) ^ ((Hour * 60) + Minute));
        hash = unchecked((hash * 397) ^ Second.GetHashCode());
        return unchecked((hash * 397) ^ (_hasOffset ? _offset : int.MinValue));
    }

    /// <summary>Determines whether two values are identical.</summary>
    /// <param name="left">The first value.</param>
    /// <param name="right">The second value.</param>
    /// <returns><see langword="true"/> when identical.</returns>
    public static bool operator ==(XsdDateTime left, XsdDateTime right) => left.Equals(right);

    /// <summary>Determines whether two values differ.</summary>
    /// <param name="left">The first value.</param>
    /// <param name="right">The second value.</param>
    /// <returns><see langword="true"/> when different.</returns>
    public static bool operator !=(XsdDateTime left, XsdDateTime right) => !left.Equals(right);

    private static StringBuilder AppendTwo(StringBuilder builder, int value) =>
        builder.Append((char)('0' + (value / 10))).Append((char)('0' + (value % 10)));

    private static bool IsDigit(ReadOnlySpan<char> text, int pos) =>
        pos < text.Length && text[pos] is >= '0' and <= '9';

    private static bool TwoDigits(ReadOnlySpan<char> text, ref int pos, out int value)
    {
        if (IsDigit(text, pos) && IsDigit(text, pos + 1))
        {
            value = ((text[pos] - '0') * 10) + (text[pos + 1] - '0');
            pos += 2;
            return true;
        }

        value = 0;
        return false;
    }

    private static bool Expect(ReadOnlySpan<char> text, ref int pos, char c)
    {
        if (pos < text.Length && text[pos] == c)
        {
            pos++;
            return true;
        }

        return false;
    }

    internal static bool IsLeapYear(long year) => year % 4 == 0 && (year % 100 != 0 || year % 400 == 0);

    internal static int DaysInMonth(long year, int month) =>
        month switch
        {
            2 => IsLeapYear(year) ? 29 : 28,
            4 or 6 or 9 or 11 => 30,
            _ => 31,
        };

    private static DatatypeError ScanLeading(
        ReadOnlySpan<char> text,
        XsdDateTimeKind kind,
        XsdVersion version,
        ref int pos,
        out long year,
        out int month,
        out int day,
        out int errorOffset
    )
    {
        year = 0;
        month = 0;
        day = 0;
        errorOffset = 0;
        if (kind >= XsdDateTimeKind.GYear)
        {
            return ScanPartial(text, kind, version, ref pos, out year, out month, out day, out errorOffset);
        }

        if (kind == XsdDateTimeKind.Time)
        {
            return DatatypeError.None;
        }

        var error = ScanDate(text, version, ref pos, out year, out month, out day, out errorOffset);
        if (error == DatatypeError.None && kind == XsdDateTimeKind.DateTime && !Expect(text, ref pos, 'T'))
        {
            errorOffset = pos;
            return DatatypeError.InvalidLexicalForm;
        }

        return error;
    }

    private static DatatypeError ScanPartial(
        ReadOnlySpan<char> text,
        XsdDateTimeKind kind,
        XsdVersion version,
        ref int pos,
        out long year,
        out int month,
        out int day,
        out int errorOffset
    )
    {
        year = 0;
        month = 0;
        day = 0;
        errorOffset = 0;
        bool valid;
        if (kind is XsdDateTimeKind.GYear or XsdDateTimeKind.GYearMonth)
        {
            var error = ScanYear(text, version, ref pos, out year, out errorOffset);
            if (error != DatatypeError.None)
            {
                return error;
            }

            valid =
                kind == XsdDateTimeKind.GYear
                || (Expect(text, ref pos, '-') && TwoDigits(text, ref pos, out month) && month is >= 1 and <= 12);
        }
        else if (kind == XsdDateTimeKind.GDay)
        {
            valid =
                Expect(text, ref pos, '-')
                && Expect(text, ref pos, '-')
                && Expect(text, ref pos, '-')
                && TwoDigits(text, ref pos, out day)
                && day is >= 1 and <= 31;
        }
        else
        {
            valid =
                Expect(text, ref pos, '-')
                && Expect(text, ref pos, '-')
                && TwoDigits(text, ref pos, out month)
                && month is >= 1 and <= 12;
            if (valid && kind == XsdDateTimeKind.GMonthDay)
            {
                // Year 2000 is a leap year, so --02-29 is accepted and --02-30 is not.
                valid =
                    Expect(text, ref pos, '-')
                    && TwoDigits(text, ref pos, out day)
                    && day >= 1
                    && day <= DaysInMonth(2000, month);
            }
        }

        if (valid)
        {
            return DatatypeError.None;
        }

        errorOffset = pos;
        return DatatypeError.InvalidLexicalForm;
    }

    private static DatatypeError ScanYear(
        ReadOnlySpan<char> text,
        XsdVersion version,
        ref int pos,
        out long year,
        out int errorOffset
    )
    {
        year = 0;
        errorOffset = 0;
        var negative = Expect(text, ref pos, '-');
        var start = pos;
        while (IsDigit(text, pos))
        {
            if (pos - start == MaxYearDigits)
            {
                errorOffset = pos;
                return DatatypeError.YearOrComponentCap;
            }

            year = (year * 10) + (text[pos] - '0');
            pos++;
        }

        var digits = pos - start;
        if (digits < 4 || (digits > 4 && text[start] == '0') || (year == 0 && version == XsdVersion.V10))
        {
            errorOffset = start;
            return DatatypeError.InvalidLexicalForm;
        }

        if (negative)
        {
            year = -year;
        }

        return DatatypeError.None;
    }

    private static DatatypeError ScanDate(
        ReadOnlySpan<char> text,
        XsdVersion version,
        ref int pos,
        out long year,
        out int month,
        out int day,
        out int errorOffset
    )
    {
        month = 0;
        day = 0;
        var yearError = ScanYear(text, version, ref pos, out year, out errorOffset);
        if (yearError != DatatypeError.None)
        {
            return yearError;
        }

        if (!Expect(text, ref pos, '-') || !TwoDigits(text, ref pos, out month) || month is < 1 or > 12)
        {
            errorOffset = pos;
            return DatatypeError.InvalidLexicalForm;
        }

        if (
            !Expect(text, ref pos, '-')
            || !TwoDigits(text, ref pos, out day)
            || day < 1
            || day > DaysInMonth(year, month)
        )
        {
            errorOffset = pos;
            return DatatypeError.InvalidLexicalForm;
        }

        return DatatypeError.None;
    }

    private static DatatypeError ScanTime(
        ReadOnlySpan<char> text,
        ref int pos,
        out int hour,
        out int minute,
        out XsdDecimal second,
        out int errorOffset
    )
    {
        second = XsdDecimal.Zero;
        minute = 0;
        errorOffset = 0;
        if (
            !TwoDigits(text, ref pos, out hour)
            || !Expect(text, ref pos, ':')
            || !TwoDigits(text, ref pos, out minute)
            || !Expect(text, ref pos, ':')
        )
        {
            errorOffset = pos;
            return DatatypeError.InvalidLexicalForm;
        }

        var start = pos;
        if (!TwoDigits(text, ref pos, out var whole))
        {
            errorOffset = pos;
            return DatatypeError.InvalidLexicalForm;
        }

        // Only a fraction may extend the two whole-second digits; any further digit without a decimal point is left
        // for the caller to reject at its offset.
        if (Expect(text, ref pos, '.'))
        {
            if (!IsDigit(text, pos))
            {
                errorOffset = pos;
                return DatatypeError.InvalidLexicalForm;
            }

            while (IsDigit(text, pos))
            {
                pos++;
            }
        }

        var parsed = XsdDecimal.Parse(text.Slice(start, pos - start));
        if (!parsed.Success)
        {
            errorOffset = start + parsed.ErrorOffset;
            return parsed.Error;
        }

        second = parsed.Value;
        if (minute > 59 || whole > 59 || hour > 24 || (hour == 24 && (minute != 0 || second.Sign != 0)))
        {
            errorOffset = start;
            return DatatypeError.InvalidLexicalForm;
        }

        return DatatypeError.None;
    }

    private static bool ScanOffset(ReadOnlySpan<char> text, ref int pos, out int offset)
    {
        offset = 0;
        if (Expect(text, ref pos, 'Z'))
        {
            return true;
        }

        var negative = text[pos] == '-';
        if (!negative && text[pos] != '+')
        {
            return false;
        }

        pos++;
        if (
            !TwoDigits(text, ref pos, out var hours)
            || !Expect(text, ref pos, ':')
            || !TwoDigits(text, ref pos, out var minutes)
        )
        {
            return false;
        }

        var total = (hours * 60) + minutes;
        if (minutes > 59 || total > MaxOffsetMinutes)
        {
            return false;
        }

        offset = negative ? -total : total;
        return true;
    }

    private static void NextDay(XsdVersion version, ref long year, ref int month, ref int day)
    {
        if (++day <= DaysInMonth(year, month))
        {
            return;
        }

        day = 1;
        if (++month <= 12)
        {
            return;
        }

        month = 1;

        // Version 1.0 has no year 0: the year after -0001 is 0001.
        year = year == -1 && version == XsdVersion.V10 ? 1 : year + 1;
    }
}
