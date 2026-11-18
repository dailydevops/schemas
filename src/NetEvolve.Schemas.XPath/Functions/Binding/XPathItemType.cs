namespace NetEvolve.Schemas.XPath.Functions.Binding;

/// <summary>The expected item type of a function parameter.</summary>
/// <remarks>
/// Limitation: only the item types a function signature of the XPath 2.0 library needs are named. The kind tests other than
/// <c>node()</c> belong to the sequence type matcher of XP-21 (#227); <c>xs:QName</c>, <c>xs:NOTATION</c>, the binary types, and the
/// Gregorian types await their value types (#545, #301).
/// </remarks>
internal enum XPathItemType
{
    /// <summary><c>item()</c>: every item passes unchanged.</summary>
    Item = 0,

    /// <summary><c>node()</c>: every item must be a node.</summary>
    Node = 1,

    /// <summary><c>xs:anyAtomicType</c>: atomization only; <c>xs:untypedAtomic</c> stays as it is.</summary>
    AnyAtomic = 2,

    /// <summary>The <c>numeric</c> pseudo type of the Functions and Operators: integer, decimal, float, or double, without promotion; <c>xs:untypedAtomic</c> is cast to <c>xs:double</c>.</summary>
    Numeric = 3,

    /// <summary><c>xs:untypedAtomic</c>.</summary>
    UntypedAtomic = 4,

    /// <summary><c>xs:string</c>.</summary>
    String = 5,

    /// <summary><c>xs:boolean</c>.</summary>
    Boolean = 6,

    /// <summary><c>xs:integer</c>.</summary>
    Integer = 7,

    /// <summary><c>xs:decimal</c>.</summary>
    Decimal = 8,

    /// <summary><c>xs:float</c>.</summary>
    Float = 9,

    /// <summary><c>xs:double</c>.</summary>
    Double = 10,

    /// <summary><c>xs:anyURI</c>.</summary>
    AnyUri = 11,

    /// <summary><c>xs:dateTime</c>.</summary>
    DateTime = 12,

    /// <summary><c>xs:date</c>.</summary>
    Date = 13,

    /// <summary><c>xs:time</c>.</summary>
    Time = 14,

    /// <summary><c>xs:duration</c>.</summary>
    Duration = 15,

    /// <summary><c>xs:yearMonthDuration</c>.</summary>
    YearMonthDuration = 16,

    /// <summary><c>xs:dayTimeDuration</c>.</summary>
    DayTimeDuration = 17,
}
