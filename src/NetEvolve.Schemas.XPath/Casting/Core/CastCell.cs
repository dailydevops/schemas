namespace NetEvolve.Schemas.XPath.Casting.Core;

/// <summary>The cell of the casting table for one source and target type (Functions and Operators, 17.1).</summary>
internal enum CastCell
{
    /// <summary>The cast is not allowed: it raises XPTY0004. A pair without a row has this cell.</summary>
    NotAllowed = 0,

    /// <summary>The cast is allowed and succeeds for every value of the source type (Y in the table of the specification).</summary>
    Always = 1,

    /// <summary>The cast is allowed, and the value decides whether it succeeds (M in the table of the specification).</summary>
    ValueDependent = 2,
}
