namespace NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>The kind of an item: a node, one atomic family of the data model, or a function item.</summary>
/// <remarks>Limitation: the contract names every family, but the factories and accessors for decimal, date and time, and duration values arrive with XP-06 (#185).</remarks>
public enum XdmItemKind
{
    /// <summary>No item: the kind of <see langword="default"/>.</summary>
    None = 0,

    /// <summary>A node.</summary>
    Node = 1,

    /// <summary><c>xs:boolean</c>.</summary>
    Boolean = 2,

    /// <summary><c>xs:integer</c> and the types derived from it.</summary>
    XsInteger = 3,

    /// <summary><c>xs:decimal</c>.</summary>
    XsDecimal = 4,

    /// <summary><c>xs:double</c>.</summary>
    XsDouble = 5,

    /// <summary><c>xs:float</c>.</summary>
    XsFloat = 6,

    /// <summary><c>xs:string</c> and the types derived from it.</summary>
    XsString = 7,

    /// <summary><c>xs:untypedAtomic</c>.</summary>
    UntypedAtomic = 8,

    /// <summary><c>xs:anyURI</c>.</summary>
    AnyUri = 9,

    /// <summary><c>xs:QName</c>.</summary>
    QName = 10,

    /// <summary><c>xs:NOTATION</c>.</summary>
    Notation = 11,

    /// <summary><c>xs:duration</c>.</summary>
    Duration = 12,

    /// <summary><c>xs:yearMonthDuration</c>.</summary>
    YearMonthDuration = 13,

    /// <summary><c>xs:dayTimeDuration</c>.</summary>
    DayTimeDuration = 14,

    /// <summary><c>xs:dateTime</c>.</summary>
    DateTime = 15,

    /// <summary><c>xs:date</c>.</summary>
    Date = 16,

    /// <summary><c>xs:time</c>.</summary>
    Time = 17,

    /// <summary><c>xs:gYearMonth</c>.</summary>
    GYearMonth = 18,

    /// <summary><c>xs:gYear</c>.</summary>
    GYear = 19,

    /// <summary><c>xs:gMonthDay</c>.</summary>
    GMonthDay = 20,

    /// <summary><c>xs:gDay</c>.</summary>
    GDay = 21,

    /// <summary><c>xs:gMonth</c>.</summary>
    GMonth = 22,

    /// <summary><c>xs:hexBinary</c>.</summary>
    HexBinary = 23,

    /// <summary><c>xs:base64Binary</c>.</summary>
    Base64Binary = 24,

    /// <summary>A function item (XPath 3.0).</summary>
    Function = 25,
}
