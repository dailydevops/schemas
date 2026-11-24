namespace NetEvolve.Schemas.XPath.Compilation.Optimization;

using System.Collections.Generic;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Evaluation.Comparison;
using NetEvolve.Schemas.XPath.Evaluation.Flow;
using NetEvolve.Schemas.XPath.Evaluation.Paths;
using NetEvolve.Schemas.XPath.Functions.Sequences;

/// <summary>
/// The early exits of the optimization stage (XPath 2.0, 2.3.4): an expression that needs the first nodes of a path only asks the path for
/// them, so the path stops reading. A path qualifies when <see cref="PathNode.TryLimit"/> accepts it, which is the case when its last step is an
/// axis step without predicates that delivers nodes in document order; every other node is left alone.
/// </summary>
internal static class EarlyExitRewrites
{
    /// <summary><c>fn:exists</c> and <c>fn:empty</c> need one node of their argument.</summary>
    /// <param name="node">The lowered node.</param>
    /// <returns>The call over a limited path, or the node itself.</returns>
    public static ExecutableNode Existence(ExecutableNode node) =>
        node is CallNode { Function: ExistsFunction or EmptyFunction, Arguments.Count: 1 } call
            ? WithArgument(call, 1)
            : node;

    /// <summary>
    /// The effective boolean value of a node sequence is whether it has a node, so a condition, an operand of <see langword="and"/> or <see langword="or"/>, and a
    /// predicate that is a path need one node of it.
    /// </summary>
    /// <param name="node">The lowered node.</param>
    /// <returns>The node over limited paths, or the node itself.</returns>
    public static ExecutableNode Boolean(ExecutableNode node) =>
        node switch
        {
            IfNode condition when Limit(condition.Condition, 1) is { } limited => new IfNode(
                limited,
                condition.Then,
                condition.Else
            ),
            LogicalNode logical when Limit(logical.Left, 1) is { } || Limit(logical.Right, 1) is { } =>
                logical.WithOperands(Limit(logical.Left, 1) ?? logical.Left, Limit(logical.Right, 1) ?? logical.Right),
            FilterNode { Predicates.Count: > 0 } filter when LimitAll(filter.Predicates) is { } predicates =>
                new FilterNode(filter.Primary, predicates, filter.Cutoff),
            PathNode path => LimitSteps(path),
            _ => node,
        };

    /// <summary>
    /// A comparison of <c>fn:count</c> against an integer literal N gives the same answer for the count capped at N + 1, so the path is
    /// asked for N + 1 nodes.
    /// </summary>
    /// <param name="node">The lowered node.</param>
    /// <returns>The comparison over a limited path, or the node itself.</returns>
    public static ExecutableNode CountComparison(ExecutableNode node) =>
        node switch
        {
            ValueComparisonNode comparison when Cap(comparison.Left, comparison.Right) is { } operands =>
                comparison.WithOperands(operands.Left, operands.Right),
            GeneralComparisonNode comparison when Cap(comparison.Left, comparison.Right) is { } operands =>
                comparison.WithOperands(operands.Left, operands.Right),
            _ => node,
        };

    private static CallNode WithArgument(CallNode call, int limit) =>
        call.Arguments[0] is PathNode path && path.TryLimit(limit) is { } limited
            ? new CallNode(call.Function, [limited])
            : call;

    // The path limited to a number of nodes; null when the node is no path or the path cannot stop early.
    private static PathNode? Limit(ExecutableNode node, int limit) =>
        node is PathNode path ? path.TryLimit(limit) : null;

    private static ExecutableNode[]? LimitAll(IReadOnlyList<ExecutableNode> predicates)
    {
        ExecutableNode[]? limited = null;
        for (var i = 0; i < predicates.Count; i++)
        {
            if (Limit(predicates[i], 1) is { } path)
            {
                limited ??= Copy(predicates);
                limited[i] = path;
            }
        }

        return limited;
    }

    private static PathNode LimitSteps(PathNode path)
    {
        PathStep[]? steps = null;
        for (var i = 0; i < path.Steps.Count; i++)
        {
            if (
                path.Steps[i] is AxisPathStep { Predicates.Count: > 0 } step
                && LimitAll(step.Predicates) is { } predicates
            )
            {
                steps ??= Copy(path.Steps);
                steps[i] = step.With(predicates, step.Cutoff);
            }
        }

        return steps is null ? path : path.WithSteps(steps);
    }

    private static (ExecutableNode Left, ExecutableNode Right)? Cap(ExecutableNode left, ExecutableNode right)
    {
        if (TryCount(left, right, out var capped))
        {
            return (capped, right);
        }

        return TryCount(right, left, out capped) ? (left, capped) : null;
    }

    // The count call over a path on one side and a non-negative integer literal on the other.
    private static bool TryCount(ExecutableNode count, ExecutableNode literal, out ExecutableNode capped)
    {
        capped = count;
        if (
            count is CallNode { Function: CountFunction, Arguments.Count: 1 } call
            && literal is ConstantNode { Value.Count: 1 } constant
            && constant.Value[0].TryGetInteger(out var number)
            && number is >= 0 and < 1_000_000_000
        )
        {
            var wrapped = WithArgument(call, (int)number + 1);
            capped = wrapped;
            return !ReferenceEquals(wrapped, call);
        }

        return false;
    }

    private static T[] Copy<T>(IReadOnlyList<T> items)
    {
        var copy = new T[items.Count];
        for (var i = 0; i < copy.Length; i++)
        {
            copy[i] = items[i];
        }

        return copy;
    }
}
