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
}
