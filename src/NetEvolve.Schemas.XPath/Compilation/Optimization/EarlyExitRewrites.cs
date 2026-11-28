namespace NetEvolve.Schemas.XPath.Compilation.Optimization;

using System.Collections.Generic;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Evaluation.Comparison;
using NetEvolve.Schemas.XPath.Evaluation.Flow;
using NetEvolve.Schemas.XPath.Evaluation.Paths;

/// <summary>
/// The early exits of the optimization stage (XPath 2.0, 2.3.4): an expression that needs the first nodes of a path only asks the path for
/// them, so the path stops reading. A path qualifies when <see cref="PathNode.TryLimit"/> accepts it, which is the case when its last step is an
/// axis step without predicates that delivers nodes in document order; every other node is left alone. The limit is set on the path in place, so
/// a rewrite allocates nothing.
/// </summary>
internal static class EarlyExitRewrites
{
    /// <summary><c>fn:exists</c>, <c>fn:empty</c>, <c>fn:not</c> and <c>fn:boolean</c> need one node of their argument.</summary>
    /// <param name="node">The lowered node.</param>
    /// <returns>The node itself.</returns>
    public static ExecutableNode Existence(ExecutableNode node)
    {
        if (
            node is CallNode call
            && (
                BuiltInCall.Is(node, "exists", 1)
                || BuiltInCall.Is(node, "empty", 1)
                || BuiltInCall.Is(node, "not", 1)
                || BuiltInCall.Is(node, "boolean", 1)
            )
        )
        {
            Limit(call.Arguments[0], 1);
        }

        return node;
    }

    /// <summary>
    /// The effective boolean value of a node sequence is whether it has a node, so a condition, an operand of <see langword="and"/> or
    /// <see langword="or"/>, and a predicate that is a path need one node of it.
    /// </summary>
    /// <param name="node">The lowered node.</param>
    /// <returns>The node itself.</returns>
    public static ExecutableNode Boolean(ExecutableNode node)
    {
        switch (node)
        {
            case IfNode condition:
                Limit(condition.Condition, 1);
                break;
            case LogicalNode logical:
                Limit(logical.Left, 1);
                Limit(logical.Right, 1);
                break;
            case FilterNode filter:
                LimitAll(filter.Predicates);
                break;
            case PathNode path:
                for (var i = 0; i < path.Steps.Count; i++)
                {
                    if (path.Steps[i] is AxisPathStep step)
                    {
                        LimitAll(step.Predicates);
                    }
                }

                break;
            default:
                break;
        }

        return node;
    }

    /// <summary>
    /// A comparison of <c>fn:count</c> against an integer literal N gives the same answer for the count capped at N + 1, so the path is
    /// asked for N + 1 nodes.
    /// </summary>
    /// <param name="node">The lowered node.</param>
    /// <returns>The node itself.</returns>
    public static ExecutableNode CountComparison(ExecutableNode node)
    {
        switch (node)
        {
            case ValueComparisonNode comparison:
                Cap(comparison.Left, comparison.Right);
                break;
            case GeneralComparisonNode comparison:
                Cap(comparison.Left, comparison.Right);
                break;
            default:
                break;
        }

        return node;
    }

    // Asks a path for a number of nodes; nothing happens for a node that is no path or a path that cannot stop early.
    private static void Limit(ExecutableNode node, int limit)
    {
        if (node is PathNode path)
        {
            _ = path.TryLimit(limit);
        }
    }

    private static void LimitAll(IReadOnlyList<ExecutableNode> predicates)
    {
        for (var i = 0; i < predicates.Count; i++)
        {
            Limit(predicates[i], 1);
        }
    }

    // The count call over a path on one side and a non-negative integer literal on the other.
    private static void Cap(ExecutableNode left, ExecutableNode right)
    {
        if (!TryCap(left, right))
        {
            _ = TryCap(right, left);
        }
    }

    private static bool TryCap(ExecutableNode call, ExecutableNode literal)
    {
        if (
            call is CallNode count
            && BuiltInCall.Is(call, "count", 1)
            && literal is ConstantNode { Value.Count: 1 } constant
            && constant.Value[0].TryGetInteger(out var number)
            && number is >= 0 and < 1_000_000_000
        )
        {
            Limit(count.Arguments[0], (int)number + 1);
            return true;
        }

        return false;
    }
}
