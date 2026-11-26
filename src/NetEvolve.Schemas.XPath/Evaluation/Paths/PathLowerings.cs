namespace NetEvolve.Schemas.XPath.Evaluation.Paths;

using System.Collections.Generic;
using NetEvolve.Arguments;
using NetEvolve.Schemas.XPath.Compilation;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Evaluation.Axes;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Types;

/// <summary>
/// The registration file of this folder: the lowerings of path expressions, axis steps, filter expressions, the context item
/// expression, and the set operators.
/// </summary>
/// <remarks>
/// An axis step outside a path is a path of one step, so it also returns its nodes in document order. While a path is lowered,
/// <c>descendant-or-self::node()/child::T</c> without predicates on the first step becomes <c>descendant::T</c>, which keeps the same nodes
/// and needs no sort; the same holds with predicates that cannot select by position (see <see cref="NonPositionalPredicates"/>), while
/// the expanded form stays for any other predicate, because a positional predicate counts among the children of each node.
/// Limitations: <c>schema-element()</c> and <c>schema-attribute()</c> tests are rejected with XPST0008 (#52) and the namespace axis
/// with XPST0010 (#54).
/// </remarks>
internal static class PathLowerings
{
    /// <summary>Registers the lowerings of this folder.</summary>
    /// <param name="registry">The registry.</param>
    /// <exception cref="System.ArgumentNullException"><paramref name="registry"/> is <see langword="null"/>.</exception>
    /// <exception cref="System.InvalidOperationException">A node kind already has a lowering.</exception>
    public static void Register(LoweringRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);

        registry.Register<PathExpression>(static (node, context) => LowerPath(node, context));
        registry.Register<AxisStep>(
            static (node, context) => new PathNode(PathRoot.Relative, [LowerAxisStep(node, context)], node.Location)
        );
        registry.Register<FilterExpression>(
            static (node, context) =>
                new FilterNode(context.Lower(node.Primary), LowerPredicates(node.Predicates, context))
        );
        registry.Register<ContextItemExpression>(static (_, _) => new ContextItemNode());
        registry.Register<SetExpression>(
            static (node, context) =>
                new SetOperationNode(node.Operator, context.Lower(node.Left), context.Lower(node.Right))
        );
    }

    private static PathNode LowerPath(PathExpression node, LoweringContext context)
    {
        var syntax = new List<Expression>(node.Steps.Count + 1);
        if (node.Root == PathRoot.RootDescendants)
        {
            syntax.Add(new AxisStep(node.Location, Axis.DescendantOrSelf, new AnyKindTest(node.Location), []));
        }

        for (var i = 0; i < node.Steps.Count; i++)
        {
            syntax.Add(node.Steps[i]);
        }

        var steps = new List<PathStep>(syntax.Count);
        for (var i = 0; i < syntax.Count; i++)
        {
            if (IsDescendantAttribute(syntax, i, context, out var attributeTest))
            {
                steps.Add(new DescendantAttributeStep(attributeTest));
                i++;
            }
            else if (IsDescendantUnion(syntax, i, context, out var union))
            {
                steps.Add(new ExpressionPathStep(context.Lower(union), isContextItem: false));
                i++;
            }
            else if (IsDescendantShortcut(syntax, i, context))
            {
                var child = (AxisStep)syntax[i + 1];
                steps.Add(
                    LowerAxisStep(new AxisStep(child.Location, Axis.Descendant, child.Test, child.Predicates), context)
                );
                i++;
            }
            else
            {
                steps.Add(LowerStep(syntax[i], context));
            }
        }

        return new PathNode(node.Root, steps.ToArray(), node.Location);
    }

    // descendant-or-self::node()/child::T, with no predicate on the first step and none on the second that can select by position.
    private static bool IsDescendantShortcut(List<Expression> steps, int index, LoweringContext context) =>
        IsDescendantOrSelfNode(steps, index)
        && steps[index + 1] is AxisStep { Axis: Axis.Child } child
        && Selects(child, context);

    // descendant-or-self::node()/attribute::T without predicates, T a plain node test: the attributes of the elements, in one walk.
    private static bool IsDescendantAttribute(
        List<Expression> steps,
        int index,
        LoweringContext context,
        out XPathNodeTest test
    )
    {
        test = default;

        return IsDescendantOrSelfNode(steps, index)
            && steps[index + 1] is AxisStep { Axis: Axis.Attribute, Predicates.Count: 0 } attribute
            && XPathNodeTest.TryFromSyntax(attribute.Test, Axis.Attribute, context.StaticContext, out test);
    }

    // descendant-or-self::node()/(A|B|...) where every operand is a child step that may become a descendant step: the union of the
    // descendant steps. Each node of the document then does not evaluate a union of its own.
    private static bool IsDescendantUnion(
        List<Expression> steps,
        int index,
        LoweringContext context,
        out Expression union
    )
    {
        union = null!;
        if (
            !IsDescendantOrSelfNode(steps, index)
            || steps[index + 1]
                is not ParenthesizedExpression { Inner: SetExpression { Operator: SetOperator.Union } set }
        )
        {
            return false;
        }

        var rewritten = Descendants(set, context);
        if (rewritten is null)
        {
            return false;
        }

        union = rewritten;
        return true;
    }

    // The same union over descendant steps, or null when an operand is not a child step that Selects as a descendant step.
    private static Expression? Descendants(Expression expression, LoweringContext context)
    {
        switch (expression)
        {
            case ParenthesizedExpression { Inner: { } inner }:
                return Descendants(inner, context);
            case SetExpression { Operator: SetOperator.Union } set:
                var left = Descendants(set.Left, context);
                var right = left is null ? null : Descendants(set.Right, context);

                return right is null ? null : new SetExpression(set.Location, SetOperator.Union, left!, right);
            case AxisStep { Axis: Axis.Child } child when Selects(child, context):
                return new AxisStep(child.Location, Axis.Descendant, child.Test, child.Predicates);
            default:
                return null;
        }
    }

    private static bool IsDescendantOrSelfNode(List<Expression> steps, int index) =>
        index + 1 < steps.Count
        && steps[index] is AxisStep { Axis: Axis.DescendantOrSelf, Test: AnyKindTest, Predicates.Count: 0 };

    // A child step without a predicate, or with predicates that cannot select by position: the candidates are the same nodes whether the
    // step runs per node or once over the descendants.
    private static bool Selects(AxisStep child, LoweringContext context) =>
        child.Predicates.Count == 0
        || NonPositionalPredicates.All(
            child.Predicates,
            context.StaticContext.DefaultFunctionNamespace,
            XPathStaticContext.Default.DefaultFunctionNamespace
        );

    private static PathStep LowerStep(Expression step, LoweringContext context) =>
        step switch
        {
            AxisStep axisStep => LowerAxisStep(axisStep, context),
            _ => new ExpressionPathStep(context.Lower(step), step is ContextItemExpression),
        };

    private static AxisPathStep LowerAxisStep(AxisStep step, LoweringContext context)
    {
        var location = step.Test.Location;
        if (step.Axis == Axis.Namespace)
        {
            throw XPathErrors.Exception(XPathErrorCodes.XPST0010, step.Location.Offset, step.Location.Length);
        }

        ItemTypeMatcher? typed = null;
        if (!XPathNodeTest.TryFromSyntax(step.Test, step.Axis, context.StaticContext, out var test))
        {
            // A kind test that carries a type name, or names a schema declaration: the item type matcher knows both.
            test = XPathNodeTest.AnyNode;
            typed = ItemTypeMatcher.Bind(new KindItemType(location, (KindTest)step.Test), context.StaticContext);
        }

        return new AxisPathStep(step.Axis, test, typed, LowerPredicates(step.Predicates, context));
    }

    private static ExecutableNode[] LowerPredicates(IReadOnlyList<Expression> predicates, LoweringContext context)
    {
        var lowered = new ExecutableNode[predicates.Count];
        for (var i = 0; i < lowered.Length; i++)
        {
            lowered[i] = context.Lower(predicates[i]);
        }

        return lowered;
    }
}
