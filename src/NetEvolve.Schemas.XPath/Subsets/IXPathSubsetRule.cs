namespace NetEvolve.Schemas.XPath.Subsets;

using NetEvolve.Schemas.XPath.Syntax.Tree;

/// <summary>
/// One rule of a subset profile. The checker asks every rule about every site of the tree; a rule reports a violation
/// with the location of the offending node. Profiles are built from rules, so another package adds a profile or one extra
/// rule without changing the checker.
/// </summary>
internal interface IXPathSubsetRule
{
    /// <summary>Gets the rule name, reported as the local name of the violation code.</summary>
    string Name { get; }

    /// <summary>Gets the message text of a violation.</summary>
    string Description { get; }

    /// <summary>Determines whether the site violates the rule.</summary>
    /// <param name="site">The site to judge.</param>
    /// <param name="location">The location of the offending node when the rule is violated.</param>
    /// <returns><see langword="true"/> when the rule is violated.</returns>
    bool IsViolatedBy(in XPathSubsetSite site, out SourceSpan location);
}
