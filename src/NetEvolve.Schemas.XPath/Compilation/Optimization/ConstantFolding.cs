namespace NetEvolve.Schemas.XPath.Compilation.Optimization;

using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Casting.Core;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Evaluation.Arithmetic;
using NetEvolve.Schemas.XPath.Evaluation.Atomization;
using NetEvolve.Schemas.XPath.Evaluation.Comparison;
using NetEvolve.Schemas.XPath.Evaluation.Flow;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// Evaluates an operator over literals once, while the expression is compiled (XPath 2.0, 2.3.4). A fold that raises a dynamic error is
/// not made: the operator stays in the tree and raises the error where the specification places it, when it is evaluated, and not at all
/// when a branch that holds it is never evaluated.
/// </summary>
/// <remarks>
/// Limitation: only arithmetic, numeric comparisons, <see langword="and"/>, <see langword="or"/>, a conditional, <c>cast as</c>, <c>castable as</c>, and constructor functions over literals are folded. A comparison of strings is left to
/// the evaluation because the default collation is a dynamic setting; any other function call is never folded.
/// </remarks>
internal static class ConstantFolding
{
    /// <summary>Folds a node whose operands are literals.</summary>
    /// <param name="node">The lowered node.</param>
    /// <returns>A constant, the taken branch of a conditional, or the node itself.</returns>
    public static ExecutableNode Rewrite(ExecutableNode node) =>
        node switch
        {
            ArithmeticNode { Left: ConstantNode, Right: ConstantNode } => Fold(node),
            UnaryArithmeticNode { Operand: ConstantNode } => Fold(node),
            ValueComparisonNode { Left: ConstantNode left, Right: ConstantNode right } when IsNumber(left, right) =>
                Fold(node),
            GeneralComparisonNode { Left: ConstantNode left, Right: ConstantNode right } when IsNumber(left, right) =>
                Fold(node),
            CastNode { Operand: ConstantNode } => Fold(node),
            CastableNode { Operand: ConstantNode } => Fold(node),
            CallNode { Function: ConstructorFunction } call when IsConstant(call) => Fold(node),
            LogicalNode { Left: ConstantNode, Right: ConstantNode } => Fold(node),
            IfNode { Condition: ConstantNode condition } conditional => Branch(conditional, condition),
            _ => node,
        };

    private static bool IsConstant(CallNode call)
    {
        for (var i = 0; i < call.Arguments.Count; i++)
        {
            if (call.Arguments[i] is not ConstantNode)
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsNumber(ConstantNode left, ConstantNode right) =>
        IsNumber(left.Value) && IsNumber(right.Value);

    private static bool IsNumber(in XdmSequence value) =>
        value.Count == 1
        && (
            value[0].TryGetInteger(out _)
            || value[0].TryGetDecimal(out _)
            || value[0].TryGetDouble(out _)
            || value[0].TryGetFloat(out _)
        );

    private static ExecutableNode Fold(ExecutableNode node)
    {
        using var context = new XPathEvaluationContext();
        try
        {
            return new ConstantNode(node.Evaluate(context));
        }
        catch (XPathException)
        {
            return node;
        }
    }

    // A literal has an effective boolean value, so the branch is picked without a way to fail.
    private static ExecutableNode Branch(IfNode node, ConstantNode condition) =>
        XPathAtomization.EffectiveBooleanValue(condition.Value) ? node.Then : node.Else;
}
