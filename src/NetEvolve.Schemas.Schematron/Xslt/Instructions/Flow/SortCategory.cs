namespace NetEvolve.Schemas.Schematron.Xslt.Instructions.Flow;

/// <summary>The comparison a sort key value takes part in; values of two categories are not comparable (XTDE1030).</summary>
internal enum SortCategory : byte
{
    /// <summary>The key is the empty sequence.</summary>
    Empty = 0,

    /// <summary>A string, an untyped value, a URI, or a node: compared by codepoint.</summary>
    Text = 1,

    /// <summary>An integer, decimal, float, or double.</summary>
    Number = 2,

    /// <summary>A boolean.</summary>
    Boolean = 3,

    /// <summary>
    /// A value of a type without an order, such as a QName. Dates, times, and durations also land here until the XPath package compares them (XP-26).
    /// </summary>
    Other = 4,
}
