namespace NetEvolve.Schemas.XPath.Subsets;

using NetEvolve.Schemas.XPath.Syntax.Tree;

/// <summary>A rule defined by a name, a message, and a check.</summary>
/// <param name="name">The rule name.</param>
/// <param name="description">The message text.</param>
/// <param name="check">The check.</param>
internal sealed class XPathSubsetRule(string name, string description, XPathSubsetRuleCheck check) : IXPathSubsetRule
{
    /// <inheritdoc />
    public string Name { get; } = name;

    /// <inheritdoc />
    public string Description { get; } = description;

    /// <inheritdoc />
    public bool IsViolatedBy(in XPathSubsetSite site, out SourceSpan location) => check(in site, out location);
}
