namespace NetEvolve.Schemas.XPath.Casting.Temporal;

using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Core;
using NetEvolve.Schemas.Xml.Datatypes.DateTime;
using NetEvolve.Schemas.Xml.Datatypes.Decimals;
using NetEvolve.Schemas.Xml.Datatypes.Duration;
using NetEvolve.Schemas.XPath.Casting.Core;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The conversions of the rows for the date, time, Gregorian, and duration types (Functions and Operators, 17.1.2 to 17.1.4). There is one
/// conversion per target type; the source type is told by the item. A failure is a <see cref="CastStatus"/>, never an exception.
/// </summary>
/// <remarks>
/// <para>
/// A lexical form goes through the built-in datatype of the target, which collapses whitespace and checks the lexical space by the XSD 1.0
/// rules, as XPath 2.0 refers to XML Schema 1.0: the year 0000 is not a year, and <c>24:00:00</c> is the start of the next day. The two
/// derived duration types come from the XSD 1.1 registry, which is the only one that defines them; their lexical spaces are the ones of XPath
/// 2.0. A year or a duration component beyond 17 digits, or a fraction beyond the digit cap of the decimal, is an implementation limit
/// (FODT0001 and FODT0002), not an invalid lexical form.
/// </para>
/// <para>
/// A cast between two date and time types keeps the properties that both types have and drops or zeroes the others; the timezone is kept
/// as it is, so a value without a timezone stays without one. A cast between two duration types keeps the months for a type with a year-month
/// part and the seconds for a type with a day-time part, and zeroes the other.
/// </para>
/// </remarks>
internal static class TemporalConversions
{
    private static readonly DatatypeRegistry V10 = BuiltInDatatypes.For(XsdVersion.V10);
    private static readonly DatatypeRegistry V11 = BuiltInDatatypes.For(XsdVersion.V11);

    private static readonly IDatatypeDescriptor<XsdDateTime> DateTimeType = Temporal(V10, "dateTime");
    private static readonly IDatatypeDescriptor<XsdDateTime> DateType = Temporal(V10, "date");
    private static readonly IDatatypeDescriptor<XsdDateTime> TimeType = Temporal(V10, "time");
    private static readonly IDatatypeDescriptor<XsdDateTime> GYearMonthType = Temporal(V10, "gYearMonth");
    private static readonly IDatatypeDescriptor<XsdDateTime> GYearType = Temporal(V10, "gYear");
    private static readonly IDatatypeDescriptor<XsdDateTime> GMonthDayType = Temporal(V10, "gMonthDay");
    private static readonly IDatatypeDescriptor<XsdDateTime> GDayType = Temporal(V10, "gDay");
    private static readonly IDatatypeDescriptor<XsdDateTime> GMonthType = Temporal(V10, "gMonth");
    private static readonly IDatatypeDescriptor<XsdDuration> DurationType =
        (IDatatypeDescriptor<XsdDuration>)V10.Get("duration");
    private static readonly IDatatypeDescriptor<XsdDuration> YearMonthDurationType =
        (IDatatypeDescriptor<XsdDuration>)V11.Get("yearMonthDuration");
    private static readonly IDatatypeDescriptor<XsdDuration> DayTimeDurationType =
        (IDatatypeDescriptor<XsdDuration>)V11.Get("dayTimeDuration");

    /// <summary>Converts to <c>xs:dateTime</c>.</summary>
    /// <param name="source">The source value.</param>
    /// <param name="result">The result.</param>
    /// <returns>The status.</returns>
    public static CastStatus ToDateTime(in XdmItem source, out XdmItem result) =>
        ToTemporal(in source, XsdDateTimeKind.DateTime, DateTimeType, out result);

    /// <summary>Converts to <c>xs:date</c>.</summary>
    /// <param name="source">The source value.</param>
    /// <param name="result">The result.</param>
    /// <returns>The status.</returns>
    public static CastStatus ToDate(in XdmItem source, out XdmItem result) =>
        ToTemporal(in source, XsdDateTimeKind.Date, DateType, out result);

    /// <summary>Converts to <c>xs:time</c>.</summary>
    /// <param name="source">The source value.</param>
    /// <param name="result">The result.</param>
    /// <returns>The status.</returns>
    public static CastStatus ToTime(in XdmItem source, out XdmItem result) =>
        ToTemporal(in source, XsdDateTimeKind.Time, TimeType, out result);

    /// <summary>Converts to <c>xs:gYearMonth</c>.</summary>
    /// <param name="source">The source value.</param>
    /// <param name="result">The result.</param>
    /// <returns>The status.</returns>
    public static CastStatus ToGYearMonth(in XdmItem source, out XdmItem result) =>
        ToTemporal(in source, XsdDateTimeKind.GYearMonth, GYearMonthType, out result);

    /// <summary>Converts to <c>xs:gYear</c>.</summary>
    /// <param name="source">The source value.</param>
    /// <param name="result">The result.</param>
    /// <returns>The status.</returns>
    public static CastStatus ToGYear(in XdmItem source, out XdmItem result) =>
        ToTemporal(in source, XsdDateTimeKind.GYear, GYearType, out result);

    /// <summary>Converts to <c>xs:gMonthDay</c>.</summary>
    /// <param name="source">The source value.</param>
    /// <param name="result">The result.</param>
    /// <returns>The status.</returns>
    public static CastStatus ToGMonthDay(in XdmItem source, out XdmItem result) =>
        ToTemporal(in source, XsdDateTimeKind.GMonthDay, GMonthDayType, out result);

    /// <summary>Converts to <c>xs:gDay</c>.</summary>
    /// <param name="source">The source value.</param>
    /// <param name="result">The result.</param>
    /// <returns>The status.</returns>
    public static CastStatus ToGDay(in XdmItem source, out XdmItem result) =>
        ToTemporal(in source, XsdDateTimeKind.GDay, GDayType, out result);

    /// <summary>Converts to <c>xs:gMonth</c>.</summary>
    /// <param name="source">The source value.</param>
    /// <param name="result">The result.</param>
    /// <returns>The status.</returns>
    public static CastStatus ToGMonth(in XdmItem source, out XdmItem result) =>
        ToTemporal(in source, XsdDateTimeKind.GMonth, GMonthType, out result);

    /// <summary>Converts to <c>xs:duration</c>.</summary>
    /// <param name="source">The source value.</param>
    /// <param name="result">The result.</param>
    /// <returns>The status.</returns>
    public static CastStatus ToDuration(in XdmItem source, out XdmItem result) =>
        ToDurationType(in source, DurationKind.Duration, DurationType, out result);

    /// <summary>Converts to <c>xs:yearMonthDuration</c>.</summary>
    /// <param name="source">The source value.</param>
    /// <param name="result">The result.</param>
    /// <returns>The status.</returns>
    public static CastStatus ToYearMonthDuration(in XdmItem source, out XdmItem result) =>
        ToDurationType(in source, DurationKind.YearMonth, YearMonthDurationType, out result);

    /// <summary>Converts to <c>xs:dayTimeDuration</c>.</summary>
    /// <param name="source">The source value.</param>
    /// <param name="result">The result.</param>
    /// <returns>The status.</returns>
    public static CastStatus ToDayTimeDuration(in XdmItem source, out XdmItem result) =>
        ToDurationType(in source, DurationKind.DayTime, DayTimeDurationType, out result);

    /// <summary>Converts to <c>xs:string</c>.</summary>
    /// <param name="source">The source value, a date and time or duration value.</param>
    /// <param name="result">The result.</param>
    /// <returns>The status.</returns>
    public static CastStatus ToStringType(in XdmItem source, out XdmItem result)
    {
        if (TryFormat(in source, out var text))
        {
            result = XdmItem.FromString(text!);
            return CastStatus.Ok;
        }

        result = default;
        return CastStatus.NotAllowed;
    }

    /// <summary>Converts to <c>xs:untypedAtomic</c>.</summary>
    /// <param name="source">The source value, a date and time or duration value.</param>
    /// <param name="result">The result.</param>
    /// <returns>The status.</returns>
    public static CastStatus ToUntypedAtomic(in XdmItem source, out XdmItem result)
    {
        if (TryFormat(in source, out var text))
        {
            result = XdmItem.FromUntypedAtomic(text!);
            return CastStatus.Ok;
        }

        result = default;
        return CastStatus.NotAllowed;
    }

    /// <summary>Gets the canonical lexical form of a date and time or duration value.</summary>
    /// <param name="source">The item.</param>
    /// <param name="text">The canonical form; <see langword="null"/> when the item is neither.</param>
    /// <returns><see langword="true"/> when the item has one of these types.</returns>
    internal static bool TryFormat(in XdmItem source, out string? text)
    {
        if (source.TryGetTemporal(out var temporal))
        {
            text = temporal.ToString();
        }
        else if (source.TryGetDuration(out var duration))
        {
            text = duration.ToString(
                source.Kind == XdmItemKind.YearMonthDuration ? DurationKind.YearMonth : DurationKind.Duration
            );
        }
        else
        {
            text = null;
        }

        return text is not null;
    }

    private static IDatatypeDescriptor<XsdDateTime> Temporal(DatatypeRegistry registry, string name) =>
        (IDatatypeDescriptor<XsdDateTime>)registry.Get(name);

    private static CastStatus ToTemporal(
        in XdmItem source,
        XsdDateTimeKind target,
        IDatatypeDescriptor<XsdDateTime> type,
        out XdmItem result
    )
    {
        result = default;
        if (source.TryGetString(out var lexical) || source.TryGetUntypedAtomic(out lexical))
        {
            var parsed = type.Parse(lexical.AsSpan());
            if (!parsed.Success)
            {
                return Failure(parsed.Error, CastStatus.DateTimeOverflow);
            }

            result = XdmItem.FromTemporal(parsed.Value);
            return CastStatus.Ok;
        }

        if (!source.TryGetTemporal(out var value) || !Accepts(value.Kind, target))
        {
            return CastStatus.NotAllowed;
        }

        result = XdmItem.FromTemporal(Convert(in value, target));
        return CastStatus.Ok;
    }

    private static CastStatus ToDurationType(
        in XdmItem source,
        DurationKind target,
        IDatatypeDescriptor<XsdDuration> type,
        out XdmItem result
    )
    {
        result = default;
        if (source.TryGetString(out var lexical) || source.TryGetUntypedAtomic(out lexical))
        {
            var parsed = type.Parse(lexical.AsSpan());
            if (!parsed.Success)
            {
                return Failure(parsed.Error, CastStatus.DurationOverflow);
            }

            result = XdmItem.FromDuration(parsed.Value, target);
            return CastStatus.Ok;
        }

        if (!source.TryGetDuration(out var value))
        {
            return CastStatus.NotAllowed;
        }

        // A duration keeps both parts, a year-month type the months, a day-time type the seconds; the other part is zero.
        var months = target == DurationKind.DayTime ? 0 : value.Months;
        var seconds = target == DurationKind.YearMonth ? XsdDecimal.Zero : value.Seconds;
        result = XdmItem.FromDuration(new XsdDuration(months, seconds), target);
        return CastStatus.Ok;
    }

    // The table of Functions and Operators, 17.1: a dateTime reaches every date and time type, a date every one but time.
    private static bool Accepts(XsdDateTimeKind source, XsdDateTimeKind target) =>
        source == target
        || source == XsdDateTimeKind.DateTime
        || (source == XsdDateTimeKind.Date && target != XsdDateTimeKind.Time);

    private static XsdDateTime Convert(in XsdDateTime value, XsdDateTimeKind target)
    {
        if (value.Kind == target)
        {
            return value;
        }

        var offset = value.TimezoneOffsetMinutes;
        return target switch
        {
            XsdDateTimeKind.DateTime or XsdDateTimeKind.Date => XsdDateTime.Create(
                target,
                value.Year,
                value.Month,
                value.Day,
                0,
                0,
                XsdDecimal.Zero,
                offset
            ),
            XsdDateTimeKind.Time => XsdDateTime.Create(target, 0, 0, 0, value.Hour, value.Minute, value.Second, offset),
            XsdDateTimeKind.GYearMonth => XsdDateTime.Create(
                target,
                value.Year,
                value.Month,
                0,
                0,
                0,
                XsdDecimal.Zero,
                offset
            ),
            XsdDateTimeKind.GYear => XsdDateTime.Create(target, value.Year, 0, 0, 0, 0, XsdDecimal.Zero, offset),
            XsdDateTimeKind.GMonthDay => XsdDateTime.Create(
                target,
                0,
                value.Month,
                value.Day,
                0,
                0,
                XsdDecimal.Zero,
                offset
            ),
            XsdDateTimeKind.GDay => XsdDateTime.Create(target, 0, 0, value.Day, 0, 0, XsdDecimal.Zero, offset),
            _ => XsdDateTime.Create(target, 0, value.Month, 0, 0, 0, XsdDecimal.Zero, offset),
        };
    }

    private static CastStatus Failure(DatatypeError error, CastStatus overflow) =>
        error is DatatypeError.YearOrComponentCap or DatatypeError.DigitCap ? overflow : CastStatus.InvalidLexical;
}
