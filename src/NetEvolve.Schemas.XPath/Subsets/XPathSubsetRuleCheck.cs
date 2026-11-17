namespace NetEvolve.Schemas.XPath.Subsets;

using NetEvolve.Schemas.XPath.Syntax.Tree;

/// <summary>The check of a delegate-backed rule.</summary>
/// <param name="site">The site to judge.</param>
/// <param name="location">The location of the offending node when the rule is violated.</param>
/// <returns><see langword="true"/> when the rule is violated.</returns>
internal delegate bool XPathSubsetRuleCheck(in XPathSubsetSite site, out SourceSpan location);
