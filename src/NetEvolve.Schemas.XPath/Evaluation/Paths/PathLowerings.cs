namespace NetEvolve.Schemas.XPath.Evaluation.Paths;

using System.Collections.Generic;
using NetEvolve.Arguments;
using NetEvolve.Schemas.XPath.Compilation;
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
/// <c>descendant-or-self::node()/child::T</c> without predicates on either step becomes <c>descendant::T</c>, which keeps the same nodes
/// and needs no sort; with a predicate the expanded form stays, because a positional predicate counts among the children of each node.
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
        Argument.ThrowIfNull(registry);

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
            if (IsDescendantShortcut(syntax, i))
            {
                var child = (AxisStep)syntax[i + 1];
                steps.Add(LowerAxisStep(new AxisStep(child.Location, Axis.Descendant, child.Test, []), context));
                i++;
            }
            else
            {
                steps.Add(LowerStep(syntax[i], context));
            }
        }

        return new PathNode(node.Root, steps.ToArray(), node.Location);
    }

    // descendant-or-self::node()/child::T, with no predicate on either step.
    private static bool IsDescendantShortcut(List<Expression> steps, int index) =>
        index + 1 < steps.Count
        && steps[index] is AxisStep { Axis: Axis.DescendantOrSelf, Test: AnyKindTest, Predicates.Count: 0 }
        && steps[index + 1] is AxisStep { Axis: Axis.Child, Predicates.Count: 0 };

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
