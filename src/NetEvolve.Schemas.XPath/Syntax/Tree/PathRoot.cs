namespace NetEvolve.Schemas.XPath.Syntax.Tree;

/// <summary>How a path expression starts.</summary>
internal enum PathRoot
{
    /// <summary>No leading slash.</summary>
    Relative = 0,

    /// <summary>A leading <c>/</c>; with no steps it selects the root.</summary>
    Root = 1,

    /// <summary>A leading <c>//</c>; the descendant-or-self step is not part of the step list.</summary>
    RootDescendants = 2,
}
