namespace NetEvolve.Schemas.XPath.Evaluation.Comparison;

/// <summary>
/// The six relations that the value comparison operators eq, ne, lt, le, gt, and ge and the general comparison operators =, !=, &lt;,
/// &lt;=, &gt;, and &gt;= share (XPath 2.0, 3.5.1 and 3.5.2): a general comparison applies the value comparison of its relation to
/// each pair of items.
/// </summary>
internal enum ComparisonRelation
{
    /// <summary>eq and =</summary>
    Equal = 0,

    /// <summary>ne and !=</summary>
    NotEqual = 1,

    /// <summary>lt and &lt;</summary>
    LessThan = 2,

    /// <summary>le and &lt;=</summary>
    LessThanOrEqual = 3,

    /// <summary>gt and &gt;</summary>
    GreaterThan = 4,

    /// <summary>ge and &gt;=</summary>
    GreaterThanOrEqual = 5,
}
