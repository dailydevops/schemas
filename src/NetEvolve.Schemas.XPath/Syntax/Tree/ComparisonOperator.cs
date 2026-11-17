namespace NetEvolve.Schemas.XPath.Syntax.Tree;

/// <summary>Operator of a comparison expression (productions GeneralComp, ValueComp, NodeComp).</summary>
internal enum ComparisonOperator
{
    /// <summary>=</summary>
    GeneralEqual = 0,

    /// <summary>!=</summary>
    GeneralNotEqual = 1,

    /// <summary>&lt;</summary>
    GeneralLessThan = 2,

    /// <summary>&lt;=</summary>
    GeneralLessThanOrEqual = 3,

    /// <summary>&gt;</summary>
    GeneralGreaterThan = 4,

    /// <summary>&gt;=</summary>
    GeneralGreaterThanOrEqual = 5,

    /// <summary>eq</summary>
    ValueEqual = 6,

    /// <summary>ne</summary>
    ValueNotEqual = 7,

    /// <summary>lt</summary>
    ValueLessThan = 8,

    /// <summary>le</summary>
    ValueLessThanOrEqual = 9,

    /// <summary>gt</summary>
    ValueGreaterThan = 10,

    /// <summary>ge</summary>
    ValueGreaterThanOrEqual = 11,

    /// <summary>is</summary>
    NodeIs = 12,

    /// <summary>&lt;&lt;</summary>
    NodePrecedes = 13,

    /// <summary>&gt;&gt;</summary>
    NodeFollows = 14,
}
