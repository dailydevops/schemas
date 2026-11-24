namespace NetEvolve.Schemas.XPath.Evaluation.Arithmetic;

using NetEvolve.Arguments;
using NetEvolve.Schemas.XPath.Compilation;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Evaluation.Atomization;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The executable form of a unary plus or minus (XPath 2.0, 3.4): the operand is prepared like a binary operand, and the
/// implementation that the operator table holds for its type computes the result. A type without an entry is XPTY0004.
/// </summary>
/// <remarks>Dynamic errors carry no location, as the error model defines for run time.</remarks>
internal sealed class UnaryArithmeticNode : ExecutableNode
{
    private readonly UnaryOperator _operator;
    private readonly ExecutableNode _operand;
    private readonly ArithmeticOperatorTable _table;

    /// <summary>Initializes a new instance of the <see cref="UnaryArithmeticNode"/> class.</summary>
    /// <param name="operator">The operator.</param>
    /// <param name="operand">The operand.</param>
    /// <param name="table">The operator table; entries added to it later are seen.</param>
    /// <exception cref="System.ArgumentNullException">An argument is <see langword="null"/>.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="operator"/> is not a defined operator.</exception>
    public UnaryArithmeticNode(UnaryOperator @operator, ExecutableNode operand, ArithmeticOperatorTable table)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan((int)@operator, (int)UnaryOperator.Plus, nameof(@operator));
        ArgumentOutOfRangeException.ThrowIfGreaterThan((int)@operator, (int)UnaryOperator.Minus, nameof(@operator));
        ArgumentNullException.ThrowIfNull(operand);
        ArgumentNullException.ThrowIfNull(table);

        _operator = @operator;
        _operand = operand;
        _table = table;
    }

    /// <summary>Gets the operand.</summary>
    public ExecutableNode Operand => _operand;

    /// <inheritdoc />
    /// <exception cref="XPathException">The operand is not a single number (XPTY0004), or an <c>xs:untypedAtomic</c> operand is not a double (FORG0001).</exception>
    public override XdmSequence Evaluate(XPathEvaluationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var operand = XPathAtomization.Atomize(_operand.Evaluate(context));
        if (operand.IsEmpty)
        {
            return XdmSequence.Empty;
        }

        var item = ArithmeticOperands.Single(operand);
        return _table.TryGet(_operator, item.TypeId, out var implementation)
            ? XdmSequence.Create(implementation!(item))
            : throw XPathErrors.Exception(XPathErrorCodes.XPTY0004);
    }
}
