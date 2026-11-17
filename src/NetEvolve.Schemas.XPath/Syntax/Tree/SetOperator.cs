namespace NetEvolve.Schemas.XPath.Syntax.Tree;

/// <summary>Operator of a union, intersect, or except expression.</summary>
internal enum SetOperator
{
    /// <summary>union or |</summary>
    Union = 0,

    /// <summary>intersect</summary>
    Intersect = 1,

    /// <summary>except</summary>
    Except = 2,
}
