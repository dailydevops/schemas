namespace NetEvolve.Schemas.XPath.Evaluation.Comparison;

using NetEvolve.Arguments;
using NetEvolve.Schemas.XPath.Compilation;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The executable form of a node comparison (XPath 2.0, 3.5.3): an empty operand gives the empty sequence, each operand must
/// otherwise be a single node (XPTY0004), and the result tests the identity of the two nodes (<see langword="is"/>) or their order in document
/// order (<c>&lt;&lt;</c> and <c>&gt;&gt;</c>). The operands are not atomized.
/// </summary>
/// <remarks>Nodes of two trees order by a stable property of the tree, as the data model requires. Allocates nothing.</remarks>
internal sealed class NodeComparisonNode : ExecutableNode
{
    private readonly ComparisonOperator _operator;
    private readonly ExecutableNode _left;
    private readonly ExecutableNode _right;

    /// <summary>Initializes a new instance of the <see cref="NodeComparisonNode"/> class.</summary>
    /// <param name="operator">The operator, one of is, &lt;&lt;, and &gt;&gt;.</param>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <exception cref="System.ArgumentNullException">An argument is <see langword="null"/>.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="operator"/> is not a node comparison operator.</exception>
    public NodeComparisonNode(ComparisonOperator @operator, ExecutableNode left, ExecutableNode right)
    {
        Argument.ThrowIfLessThan((int)@operator, (int)ComparisonOperator.NodeIs, nameof(@operator));
        Argument.ThrowIfGreaterThan((int)@operator, (int)ComparisonOperator.NodeFollows, nameof(@operator));
        Argument.ThrowIfNull(left);
        Argument.ThrowIfNull(right);

        _operator = @operator;
        _left = left;
        _right = right;
    }

    /// <inheritdoc />
    /// <exception cref="XPathException">An operand is not a single node (XPTY0004).</exception>
    public override XdmSequence Evaluate(XPathEvaluationContext context)
    {
        Argument.ThrowIfNull(context);

        var left = _left.Evaluate(context);
        var right = _right.Evaluate(context);
        if (left.IsEmpty || right.IsEmpty)
        {
            return XdmSequence.Empty;
        }

        var first = SingleNode(left);
        var second = SingleNode(right);
        var result = _operator switch
        {
            ComparisonOperator.NodeIs => first.IsSameNode(second),
            ComparisonOperator.NodePrecedes => first.CompareDocumentOrder(second) < 0,
            _ => first.CompareDocumentOrder(second) > 0,
        };
        return XdmSequence.Create(XdmItem.FromBoolean(result));
    }

    private static XPathNode SingleNode(in XdmSequence operand) =>
        operand.Count == 1 && operand[0].TryGetNode(out var node)
            ? node
            : throw XPathErrors.Exception(XPathErrorCodes.XPTY0004);
}
