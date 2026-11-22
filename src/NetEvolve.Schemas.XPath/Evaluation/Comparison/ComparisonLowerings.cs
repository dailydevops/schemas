namespace NetEvolve.Schemas.XPath.Evaluation.Comparison;

using NetEvolve.Arguments;
using NetEvolve.Schemas.XPath.Compatibility;
using NetEvolve.Schemas.XPath.Compilation;
using NetEvolve.Schemas.XPath.Syntax.Tree;

/// <summary>The registration file of this folder: the lowering of the comparison expressions of the three kinds.</summary>
/// <remarks>
/// The lowered nodes read the operator table at evaluation, so the entries that other folders add to the same table, before or after
/// this registration, are found. The composition root (XP-19) creates one table and passes it to every folder that registers
/// operators; no such root exists yet. When XPath 1.0 compatibility mode is on, a general comparison lowers to
/// <see cref="CompatibilityGeneralComparisonNode"/> (XPath 2.0, 3.5.2) instead of <see cref="GeneralComparisonNode"/>; the value and node
/// comparisons do not depend on the mode.
/// </remarks>
internal static class ComparisonLowerings
{
    /// <summary>Registers the lowering of this folder.</summary>
    /// <param name="registry">The registry.</param>
    /// <param name="table">The operator table that the value and general comparisons dispatch through.</param>
    /// <exception cref="System.ArgumentNullException">An argument is <see langword="null"/>.</exception>
    /// <exception cref="System.InvalidOperationException">The comparison expression already has a lowering.</exception>
    public static void Register(LoweringRegistry registry, ComparisonOperatorTable table)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(table);

        registry.Register<ComparisonExpression>((node, context) => Lower(node, context, table));
    }

    private static ExecutableNode Lower(
        ComparisonExpression node,
        LoweringContext context,
        ComparisonOperatorTable table
    )
    {
        ArgumentNullException.ThrowIfNull(node);
        ArgumentNullException.ThrowIfNull(context);

        var left = context.Lower(node.Left);
        var right = context.Lower(node.Right);
        return node.Operator switch
        {
            >= ComparisonOperator.NodeIs => new NodeComparisonNode(node.Operator, left, right),
            >= ComparisonOperator.ValueEqual => new ValueComparisonNode(node.Operator, left, right, table),
            _ => context.StaticContext.XPath10Compatibility
                ? new CompatibilityGeneralComparisonNode(node.Operator, left, right, table)
                : new GeneralComparisonNode(node.Operator, left, right, table),
        };
    }
}
