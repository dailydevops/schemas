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

/// <summary>
/// Builds the plan of one pattern text from its syntax tree. The tree is accepted by the pattern profile before, except
/// for an extended pattern (XSLT binding): there the XSLT 3.0 forms are built here and every path is checked here.
/// </summary>
/// <remarks>
/// Limitation (DEF-60): the XSLT 3.0 forms <c>intersect</c>, <c>except</c>, parentheses, and predicates on a parenthesized
/// pattern are accepted under every XSLT binding, so also under <c>xslt</c> and <c>xslt2</c>, which is a superset of
/// those versions. Patterns rooted at a variable reference are not supported. A key start (<c>key('name', 'value')</c>,
/// alone or followed by a relative path) is built under an XSLT binding only: the call is compiled as a node expression
/// and evaluated against the key index of the run (#780).
/// </remarks>
internal sealed class PatternBuilder(
    IQueryBinding binding,
    QueryStaticScope scope,
    XPathStaticContext context,
    string text,
    PatternLocator locator,
    bool extended
)
{
    private const string UnsupportedRule = "node-test";
    private const string PredicateRule = "predicate";

    // The slots the predicates of every alternative read, for CompiledPattern.ReferencedSlots.
    private readonly SlotSet _slots = new();

    internal bool TryBuild(Expression tree, out PatternPlan plan, out SchematronProblem problem)
    {
        plan = null!;
        var expressions = new List<Expression>();
        Collect(tree, expressions);

        var alternatives = new PatternAlternative[expressions.Count];
        var priorities = new decimal[expressions.Count];
        var prefilter = new Prefilter();
        try
        {
            for (var i = 0; i < alternatives.Length; i++)
            {
                if (!TryBuildNode(expressions[i], prefilter, out var alternative, out problem))
                {
                    return false;
                }

                alternatives[i] = alternative;
                priorities[i] = alternative.Priority;
            }
        }
        catch (XPathException exception)
        {
            problem = Problem(SchematronDiagnosticCodes.ExpressionInvalid, exception.Diagnostic);
            return false;
        }

        plan = new PatternPlan(
            prefilter.Kinds,
            prefilter.AnyName ? [] : prefilter.Names.ToArray(),
            alternatives,
            priorities,
            _slots.ToArray()
        );
        problem = default;
        return true;
    }

    private static void Collect(Expression expression, List<Expression> alternatives)
    {
        if (expression is SetExpression { Operator: SetOperator.Union } set)
        {
            Collect(set.Left, alternatives);
            Collect(set.Right, alternatives);
        }
        else
        {
            alternatives.Add(expression);
        }
    }

    // The prefilter data of the pattern: a node must have one of these kinds and names to match at all.
    private sealed class Prefilter
    {
        internal QueryNodeKinds Kinds { get; set; }

        internal List<XPathQName> Names { get; } = [];

        internal bool AnyName { get; set; }
    }

    private bool TryBuildNode(
        Expression expression,
        Prefilter prefilter,
        out PatternAlternative alternative,
        out SchematronProblem problem
    )
    {
        alternative = null!;
        problem = default;
        switch (expression)
        {
            case SetExpression set when extended:
            {
                // A node of the right side alone never matches, so only a union adds its kinds and names.
                var rightFilter = set.Operator == SetOperator.Union ? prefilter : new Prefilter();
                if (
                    !TryBuildNode(set.Left, prefilter, out var left, out problem)
                    || !TryBuildNode(set.Right, rightFilter, out var right, out problem)
                )
                {
                    return false;
                }

                alternative = new SetPatternAlternative(set.Operator, left, right);
                return true;
            }
            case ParenthesizedExpression { Inner: { } inner } when extended:
                return TryBuildNode(inner, prefilter, out alternative, out problem);
            case FilterExpression { Primary: ParenthesizedExpression { Inner: not null } } filter when extended:
                return TryBuildFiltered(filter, prefilter, out alternative, out problem);
            default:
                if (extended && KeyCall(expression) is { } keyCall)
                {
                    return TryBuildKeyPath(expression, keyCall, prefilter, out alternative, out problem);
                }

                if (extended && !PatternProfile.Checker.Accepts(expression, out var violation))
                {
                    problem = new SchematronProblem(
                        SchematronDiagnosticCodes.PatternUnsupported,
                        violation.Message,
                        locator.Locate(violation.Offset),
                        violation.QName.LocalName
                    );
                    return false;
                }

                return TryBuildPath(expression, prefilter, out alternative, out problem);
        }
    }

    private bool TryBuildFiltered(
        FilterExpression filter,
        Prefilter prefilter,
        out PatternAlternative alternative,
        out SchematronProblem problem
    )
    {
        alternative = null!;
        var predicates = new QueryExpression[filter.Predicates.Count];
        for (var i = 0; i < predicates.Length; i++)
        {
            var predicate = filter.Predicates[i];
            if (
                PatternPredicates.IsNumeric(predicate)
                || PatternPredicates.Calls(predicate, "last")
                || PatternPredicates.Calls(predicate, "position")
            )
            {
                problem = new SchematronProblem(
                    SchematronDiagnosticCodes.PatternUnsupported,
                    "A predicate of a parenthesized pattern must not read a position or the size.",
                    locator.Locate(predicate.Location.Offset),
                    PredicateRule
                );
                return false;
            }

            if (!TryCompilePredicate(predicate, false, out predicates[i], out problem))
            {
                return false;
            }
        }

        if (!TryBuildNode(filter.Primary, prefilter, out var inner, out problem))
        {
            return false;
        }

        alternative = new FilteredPatternAlternative(inner, predicates);
        return true;
    }

    private bool TryCompilePredicate(
        Expression predicate,
        bool numeric,
        out QueryExpression compiled,
        out SchematronProblem problem
    )
    {
        // A numeric predicate is the position test it stands for; every other one is a boolean test.
        var source = text.Substring(predicate.Location.Offset, predicate.Location.Length);
        if (numeric)
        {
            source = "position() = (" + source + ")";
        }

        if (!binding.TryCompile(source.AsSpan(), QueryExpressionKind.Boolean, scope, out compiled, out problem))
        {
            return false;
        }

        _ = _slots.Add(compiled);
        return true;
    }

    private bool TryBuildPath(
        Expression expression,
        Prefilter prefilter,
        out PatternAlternative alternative,
        out SchematronProblem problem
    )
    {
        var path = expression as PathExpression;
        var raw = path is null ? (IReadOnlyList<Expression>)new[] { expression } : path.Steps;
        return TryBuildSteps(path?.Root ?? PathRoot.Relative, raw, null, prefilter, out alternative, out problem);
    }

    // The call of key() that starts a pattern: alone, or as the first step of a relative path (XSLT 2.0 IdKeyPattern).
    private static FunctionCall? KeyCall(Expression expression)
    {
        var first = expression is PathExpression { Root: PathRoot.Relative, Steps.Count: > 0 } path
            ? path.Steps[0]
            : expression;
        return first is FunctionCall { Name: { Prefix: null, LocalName: "key" } } call ? call : null;
    }

    private bool TryBuildKeyPath(
        Expression expression,
        FunctionCall call,
        Prefilter prefilter,
        out PatternAlternative alternative,
        out SchematronProblem problem
    )
    {
        alternative = null!;
        if (call.Arguments.Count != 2 || !IsKeyArgument(call.Arguments[0]) || !IsKeyArgument(call.Arguments[1]))
        {
            problem = new SchematronProblem(
                SchematronDiagnosticCodes.PatternUnsupported,
                "The call of key() that starts a pattern takes two arguments, each a string literal or a variable reference.",
                locator.Locate(call.Location.Offset),
                "key"
            );
            return false;
        }

        var rest = new List<Expression>();
        if (expression is PathExpression path)
        {
            for (var i = 1; i < path.Steps.Count; i++)
            {
                rest.Add(path.Steps[i]);
            }
        }

        if (
            rest.Count > 0
            && !PatternProfile.Checker.Accepts(
                new PathExpression(expression.Location, PathRoot.Relative, rest),
                out var violation
            )
        )
        {
            problem = new SchematronProblem(
                SchematronDiagnosticCodes.PatternUnsupported,
                violation.Message,
                locator.Locate(violation.Offset),
                violation.QName.LocalName
            );
            return false;
        }

        var source = text.Substring(call.Location.Offset, call.Location.Length);
        if (!binding.TryCompile(source.AsSpan(), QueryExpressionKind.Nodes, scope, out var keyStart, out problem))
        {
            return false;
        }

        _ = _slots.Add(keyStart);
        return TryBuildSteps(PathRoot.Relative, rest, keyStart, prefilter, out alternative, out problem);
    }

    private static bool IsKeyArgument(Expression argument) => argument is StringLiteral or VariableReference;

    private bool TryBuildSteps(
        PathRoot root,
        IReadOnlyList<Expression> raw,
        QueryExpression? keyStart,
        Prefilter prefilter,
        out PatternAlternative alternative,
        out SchematronProblem problem
    )
    {
        alternative = null!;
        problem = default;
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

        var last = axisSteps.Count == 0 ? null : axisSteps[axisSteps.Count - 1];
        if (keyStart is not null && last is null)
        {
            // Every node of the key can match, whatever its kind and name.
            prefilter.Kinds |= QueryNodeKinds.Document | QueryNodeKinds.Element | QueryNodeKinds.Attribute;
            prefilter.Kinds |= QueryNodeKinds.Text | QueryNodeKinds.Comment | QueryNodeKinds.ProcessingInstruction;
            prefilter.AnyName = true;
        }
        else
        {
            prefilter.Kinds |= KindsOf(last);
            prefilter.AnyName |= !TryAddName(last, prefilter.Names);
        }

        // A pattern with a key start has the priority 0.5 (XSLT 2.0 section 6.4: not a single step).
        var priority = keyStart is null ? PatternPriority.Of(root, axisSteps) : 0.5m;
        alternative = new PathAlternative(root, steps.ToArray(), priority, keyStart);
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

            if (!TryCompilePredicate(predicate, numeric, out predicates[i], out problem))
            {
                return false;
            }
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
