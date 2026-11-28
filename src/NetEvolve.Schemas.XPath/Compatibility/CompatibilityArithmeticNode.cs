namespace NetEvolve.Schemas.XPath.Compatibility;

using NetEvolve.Schemas.XPath.Compilation;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Evaluation.Arithmetic;
using NetEvolve.Schemas.XPath.Evaluation.Atomization;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The executable form of a binary arithmetic expression in XPath 1.0 compatibility mode (XPath 2.0, 3.4): each operand is atomized; an
/// empty operand gives the <c>xs:double</c> NaN as the result, a sequence of several items is cut to its first item, a boolean, a string, a
/// decimal, a float, or an untyped value becomes an <c>xs:double</c> by <c>fn:number</c>, and the implementation that the operator table
/// holds for the operand types computes the result. A pair of types without an entry is XPTY0004. The lowering selects this node when
/// <see cref="XPathStaticContext.XPath10Compatibility"/> is on; the node of the mode without it is <see cref="ArithmeticNode"/>.
/// </summary>
/// <remarks>Dynamic errors carry no location, as the error model defines for run time. Both operands are evaluated even when the first one is empty.</remarks>
internal sealed class CompatibilityArithmeticNode : ExecutableNode
{
    private static readonly XdmSequence NaN = XdmSequence.Create(XdmItem.FromDouble(double.NaN));

    private readonly ArithmeticOperator _operator;
    private readonly ExecutableNode _left;
    private readonly ExecutableNode _right;
    private readonly ArithmeticOperatorTable _table;

    /// <summary>Initializes a new instance of the <see cref="CompatibilityArithmeticNode"/> class.</summary>
    /// <param name="operator">The operator.</param>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <param name="table">The operator table; entries added to it later are seen.</param>
    /// <exception cref="System.ArgumentNullException">An argument is <see langword="null"/>.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="operator"/> is not a defined operator.</exception>
    public CompatibilityArithmeticNode(
        ArithmeticOperator @operator,
        ExecutableNode left,
        ExecutableNode right,
        ArithmeticOperatorTable table
    )
    {
        ArgumentOutOfRangeException.ThrowIfLessThan((int)@operator, (int)ArithmeticOperator.Add, nameof(@operator));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(
            (int)@operator,
            (int)ArithmeticOperator.Modulo,
            nameof(@operator)
        );
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        ArgumentNullException.ThrowIfNull(table);

        _operator = @operator;
        _left = left;
        _right = right;
        _table = table;
    }

    /// <inheritdoc />
    /// <exception cref="XPathException">The pair of operand types has no operator (XPTY0004), or the operator raises a dynamic error (FOAR0001, FOAR0002, FODT0001, FODT0002, FOCA0005).</exception>
    public override XdmSequence Evaluate(XPathEvaluationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var left = XPathAtomization.Atomize(_left.Evaluate(context), context.Annotations);
        var right = XPathAtomization.Atomize(_right.Evaluate(context), context.Annotations);
        if (left.IsEmpty || right.IsEmpty)
        {
            return NaN;
        }

        var leftItem = CompatibilityOperands.ArithmeticOperand(left[0]);
        var rightItem = CompatibilityOperands.ArithmeticOperand(right[0]);
        return _table.TryGetContextual(_operator, leftItem.TypeId, rightItem.TypeId, out var implementation)
            ? XdmSequence.Create(implementation!(leftItem, rightItem, context))
            : throw XPathErrors.Exception(XPathErrorCodes.XPTY0004);
    }
}
