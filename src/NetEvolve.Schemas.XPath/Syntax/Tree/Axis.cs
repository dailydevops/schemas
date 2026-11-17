namespace NetEvolve.Schemas.XPath.Syntax.Tree;

/// <summary>Axis of an axis step. Abbreviations are resolved by the parser to these values.</summary>
internal enum Axis
{
    /// <summary>child</summary>
    Child = 0,

    /// <summary>descendant</summary>
    Descendant = 1,

    /// <summary>attribute (also <c>@</c>)</summary>
    Attribute = 2,

    /// <summary>self</summary>
    Self = 3,

    /// <summary>descendant-or-self</summary>
    DescendantOrSelf = 4,

    /// <summary>following-sibling</summary>
    FollowingSibling = 5,

    /// <summary>following</summary>
    Following = 6,

    /// <summary>namespace (rejected later with XPST0010, DEF-42)</summary>
    Namespace = 7,

    /// <summary>parent (also <c>..</c>)</summary>
    Parent = 8,

    /// <summary>ancestor</summary>
    Ancestor = 9,

    /// <summary>preceding-sibling</summary>
    PrecedingSibling = 10,

    /// <summary>preceding</summary>
    Preceding = 11,

    /// <summary>ancestor-or-self</summary>
    AncestorOrSelf = 12,
}
