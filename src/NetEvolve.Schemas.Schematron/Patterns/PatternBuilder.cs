namespace NetEvolve.Schemas.Schematron.Patterns;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Xslt.Instructions.Flow;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Evaluation.Axes;
using NetEvolve.Schemas.XPath.Syntax.Tree;

/// <summary>Builds the plan of one pattern text from its syntax tree, which the pattern profile has accepted.</summary>
internal sealed class PatternBuilder(
    IQueryBinding binding,
    QueryStaticScope scope,
    XPathStaticContext context,
    string text,
    PatternLocator locator
)
{
    private const string UnsupportedRule = "node-test";

    // The slots the predicates of every alternative read, for CompiledPattern.ReferencedSlots.
    private readonly SlotSet _slots = new();

    internal bool TryBuild(Expression tree, out PatternPlan plan, out SchematronProblem problem)
    {
        plan = null!;
        var expressions = new List<Expression>();
        Collect(tree, expressions);

        var alternatives = new PatternAlternative[expressions.Count];
        var priorities = new decimal[expressions.Count];
        var kinds = QueryNodeKinds.None;
        var names = new List<XPathQName>();
        var anyName = false;
        try
        {
            for (var i = 0; i < alternatives.Length; i++)
            {
                if (!TryBuildAlternative(expressions[i], out var alternative, out var last, out problem))
                {
                    return false;
                }

                alternatives[i] = alternative;
                priorities[i] = alternative.Priority;
                kinds |= KindsOf(last);
                anyName |= !TryAddName(last, names);
            }
        }
        catch (XPathException exception)
        {
            problem = Problem(SchematronDiagnosticCodes.ExpressionInvalid, exception.Diagnostic);
            return false;
        }

        plan = new PatternPlan(kinds, anyName ? [] : names.ToArray(), alternatives, priorities, _slots.ToArray());
        problem = default;
        return true;
    }

    private static void Collect(Expression expression, List<Expression> alternatives)
    {
        if (expression is SetExpression set)
        {
            Collect(set.Left, alternatives);
            Collect(set.Right, alternatives);
        }
        else
        {
            alternatives.Add(expression);
        }
    }

    private bool TryBuildAlternative(
        Expression expression,
        out PatternAlternative alternative,
        out AxisStep? last,
        out SchematronProblem problem
    )
    {
        alternative = null!;
        last = null;
        problem = default;
        var path = expression as PathExpression;
        var root = path?.Root ?? PathRoot.Relative;
        var raw = path is null ? (IReadOnlyList<Expression>)new[] { expression } : path.Steps;

        var steps = new List<PatternStep>();
        var axisSteps = new List<AxisStep>();
        var descendant = false;
        foreach (var node in raw)
        {
            var axisStep = (AxisStep)node;
            if (IsSeparator(axisStep))
            {
                descendant = true;
                continue;
            }

            if (!TryBuildStep(axisStep, descendant, out var step, out problem))
            {
                return false;
            }

            steps.Add(step);
            axisSteps.Add(axisStep);
            descendant = false;
        }

        last = axisSteps.Count == 0 ? null : axisSteps[axisSteps.Count - 1];
        alternative = new PatternAlternative(root, steps.ToArray(), PatternPriority.Of(root, axisSteps));
        return true;
    }

    // The step the parser inserts for "//": descendant-or-self::node() located at the two-character operator.
    private static bool IsSeparator(AxisStep step) =>
        step is { Axis: Axis.DescendantOrSelf, Test: AnyKindTest, Predicates.Count: 0, Location.Length: 2 };

    private bool TryBuildStep(
        AxisStep step,
        bool descendantBefore,
        out PatternStep patternStep,
        out SchematronProblem problem
    )
    {
        patternStep = null!;
        problem = default;
        if (!XPathNodeTest.TryFromSyntax(step.Test, step.Axis, context, out var test))
        {
            problem = new SchematronProblem(
                SchematronDiagnosticCodes.PatternUnsupported,
                "The pattern step uses a node test outside the supported pattern subset.",
                locator.Locate(step.Test.Location.Offset),
                UnsupportedRule
            );
            return false;
        }

        var count = step.Predicates.Count;
        var predicates = new QueryExpression[count];
        var positional = new bool[count];
        var usesLast = new bool[count];
        for (var i = 0; i < count; i++)
        {
            var predicate = step.Predicates[i];
            var numeric = PatternPredicates.IsNumeric(predicate);
            usesLast[i] = PatternPredicates.Calls(predicate, "last");
            positional[i] = numeric || usesLast[i] || PatternPredicates.Calls(predicate, "position");

            // A numeric predicate is the position test it stands for; every other one is a boolean test.
            var source = text.Substring(predicate.Location.Offset, predicate.Location.Length);
            if (numeric)
            {
                source = "position() = (" + source + ")";
            }

            if (!binding.TryCompile(source.AsSpan(), QueryExpressionKind.Boolean, scope, out var compiled, out problem))
            {
                return false;
            }

            predicates[i] = compiled;
            _ = _slots.Add(compiled);
        }

        patternStep = new PatternStep(
            test,
            step.Axis == Axis.Attribute,
            step.Test is DocumentTest,
            descendantBefore,
            predicates,
            positional,
            usesLast
        );
        return true;
    }

    private static QueryNodeKinds KindsOf(AxisStep? last)
    {
        if (last is null)
        {
            return QueryNodeKinds.Document;
        }

        if (last.Axis == Axis.Attribute)
        {
            return QueryNodeKinds.Attribute;
        }

        return last.Test switch
        {
            NameTest or ElementTest => QueryNodeKinds.Element,
            TextTest => QueryNodeKinds.Text,
            CommentTest => QueryNodeKinds.Comment,
            ProcessingInstructionTest => QueryNodeKinds.ProcessingInstruction,
            DocumentTest => QueryNodeKinds.Document,
            AnyKindTest => QueryNodeKinds.Element
                | QueryNodeKinds.Text
                | QueryNodeKinds.Comment
                | QueryNodeKinds.ProcessingInstruction,
            _ => QueryNodeKinds.None,
        };
    }

    // Adds the expanded name the last step requires; false when it accepts any name.
    private bool TryAddName(AxisStep? last, List<XPathQName> names)
    {
        switch (last?.Test)
        {
            case NameTest { Kind: NameTestKind.QualifiedName } name:
                names.Add(Expand(name.Prefix, name.LocalName!));
                return true;
            case ElementTest { Name: { } element }:
                names.Add(Expand(element.Prefix, element.LocalName));
                return true;
            case AttributeTest { Name: { } attribute }:
                names.Add(Expand(attribute.Prefix, attribute.LocalName));
                return true;
            case ProcessingInstructionTest { Target: { } target }:
                names.Add(new XPathQName(string.Empty, target));
                return true;
            default:
                return false;
        }
    }

    // The prefixes were resolved while the node tests were built, and an unprefixed name has no namespace.
    private XPathQName Expand(string? prefix, string local)
    {
        var namespaceUri = string.Empty;
        if (prefix is not null && context.TryResolveNamespace(prefix, out var resolved))
        {
            namespaceUri = resolved ?? string.Empty;
        }

        return new XPathQName(namespaceUri, local);
    }

    private SchematronProblem Problem(DiagnosticCode code, XPathDiagnostic diagnostic) =>
        new(code, diagnostic.Message, locator.Locate(diagnostic.Offset), diagnostic.QName.LocalName);
}
