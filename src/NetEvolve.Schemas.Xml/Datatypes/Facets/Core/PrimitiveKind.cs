namespace NetEvolve.Schemas.Xml.Datatypes.Facets.Core;

/// <summary>The primitive datatype that a derived atomic type ultimately restricts; the key of the applicability table (XSD 1.1 Part 2, section 4.1.5).</summary>
/// <remarks>Limitation: <c>precisionDecimal</c> is not listed; it is a deferred feature.</remarks>
public enum PrimitiveKind
{
    /// <summary>No primitive: lists and unions, which have no primitive ancestor.</summary>
    None = 0,

    /// <summary><see cref="string"/>.</summary>
    StringType,

    /// <summary><c>boolean</c>.</summary>
    Boolean,

    /// <summary><see cref="decimal"/>.</summary>
    DecimalType,

    /// <summary><see cref="float"/>.</summary>
    FloatType,

    /// <summary><see cref="double"/>.</summary>
    DoubleType,

    /// <summary><c>duration</c>.</summary>
    Duration,

    /// <summary><c>dateTime</c>.</summary>
    DateTime,

    /// <summary><c>time</c>.</summary>
    Time,

    /// <summary><c>date</c>.</summary>
    Date,

    /// <summary><c>gYearMonth</c>.</summary>
    GYearMonth,

    /// <summary><c>gYear</c>.</summary>
    GYear,

    /// <summary><c>gMonthDay</c>.</summary>
    GMonthDay,

    /// <summary><c>gDay</c>.</summary>
    GDay,

    /// <summary><c>gMonth</c>.</summary>
    GMonth,

    /// <summary><c>hexBinary</c>.</summary>
    HexBinary,

    /// <summary><c>base64Binary</c>.</summary>
    Base64Binary,

    /// <summary><c>anyURI</c>.</summary>
    AnyUri,

    /// <summary><c>QName</c>.</summary>
    QName,

    /// <summary><c>NOTATION</c>.</summary>
    Notation,
}
