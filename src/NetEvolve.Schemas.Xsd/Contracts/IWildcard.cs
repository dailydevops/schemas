namespace NetEvolve.Schemas.Xsd.Contracts;

using NetEvolve.Schemas.XPath.Api;

/// <summary>A wildcard component: a name test with the set operations of Structures 3.10.6.</summary>
internal interface IWildcard : IXsdComponent
{
    /// <summary>Gets how matched items are assessed.</summary>
    XsdProcessContents ProcessContents { get; }

    /// <summary>Determines whether the wildcard allows an expanded name.</summary>
    /// <param name="name">The name.</param>
    /// <returns><see langword="true"/> when the name is allowed.</returns>
    bool Allows(XPathQName name);

    /// <summary>Determines whether every name this wildcard allows is also allowed by <paramref name="other"/> (Wildcard Subset).</summary>
    /// <param name="other">The superset candidate.</param>
    /// <returns><see langword="true"/> when this wildcard is a subset.</returns>
    bool IsSubsetOf(IWildcard other);

    /// <summary>Computes the union (Attribute Wildcard Union).</summary>
    /// <param name="other">The other wildcard.</param>
    /// <returns>The union, or <see langword="null"/> when it is not expressible.</returns>
    IWildcard? Union(IWildcard other);

    /// <summary>Computes the intersection (Attribute Wildcard Intersection).</summary>
    /// <param name="other">The other wildcard.</param>
    /// <returns>The intersection, or <see langword="null"/> when it is not expressible.</returns>
    IWildcard? Intersect(IWildcard other);
}
