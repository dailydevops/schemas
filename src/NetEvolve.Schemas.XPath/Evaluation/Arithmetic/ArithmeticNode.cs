namespace NetEvolve.Schemas.XPath.Evaluation.Arithmetic;

using NetEvolve.Arguments;
using NetEvolve.Schemas.XPath.Compilation;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Evaluation.Atomization;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The executable form of a binary arithmetic expression (XPath 2.0, 3.4): the operands are atomized; an empty operand gives the
/// empty sequence, more than one item is XPTY0004, <c>xs:untypedAtomic</c> is cast to <c>xs:double</c>, and the implementation that
/// the operator table holds for the operand types computes the result. A pair of types without an entry is XPTY0004.
/// </summary>
/// <remarks>Dynamic errors carry no location, as the error model defines for run time.</remarks>
internal sealed class ArithmeticNode : ExecutableNode
{
    private readonly ArithmeticOperator _operator;
    private readonly ExecutableNode _left;
    private readonly ExecutableNode _right;
    private readonly ArithmeticOperatorTable _table;

    /// <summary>Initializes a new instance of the <see cref="ArithmeticNode"/> class.</summary>
    /// <param name="operator">The operator.</param>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <param name="table">The operator table; entries added to it later are seen.</param>
    /// <exception cref="System.ArgumentNullException">An argument is <see langword="null"/>.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="operator"/> is not a defined operator.</exception>
    public ArithmeticNode(
        ArithmeticOperator @operator,
        ExecutableNode left,
        ExecutableNode right,
        ArithmeticOperatorTable table
    )
    {
        Argument.ThrowIfLessThan((int)@operator, (int)ArithmeticOperator.Add, nameof(@operator));
        Argument.ThrowIfGreaterThan((int)@operator, (int)ArithmeticOperator.Modulo, nameof(@operator));
        Argument.ThrowIfNull(left);
        Argument.ThrowIfNull(right);
        Argument.ThrowIfNull(table);

        _operator = @operator;
        _left = left;
        _right = right;
        _table = table;
    }

    /// <inheritdoc />
    /// <exception cref="XPathException">An operand is not a single number (XPTY0004), an <c>xs:untypedAtomic</c> operand is not a double (FORG0001), or the operator raises a dynamic error (FOAR0001, FOAR0002).</exception>
    public override XdmSequence Evaluate(XPathEvaluationContext context)
    {
        Argument.ThrowIfNull(context);

        var left = XPathAtomization.Atomize(_left.Evaluate(context));
        var right = XPathAtomization.Atomize(_right.Evaluate(context));
        if (left.IsEmpty || right.IsEmpty)
        {
            return XdmSequence.Empty;
        }

        var leftItem = ArithmeticOperands.Single(left);
        var rightItem = ArithmeticOperands.Single(right);
        return _table.TryGet(_operator, leftItem.TypeId, rightItem.TypeId, out var implementation)
            ? XdmSequence.Create(implementation!(leftItem, rightItem))
            : throw XPathErrors.Exception(XPathErrorCodes.XPTY0004);
    }
}
