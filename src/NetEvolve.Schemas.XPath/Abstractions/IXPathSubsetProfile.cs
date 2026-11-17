namespace NetEvolve.Schemas.XPath.Abstractions;

using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Syntax.Tree;

/// <summary>
/// The seam through which Schematron and Xsd run an XPath subset profile: it takes a parsed syntax tree and accepts it or
/// reports a violation. The syntax tree stays internal; both packages reach it through <c>InternalsVisibleTo</c>.
/// The rule-driven implementation lives in <c>Subsets</c> (XP-57); it implements this interface and does not redeclare it.
/// </summary>
internal interface IXPathSubsetProfile
{
    /// <summary>Checks a parsed expression against the profile.</summary>
    /// <param name="tree">The root of the parsed syntax tree.</param>
    /// <param name="violation">The first violation, <see langword="default"/> when the tree is accepted.</param>
    /// <returns><see langword="true"/> when the profile accepts the tree.</returns>
    bool Accepts(Expression tree, out XPathDiagnostic violation);
}
