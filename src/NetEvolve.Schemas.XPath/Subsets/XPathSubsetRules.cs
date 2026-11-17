namespace NetEvolve.Schemas.XPath.Subsets;

using NetEvolve.Schemas.XPath.Syntax.Tree;

/// <summary>The rules of the identity-constraint selector and field profiles (XML Schema Part 1, 3.11.6).</summary>
internal static class XPathSubsetRules
{
    /// <summary>A path must be relative; the violation is the leading <c>/</c> or <c>//</c>.</summary>
    public static IXPathSubsetRule AbsolutePathRule { get; } =
        new XPathSubsetRule(
            "absolute-path",
            "A path must be relative to the context node.",
            static (in XPathSubsetSite site, out SourceSpan location) =>
            {
                var absolute = site.Kind == XPathSubsetSiteKind.Path && site.Root != PathRoot.Relative;
                location = absolute
                    ? new SourceSpan(site.Node.Location.Offset, site.Root == PathRoot.Root ? 1 : 2)
                    : default;
                return absolute;
            }
        );

    /// <summary>Only the union <c>|</c> combines paths; the violation is the gap that holds the operator.</summary>
    public static IXPathSubsetRule SetOperatorRule { get; } =
        new XPathSubsetRule(
            "set-operator",
            "Only the union operator '|' may combine paths.",
            static (in XPathSubsetSite site, out SourceSpan location) =>
            {
                if (site.Kind == XPathSubsetSiteKind.Operator && site.Node is SetExpression set)
                {
                    var start = set.Left.Location.End;
                    location = new SourceSpan(start, set.Right.Location.Offset - start);
                    return true;
                }

                location = default;
                return false;
            }
        );

    /// <summary>Steps carry no predicates.</summary>
    public static IXPathSubsetRule PredicateRule { get; } =
        new XPathSubsetRule(
            "predicate",
            "A step must not have a predicate.",
            static (in XPathSubsetSite site, out SourceSpan location) =>
            {
                var predicates = site.Node switch
                {
                    AxisStep step => step.Predicates,
                    FilterExpression filter => filter.Predicates,
                    _ => null,
                };
                if (site.Kind == XPathSubsetSiteKind.Step && predicates is { Count: > 0 })
                {
                    location = predicates[0].Location;
                    return true;
                }

                location = default;
                return false;
            }
        );

    /// <summary>A step is an axis step or <c>.</c>; literals, calls, parentheses, and operators are not steps.</summary>
    public static IXPathSubsetRule StepKindRule { get; } =
        new XPathSubsetRule(
            "step-kind",
            "Only '.', a name test, and an attribute name test are steps.",
            static (in XPathSubsetSite site, out SourceSpan location) =>
            {
                var bad = site.Kind == XPathSubsetSiteKind.Step && site.Node is not (AxisStep or ContextItemExpression);
                location = bad ? site.Node.Location : default;
                return bad;
            }
        );

    /// <summary>Only the child and attribute axes (and the <c>//</c> of <c>.//</c>) are available.</summary>
    public static IXPathSubsetRule AxisRule { get; } =
        new XPathSubsetRule(
            "axis",
            "Only the child and attribute axes are allowed.",
            static (in XPathSubsetSite site, out SourceSpan location) =>
            {
                var bad =
                    site.Kind == XPathSubsetSiteKind.Step
                    && site.Node is AxisStep { Axis: not (Axis.Child or Axis.Attribute) }
                    && !site.IsDescendantSeparator;
                location = bad ? site.Node.Location : default;
                return bad;
            }
        );

    /// <summary>The <c>//</c> step is allowed only as the leading <c>.//</c>.</summary>
    public static IXPathSubsetRule DescendantPositionRule { get; } =
        new XPathSubsetRule(
            "descendant-position",
            "'//' is allowed only as the leading './/'.",
            static (in XPathSubsetSite site, out SourceSpan location) =>
            {
                var bad =
                    site.Kind == XPathSubsetSiteKind.Step
                    && site.IsDescendantSeparator
                    && !site.IsLeadingDescendantPrefix;
                location = bad ? site.Node.Location : default;
                return bad;
            }
        );

    /// <summary>A step tests a name, not a node kind.</summary>
    public static IXPathSubsetRule NodeTestRule { get; } =
        new XPathSubsetRule(
            "node-test",
            "A step must use a name test, not a kind test.",
            static (in XPathSubsetSite site, out SourceSpan location) =>
            {
                if (
                    site.Kind == XPathSubsetSiteKind.Step
                    && site.Node is AxisStep { Test: not NameTest } step
                    && !site.IsDescendantSeparator
                )
                {
                    location = step.Test.Location;
                    return true;
                }

                location = default;
                return false;
            }
        );

    /// <summary>The name test is a QName, <c>*</c>, or <c>prefix:*</c>; <c>*:local</c> is XPath 2.0 only.</summary>
    public static IXPathSubsetRule NameTestRule { get; } =
        new XPathSubsetRule(
            "name-test",
            "A name test must be a QName, '*', or 'prefix:*'.",
            static (in XPathSubsetSite site, out SourceSpan location) =>
            {
                if (
                    site.Kind == XPathSubsetSiteKind.Step
                    && site.Node is AxisStep { Test: NameTest { Kind: NameTestKind.AnyNamespace } test }
                )
                {
                    location = test.Location;
                    return true;
                }

                location = default;
                return false;
            }
        );

    /// <summary>The selector profile has no attribute steps.</summary>
    public static IXPathSubsetRule AttributeStepRule { get; } =
        new XPathSubsetRule(
            "attribute-step",
            "A selector must not select attributes.",
            static (in XPathSubsetSite site, out SourceSpan location) =>
            {
                var bad = site.Kind == XPathSubsetSiteKind.Step && site.Node is AxisStep { Axis: Axis.Attribute };
                location = bad ? site.Node.Location : default;
                return bad;
            }
        );

    /// <summary>The field profile allows an attribute step only as the final step.</summary>
    public static IXPathSubsetRule AttributeStepPositionRule { get; } =
        new XPathSubsetRule(
            "attribute-step-position",
            "An attribute step must be the final step of a field path.",
            static (in XPathSubsetSite site, out SourceSpan location) =>
            {
                var bad =
                    site.Kind == XPathSubsetSiteKind.Step
                    && site.Node is AxisStep { Axis: Axis.Attribute }
                    && site.StepIndex != site.StepCount - 1;
                location = bad ? site.Node.Location : default;
                return bad;
            }
        );
}
