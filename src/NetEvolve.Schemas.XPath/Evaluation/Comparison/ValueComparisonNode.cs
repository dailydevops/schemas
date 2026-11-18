namespace NetEvolve.Schemas.XPath.Evaluation.Comparison;

using NetEvolve.Arguments;
using NetEvolve.Schemas.XPath.Compilation;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Evaluation.Atomization;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The executable form of a value comparison (XPath 2.0, 3.5.1): the operands are atomized; an empty operand gives the empty
/// sequence, more than one item is XPTY0004, <c>xs:untypedAtomic</c> is cast to <c>xs:string</c>, and the implementation that the
/// operator table holds for the operand types gives the boolean result. A pair of types without an entry is XPTY0004.
/// </summary>
/// <remarks>Dynamic errors carry no location, as the error model defines for run time.</remarks>
internal sealed class ValueComparisonNode : ExecutableNode
{
    private readonly ComparisonRelation _relation;
    private readonly ExecutableNode _left;
    private readonly ExecutableNode _right;
    private readonly ComparisonOperatorTable _table;

    /// <summary>Initializes a new instance of the <see cref="ValueComparisonNode"/> class.</summary>
    /// <param name="operator">The operator, one of eq, ne, lt, le, gt, and ge.</param>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <param name="table">The operator table; entries added to it later are seen.</param>
    /// <exception cref="System.ArgumentNullException">An argument is <see langword="null"/>.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="operator"/> is not a value comparison operator.</exception>
    public ValueComparisonNode(
        ComparisonOperator @operator,
        ExecutableNode left,
        ExecutableNode right,
        ComparisonOperatorTable table
    )
    {
        Argument.ThrowIfLessThan((int)@operator, (int)ComparisonOperator.ValueEqual, nameof(@operator));
        Argument.ThrowIfGreaterThan((int)@operator, (int)ComparisonOperator.ValueGreaterThanOrEqual, nameof(@operator));
        Argument.ThrowIfNull(left);
        Argument.ThrowIfNull(right);
        Argument.ThrowIfNull(table);

        _relation = ComparisonRelations.Of(@operator);
        _left = left;
        _right = right;
        _table = table;
    }

    /// <inheritdoc />
    /// <exception cref="XPathException">An operand has more than one item, or the operand types have no operator (XPTY0004).</exception>
    public override XdmSequence Evaluate(XPathEvaluationContext context)
    {
        Argument.ThrowIfNull(context);

        var left = XPathAtomization.Atomize(_left.Evaluate(context));
        var right = XPathAtomization.Atomize(_right.Evaluate(context));
        if (left.IsEmpty || right.IsEmpty)
        {
            return XdmSequence.Empty;
        }

        var leftItem = ComparisonOperands.ValueOperand(left);
        var rightItem = ComparisonOperands.ValueOperand(right);
        return _table.TryGet(_relation, leftItem.TypeId, rightItem.TypeId, out var implementation)
            ? XdmSequence.Create(XdmItem.FromBoolean(implementation!(_relation, leftItem, rightItem)))
            : throw XPathErrors.Exception(XPathErrorCodes.XPTY0004);
    }
}
