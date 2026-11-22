namespace NetEvolve.Schemas.XPath.Evaluation.Paths;

using System;
using NetEvolve.Arguments;
using NetEvolve.Schemas.XPath.Compilation;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Evaluation.Ordering;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The executable form of <c>union</c>, <c>intersect</c>, and <c>except</c> (XPath 2.0, 3.3.3): both operands must be sequences of
/// nodes; the result is in document order without duplicates. An operand that is known to be in document order is not sorted; the
/// others are sorted and deduplicated, and the two sorted operands are merged.
/// </summary>
internal sealed class SetOperationNode : ExecutableNode, IOrderedNodeSequence
{
    private readonly SetOperator _operator;
    private readonly ExecutableNode _left;
    private readonly ExecutableNode _right;

    /// <summary>Initializes a new instance of the <see cref="SetOperationNode"/> class.</summary>
    /// <param name="operator">The operator.</param>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="operator"/> is not a defined operator.</exception>
    public SetOperationNode(SetOperator @operator, ExecutableNode left, ExecutableNode right)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan((int)@operator, (int)SetOperator.Union, nameof(@operator));
        ArgumentOutOfRangeException.ThrowIfGreaterThan((int)@operator, (int)SetOperator.Except, nameof(@operator));
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);

        _operator = @operator;
        _left = left;
        _right = right;
        LeftRequirement = RequirementOf(left);
        RightRequirement = RequirementOf(right);
    }

    /// <summary>Gets what the left operand needs before the merge.</summary>
    public XPathOrderRequirement LeftRequirement { get; }

    /// <summary>Gets what the right operand needs before the merge.</summary>
    public XPathOrderRequirement RightRequirement { get; }

    /// <inheritdoc />
    public bool IsDocumentOrdered => true;

    /// <inheritdoc />
    /// <exception cref="XPathException">An operand has an item that is not a node (XPTY0004).</exception>
    public override XdmSequence Evaluate(XPathEvaluationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var leftSource = new XdmSequenceNodeSource(_left.Evaluate(context));
        var left = XPathDocumentOrder.Normalize(ref leftSource, LeftRequirement, null);
        try
        {
            var rightSource = new XdmSequenceNodeSource(_right.Evaluate(context));
            var right = XPathDocumentOrder.Normalize(ref rightSource, RightRequirement, null);
            try
            {
                return Merge(left.Nodes, right.Nodes);
            }
            finally
            {
                right.Dispose();
            }
        }
        finally
        {
            left.Dispose();
        }
    }

    private static XPathOrderRequirement RequirementOf(ExecutableNode operand) =>
        operand is IOrderedNodeSequence { IsDocumentOrdered: true }
            ? XPathOrderRequirement.None
            : XPathOrderRequirement.Sort;

    // Both operands are in document order without duplicates, so one walk over both decides every node.
    private XdmSequence Merge(ReadOnlySpan<XPathNode> left, ReadOnlySpan<XPathNode> right)
    {
        var result = default(XdmItemList);
        try
        {
            int l = 0,
                r = 0;
            while (l < left.Length && r < right.Length)
            {
                var order = left[l].CompareDocumentOrder(right[r]);
                if (order < 0)
                {
                    AddIf(ref result, left[l++], _operator != SetOperator.Intersect);
                }
                else if (order > 0)
                {
                    AddIf(ref result, right[r++], _operator == SetOperator.Union);
                }
                else
                {
                    AddIf(ref result, left[l++], _operator != SetOperator.Except);
                    r++;
                }
            }

            for (; l < left.Length; l++)
            {
                AddIf(ref result, left[l], _operator != SetOperator.Intersect);
            }

            for (; r < right.Length; r++)
            {
                AddIf(ref result, right[r], _operator == SetOperator.Union);
            }

            return result.ToSequence();
        }
        finally
        {
            result.Dispose();
        }
    }

    private static void AddIf(ref XdmItemList result, in XPathNode node, bool condition)
    {
        if (condition)
        {
            result.Add(XdmItem.FromNode(node));
        }
    }
}
