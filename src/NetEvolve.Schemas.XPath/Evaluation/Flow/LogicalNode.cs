namespace NetEvolve.Schemas.XPath.Evaluation.Flow;

using NetEvolve.Arguments;
using NetEvolve.Schemas.XPath.Compilation;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Evaluation.Atomization;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The executable form of <see langword="and"/> and <see langword="or"/> (XPath 2.0, 3.6): the operands are reduced to their effective boolean values, and the
/// second one is evaluated only when the first does not decide the result.
/// </summary>
/// <remarks>
/// XPath 2.0 (2.3.4) lets an implementation evaluate the operands in either order and skip an operand whose error the other one hides.
/// The first operand is always evaluated first here, so an error in it is raised even when the second operand would decide.
/// </remarks>
internal sealed class LogicalNode : ExecutableNode
{
    private readonly bool _decisive;
    private readonly ExecutableNode _left;
    private readonly ExecutableNode _right;

    /// <summary>Initializes a new instance of the <see cref="LogicalNode"/> class.</summary>
    /// <param name="operator">The operator.</param>
    /// <param name="left">The first operand.</param>
    /// <param name="right">The second operand.</param>
    /// <exception cref="System.ArgumentNullException">An operand is <see langword="null"/>.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="operator"/> is not a defined operator.</exception>
    public LogicalNode(LogicalOperator @operator, ExecutableNode left, ExecutableNode right)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan((int)@operator, (int)LogicalOperator.Or, nameof(@operator));
        ArgumentOutOfRangeException.ThrowIfGreaterThan((int)@operator, (int)LogicalOperator.And, nameof(@operator));
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);

        // The value of the first operand that settles the result: true for or, false for and.
        _decisive = @operator == LogicalOperator.Or;
        _left = left;
        _right = right;
    }

    /// <summary>Gets the first operand.</summary>
    public ExecutableNode Left => _left;

    /// <summary>Gets the second operand.</summary>
    public ExecutableNode Right => _right;

    /// <summary>Creates the same logical expression over other operands.</summary>
    /// <param name="left">The first operand.</param>
    /// <param name="right">The second operand.</param>
    /// <returns>The logical expression.</returns>
    public LogicalNode WithOperands(ExecutableNode left, ExecutableNode right) =>
        new(_decisive ? LogicalOperator.Or : LogicalOperator.And, left, right);

    /// <inheritdoc />
    /// <exception cref="XPathException">An evaluated operand has no effective boolean value (FORG0006), or raises an error.</exception>
    public override XdmSequence Evaluate(XPathEvaluationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var first = XPathAtomization.EffectiveBooleanValue(_left.Evaluate(context));
        var value = first == _decisive ? _decisive : XPathAtomization.EffectiveBooleanValue(_right.Evaluate(context));
        return XdmSequence.Create(XdmItem.FromBoolean(value));
    }
}
