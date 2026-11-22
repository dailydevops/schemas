namespace NetEvolve.Schemas.XPath.Compatibility;

using NetEvolve.Arguments;
using NetEvolve.Schemas.XPath.Compilation;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Evaluation.Arithmetic;
using NetEvolve.Schemas.XPath.Evaluation.Atomization;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The executable form of a unary plus or minus in XPath 1.0 compatibility mode (XPath 2.0, 3.4): the operand is prepared like an
/// operand of <see cref="CompatibilityArithmeticNode"/>, so an empty operand gives NaN, and the implementation that the operator table
/// holds for its type computes the result. A type without an entry is XPTY0004. The lowering selects this node when
/// <see cref="XPathStaticContext.XPath10Compatibility"/> is on; the node of the mode without it is <see cref="UnaryArithmeticNode"/>.
/// </summary>
/// <remarks>Dynamic errors carry no location, as the error model defines for run time.</remarks>
internal sealed class CompatibilityUnaryNode : ExecutableNode
{
    private static readonly XdmSequence NaN = XdmSequence.Create(XdmItem.FromDouble(double.NaN));

    private readonly UnaryOperator _operator;
    private readonly ExecutableNode _operand;
    private readonly ArithmeticOperatorTable _table;

    /// <summary>Initializes a new instance of the <see cref="CompatibilityUnaryNode"/> class.</summary>
    /// <param name="operator">The operator.</param>
    /// <param name="operand">The operand.</param>
    /// <param name="table">The operator table; entries added to it later are seen.</param>
    /// <exception cref="System.ArgumentNullException">An argument is <see langword="null"/>.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="operator"/> is not a defined operator.</exception>
    public CompatibilityUnaryNode(UnaryOperator @operator, ExecutableNode operand, ArithmeticOperatorTable table)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan((int)@operator, (int)UnaryOperator.Plus, nameof(@operator));
        ArgumentOutOfRangeException.ThrowIfGreaterThan((int)@operator, (int)UnaryOperator.Minus, nameof(@operator));
        ArgumentNullException.ThrowIfNull(operand);
        ArgumentNullException.ThrowIfNull(table);

        _operator = @operator;
        _operand = operand;
        _table = table;
    }

    /// <inheritdoc />
    /// <exception cref="XPathException">The type of the operand has no operator (XPTY0004).</exception>
    public override XdmSequence Evaluate(XPathEvaluationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var operand = XPathAtomization.Atomize(_operand.Evaluate(context));
        if (operand.IsEmpty)
        {
            return NaN;
        }

        var item = CompatibilityOperands.ArithmeticOperand(operand[0]);
        return _table.TryGet(_operator, item.TypeId, out var implementation)
            ? XdmSequence.Create(implementation!(item))
            : throw XPathErrors.Exception(XPathErrorCodes.XPTY0004);
    }
}
