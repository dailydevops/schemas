namespace NetEvolve.Schemas.Schematron.Patterns;

using NetEvolve.Schemas.XPath.Subsets;
using NetEvolve.Schemas.XPath.Syntax.Tree;

/// <summary>
/// The XSLT 2.0 pattern subset (section 5.5) as a second profile of the XPath subset facility: <c>Pattern ::= PathPattern
/// ('|' PathPattern)*</c> over child, attribute, and descendant-or-self steps, with predicates. The checker and the shared
/// rules are the ones of the identity-constraint profiles; only the two rules that differ live here.
/// </summary>
/// <remarks>
/// Limitations: <c>id()</c> as pattern start (DEF-55), the XSLT 3.0 forms (predicate patterns,
/// <c>intersect</c>, <c>except</c>, variable-rooted and parenthesized patterns; DEF-60), <c>schema-element()</c>,
/// <c>schema-attribute()</c>, and type names in <c>element()</c> and <c>attribute()</c> are reported as unsupported. This
/// profile judges the XSLT 2.0 subset; under an XSLT binding the pattern builder accepts the XSLT 3.0 forms around paths
/// that this profile accepts (DEF-60), and variable-rooted patterns stay unsupported.
/// The same holds for a <c>key()</c> start (#780), which the builder accepts under an XSLT binding.
/// </remarks>
internal static class PatternProfile
{
    /// <summary>A step is an axis step; <c>.</c>, calls, variables, literals, and parentheses are not pattern steps.</summary>
    private static readonly XPathSubsetRule StepKindRule = new(
        "step-kind",
        "A pattern step must be an axis step with a name test or a kind test.",
        static (in XPathSubsetSite site, out SourceSpan location) =>
        {
            var bad = site.Kind == XPathSubsetSiteKind.Step && site.Node is not AxisStep;
            location = bad ? site.Node.Location : default;
            return bad;
        }
    );

    /// <summary>A step tests a name or one of the kind tests without a type or schema reference.</summary>
    private static readonly XPathSubsetRule NodeTestRule = new(
        "node-test",
        "A pattern step must use a name test, node(), text(), comment(), processing-instruction(), element(), attribute(), or document-node().",
        static (in XPathSubsetSite site, out SourceSpan location) =>
        {
            if (site.Kind == XPathSubsetSiteKind.Step && site.Node is AxisStep step && !IsSupported(step.Test))
            {
                location = step.Test.Location;
                return true;
            }

            location = default;
            return false;
        }
    );

    /// <summary>Gets the pattern profile.</summary>
    internal static XPathSubsetChecker Checker { get; } =
        new([XPathSubsetRules.SetOperatorRule, StepKindRule, XPathSubsetRules.AxisRule, NodeTestRule]);

    private static bool IsSupported(NodeTest test) =>
        test switch
        {
            NameTest or AnyKindTest or TextTest or CommentTest or ProcessingInstructionTest => true,
            ElementTest { TypeName: null } or AttributeTest { TypeName: null } => true,
            DocumentTest { Element: null } => true,
            DocumentTest { Element: ElementTest { TypeName: null } } => true,
            _ => false,
        };
}
