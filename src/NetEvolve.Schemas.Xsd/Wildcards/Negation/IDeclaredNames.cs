namespace NetEvolve.Schemas.Xsd.Wildcards.Negation;

using NetEvolve.Schemas.XPath.Api;

/// <summary>A set of expanded names that a wildcard consults for <c>##defined</c> and <c>##definedSibling</c>.</summary>
internal interface IDeclaredNames
{
    /// <summary>Determines whether the set holds a name.</summary>
    /// <param name="name">The expanded name.</param>
    /// <returns><see langword="true"/> when the name is in the set.</returns>
    bool Contains(XPathQName name);
}
