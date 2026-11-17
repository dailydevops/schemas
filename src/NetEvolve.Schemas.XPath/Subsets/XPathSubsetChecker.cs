namespace NetEvolve.Schemas.XPath.Subsets;

using System.Collections.Generic;
using NetEvolve.Schemas.XPath.Abstractions;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Syntax.Tree;

/// <summary>
/// A rule-driven subset profile: it walks unions, paths, and steps of a parsed expression and asks every rule about every
/// site. The first violation is reported with the rule name as the local name of its code and the location of the
/// offending step or operator. Accepting an expression allocates nothing.
/// </summary>
/// <remarks>
/// Limitations: the syntax tree does not keep abbreviations apart from their long forms, so <c>child::a</c> is judged as
/// <c>a</c>, <c>attribute::a</c> as <c>@a</c>, and the <c>union</c> keyword as <c>|</c>; all three are accepted where the
/// abbreviation is. Explicit axes other than child and attribute are rejected by the <c>axis</c> rule.
/// </remarks>
internal sealed class XPathSubsetChecker : IXPathSubsetProfile
{
    /// <summary>The namespace of the violation codes; the local name is the rule name.</summary>
    public const string RuleNamespace = "urn:netevolve:schemas:xpath:subset";

    private readonly IXPathSubsetRule[] _rules;

    /// <summary>Initializes a new instance of the <see cref="XPathSubsetChecker"/> class.</summary>
    /// <param name="rules">The rules, applied in the given order.</param>
    public XPathSubsetChecker(IReadOnlyList<IXPathSubsetRule> rules)
    {
        var copy = new IXPathSubsetRule[rules.Count];
        for (var i = 0; i < copy.Length; i++)
        {
            copy[i] = rules[i];
        }

        _rules = copy;
    }

    /// <summary>
    /// Gets the profile of XSD identity-constraint selectors: <c>Path ('|' Path)*</c> with
    /// <c>Path ::= ('.//')? Step ('/' Step)*</c> and <c>Step ::= '.' | NameTest</c>.
    /// </summary>
    public static XPathSubsetChecker Selector { get; } =
        new([
            XPathSubsetRules.AbsolutePathRule,
            XPathSubsetRules.SetOperatorRule,
            XPathSubsetRules.PredicateRule,
            XPathSubsetRules.StepKindRule,
            XPathSubsetRules.AxisRule,
            XPathSubsetRules.DescendantPositionRule,
            XPathSubsetRules.NodeTestRule,
            XPathSubsetRules.NameTestRule,
            XPathSubsetRules.AttributeStepRule,
        ]);

    /// <summary>Gets the profile of XSD identity-constraint fields: the selector steps plus one final attribute step.</summary>
    public static XPathSubsetChecker Field { get; } =
        new([
            XPathSubsetRules.AbsolutePathRule,
            XPathSubsetRules.SetOperatorRule,
            XPathSubsetRules.PredicateRule,
            XPathSubsetRules.StepKindRule,
            XPathSubsetRules.AxisRule,
            XPathSubsetRules.DescendantPositionRule,
            XPathSubsetRules.NodeTestRule,
            XPathSubsetRules.NameTestRule,
            XPathSubsetRules.AttributeStepPositionRule,
        ]);

    /// <summary>Creates a profile with the rules of this one plus one more, applied last.</summary>
    /// <param name="rule">The extra rule.</param>
    /// <returns>The new profile; this one is unchanged.</returns>
    public XPathSubsetChecker With(IXPathSubsetRule rule)
    {
        var rules = new List<IXPathSubsetRule>(_rules) { rule };
        return new XPathSubsetChecker(rules);
    }

    /// <inheritdoc />
    public bool Accepts(Expression tree, out XPathDiagnostic violation)
    {
        if (tree is SetExpression set)
        {
            if (set.Operator != SetOperator.Union)
            {
                return Apply(
                    new XPathSubsetSite(XPathSubsetSiteKind.Operator, set, PathRoot.Relative, 0, 0, null),
                    out violation
                );
            }

            return Accepts(set.Left, out violation) && Accepts(set.Right, out violation);
        }

        return tree is PathExpression path
            ? AcceptsPath(path, path.Root, path.Steps, out violation)
            : AcceptsPath(tree, PathRoot.Relative, null, out violation);
    }

    // A non-path expression is judged as a path of one step, which the step-kind rule rejects when it is no step.
    private bool AcceptsPath(
        Expression node,
        PathRoot root,
        IReadOnlyList<Expression>? steps,
        out XPathDiagnostic violation
    )
    {
        var count = steps?.Count ?? 1;
        if (!Apply(new XPathSubsetSite(XPathSubsetSiteKind.Path, node, root, 0, count, null), out violation))
        {
            return false;
        }

        Expression? previous = null;
        for (var i = 0; i < count; i++)
        {
            var step = steps is null ? node : steps[i];
            if (!Apply(new XPathSubsetSite(XPathSubsetSiteKind.Step, step, root, i, count, previous), out violation))
            {
                return false;
            }

            previous = step;
        }

        violation = default;
        return true;
    }

    private bool Apply(in XPathSubsetSite site, out XPathDiagnostic violation)
    {
        foreach (var rule in _rules)
        {
            if (rule.IsViolatedBy(in site, out var location))
            {
                violation = new XPathDiagnostic(
                    new XPathErrorCode(RuleNamespace, rule.Name),
                    XPathErrorKind.Static,
                    rule.Name + ": " + rule.Description,
                    location.Offset,
                    location.Length
                );
                return false;
            }
        }

        violation = default;
        return true;
    }
}
