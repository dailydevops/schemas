namespace NetEvolve.Schemas.XPath.Subsets;

/// <summary>What a rule is looking at when the checker asks it for a verdict.</summary>
internal enum XPathSubsetSiteKind
{
    /// <summary>A whole path: its root and its step count.</summary>
    Path = 0,

    /// <summary>One step of a path (or a non-step expression standing where a step is expected).</summary>
    Step = 1,

    /// <summary>A set operator other than the union <c>|</c>.</summary>
    Operator = 2,
}
