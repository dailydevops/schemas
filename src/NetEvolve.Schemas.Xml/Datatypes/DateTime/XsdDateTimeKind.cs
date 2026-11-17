namespace NetEvolve.Schemas.Xml.Datatypes.DateTime;

/// <summary>Selects which of the primitive types <c>dateTime</c>, <c>date</c>, and <c>time</c> an <see cref="XsdDateTime"/> value belongs to.</summary>
public enum XsdDateTimeKind
{
    /// <summary><c>xs:dateTime</c>: all seven properties.</summary>
    DateTime = 0,

    /// <summary><c>xs:date</c>: year, month, day, and the timezone; the time of day is zero.</summary>
    Date = 1,

    /// <summary><c>xs:time</c>: hour, minute, second, and the timezone; year, month, and day are zero.</summary>
    Time = 2,

    /// <summary><c>xs:gYear</c>: the year and the timezone; month, day, and the time of day are absent (zero).</summary>
    GYear = 3,

    /// <summary><c>xs:gYearMonth</c>: year, month, and the timezone; day and the time of day are absent (zero).</summary>
    GYearMonth = 4,

    /// <summary><c>xs:gMonth</c>: the month and the timezone; year, day, and the time of day are absent (zero).</summary>
    GMonth = 5,

    /// <summary><c>xs:gMonthDay</c>: month, day, and the timezone; the year and the time of day are absent (zero).</summary>
    GMonthDay = 6,

    /// <summary><c>xs:gDay</c>: the day and the timezone; year, month, and the time of day are absent (zero).</summary>
    GDay = 7,
}
