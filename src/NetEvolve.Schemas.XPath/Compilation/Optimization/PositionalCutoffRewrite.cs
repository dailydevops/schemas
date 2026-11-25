namespace NetEvolve.Schemas.XPath.Compilation.Optimization;

using System.Collections.Generic;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Evaluation.Comparison;
using NetEvolve.Schemas.XPath.Evaluation.Flow;
using NetEvolve.Schemas.XPath.Evaluation.Paths;

/// <summary>
/// Turns a positional predicate at the front of a predicate list into a <see cref="PositionalCutoff"/> (XPath 2.0, 3.2.2): <c>[N]</c>,
/// <c>[position() op N]</c> over an integer literal, and <c>[last()]</c>. The cutoff keeps the positions it names without evaluating a predicate
/// per item, and a bounded one stops the walk of a step, or of the path that a filter expression reads.
/// </summary>
/// <remarks>
/// Only the first predicate qualifies, because the position of an item counts among the items the predicates before it kept. Limitation: a
/// literal beyond a billion, a decimal or double literal, and a comparison with <c>position()</c> on the right are left as predicates.
/// </remarks>
internal static class PositionalCutoffRewrite
{
    private const long Bound = 1_000_000_000;

    /// <summary>Rewrites a filter expression or a path.</summary>
    /// <param name="node">The lowered node.</param>
    /// <returns>The rewritten node, or the node itself.</returns>
    public static ExecutableNode Rewrite(ExecutableNode node) =>
        node switch
        {
            FilterNode { Cutoff: null } filter when filter.Predicates.Count > 0 => RewriteFilter(filter),
            PathNode path => RewritePath(path),
            _ => node,
        };

    /// <summary>Recognizes a positional predicate.</summary>
    /// <param name="predicate">The lowered predicate.</param>
    /// <param name="cutoff">The cutoff when the result is <see langword="true"/>.</param>
    /// <returns><see langword="true"/> when the predicate selects by position over literals only.</returns>
    public static bool TryRecognize(ExecutableNode predicate, out PositionalCutoff cutoff)
    {
        cutoff = default;
        switch (predicate)
        {
            case ConstantNode constant when TryGetInteger(constant, out var number):
                cutoff = number < 1 ? PositionalCutoff.Range(1, 0) : PositionalCutoff.Range((int)number, (int)number);
                return true;
            case not null when BuiltInCall.Is(predicate, "last", 0):
                cutoff = PositionalCutoff.LastItem();
                return true;
            case ValueComparisonNode { Right: ConstantNode bound } value
                when BuiltInCall.Is(value.Left, "position", 0) && TryGetInteger(bound, out var number):
                return TryRelate(value.Relation, number, out cutoff);
            case GeneralComparisonNode { Right: ConstantNode bound } general
                when BuiltInCall.Is(general.Left, "position", 0) && TryGetInteger(bound, out var number):
                return TryRelate(general.Relation, number, out cutoff);
            default:
                return false;
        }
    }

    private static bool TryGetInteger(ConstantNode constant, out long number)
    {
        number = 0;
        return constant.Value.Count == 1
            && constant.Value[0].TryGetInteger(out number)
            && number is > -Bound and < Bound;
    }

    private static bool TryRelate(ComparisonRelation relation, long number, out PositionalCutoff cutoff)
    {
        var n = (int)number;
        switch (relation)
        {
            case ComparisonRelation.Equal:
                cutoff = n < 1 ? PositionalCutoff.Range(1, 0) : PositionalCutoff.Range(n, n);
                return true;
            case ComparisonRelation.LessThanOrEqual:
                cutoff = PositionalCutoff.Range(1, n);
                return true;
            case ComparisonRelation.LessThan:
                cutoff = PositionalCutoff.Range(1, n - 1);
                return true;
            case ComparisonRelation.GreaterThan:
                cutoff = PositionalCutoff.Range(n < 0 ? 1 : n + 1, int.MaxValue);
                return true;
            case ComparisonRelation.GreaterThanOrEqual:
                cutoff = PositionalCutoff.Range(n < 1 ? 1 : n, int.MaxValue);
                return true;
            default:
                cutoff = default;
                return false;
        }
    }

    private static FilterNode RewriteFilter(FilterNode filter)
    {
        if (!TryRecognize(filter.Predicates[0], out var cutoff))
        {
            return filter;
        }

        // A bounded cutoff counts positions of the primary expression in its own order, so a path that can stop early reads that many nodes.
        if (cutoff.Budget > 0 && filter.Primary is PathNode path)
        {
            _ = path.TryLimit(cutoff.Budget);
        }

        return new FilterNode(filter.Primary, Skip(filter.Predicates), cutoff);
    }

    private static PathNode RewritePath(PathNode path)
    {
        PathStep[]? steps = null;
        for (var i = 0; i < path.Steps.Count; i++)
        {
            if (
                path.Steps[i] is AxisPathStep { Cutoff: null } step
                && step.Predicates.Count > 0
                && TryRecognize(step.Predicates[0], out var cutoff)
            )
            {
                steps ??= Copy(path.Steps);
                steps[i] = step.With(Skip(step.Predicates), cutoff);
            }
        }

        return steps is null ? path : path.WithSteps(steps);
    }

    private static PathStep[] Copy(IReadOnlyList<PathStep> steps)
    {
        var copy = new PathStep[steps.Count];
        for (var i = 0; i < copy.Length; i++)
        {
            copy[i] = steps[i];
        }

        return copy;
    }

    private static ExecutableNode[] Skip(IReadOnlyList<ExecutableNode> predicates)
    {
        var rest = new ExecutableNode[predicates.Count - 1];
        for (var i = 0; i < rest.Length; i++)
        {
            rest[i] = predicates[i + 1];
        }

        return rest;
    }
}
