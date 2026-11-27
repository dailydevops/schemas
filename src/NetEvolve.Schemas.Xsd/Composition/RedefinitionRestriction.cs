namespace NetEvolve.Schemas.Xsd.Composition;

using NetEvolve.Schemas.XPath.Api;

/// <summary>
/// A group or attribute group that a <c>redefine</c> replaces without referring to itself: the redefinition must be a restriction of the
/// original (Structures 1.0, 4.2.2, <c>src-redefine</c> 6.2 and 7.2), which the loader renamed and the component checks compare.
/// </summary>
internal sealed class RedefinitionRestriction
{
    internal RedefinitionRestriction(bool isGroup, XPathQName redefined, XPathQName original)
    {
        IsGroup = isGroup;
        Redefined = redefined;
        Original = original;
    }

    /// <summary>Gets a value indicating whether the component is a model group definition; otherwise it is an attribute group.</summary>
    internal bool IsGroup { get; }

    /// <summary>Gets the expanded name of the redefinition.</summary>
    internal XPathQName Redefined { get; }

    /// <summary>Gets the expanded name that the original has after the rename.</summary>
    internal XPathQName Original { get; }
}
