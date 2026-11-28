namespace NetEvolve.Schemas.XPath.Evaluation.Paths;

using System;
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
internal sealed class SetOperationNode : ExecutableNode, IOrderedNodeSequence, IItemListProducer
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

        var result = default(XdmItemList);
        try
        {
            EvaluateInto(context, ref result);
            return result.ToSequence();
        }
        finally
        {
            result.Dispose();
        }
    }

    /// <inheritdoc />
    /// <exception cref="XPathException">An operand has an item that is not a node (XPTY0004).</exception>
    public void EvaluateInto(XPathEvaluationContext context, ref XdmItemList results)
    {
        ArgumentNullException.ThrowIfNull(context);

        var left = Operand(_left, LeftRequirement, context);
        try
        {
            var right = Operand(_right, RightRequirement, context);
            try
            {
                Merge(left.Nodes, right.Nodes, ref results);
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

    // An operand that is a path or a set operation hands over its items in a pooled list; the copy into a sequence is skipped.
    private static XPathNodeBuffer Operand(
        ExecutableNode operand,
        XPathOrderRequirement requirement,
        XPathEvaluationContext context
    )
    {
        if (operand is IItemListProducer producer)
        {
            var items = default(XdmItemList);
            try
            {
                producer.EvaluateInto(context, ref items);
                var span = items.Items;
                for (var i = 0; i < span.Length; i++)
                {
                    if (!span[i].IsNode)
                    {
                        throw XPathErrors.Exception(XPathErrorCodes.XPTY0004);
                    }
                }

                var listSource = items.GetNodeSource();
                return XPathDocumentOrder.Normalize(ref listSource, requirement, null);
            }
            finally
            {
                items.Dispose();
            }
        }

        var source = new XdmSequenceNodeSource(operand.Evaluate(context));
        return XPathDocumentOrder.Normalize(ref source, requirement, null);
    }

    private static XPathOrderRequirement RequirementOf(ExecutableNode operand) =>
        operand is IOrderedNodeSequence { IsDocumentOrdered: true }
            ? XPathOrderRequirement.None
            : XPathOrderRequirement.Sort;

    // Both operands are in document order without duplicates, so one walk over both decides every node.
    private void Merge(ReadOnlySpan<XPathNode> left, ReadOnlySpan<XPathNode> right, ref XdmItemList result)
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
    }

    private static void AddIf(ref XdmItemList result, in XPathNode node, bool condition)
    {
        if (condition)
        {
            result.Add(XdmItem.FromNode(node));
        }
    }
}
