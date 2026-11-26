namespace NetEvolve.Schemas.XPath.Evaluation.Flow;

using NetEvolve.Arguments;
using NetEvolve.Schemas.XPath.Compilation;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The executable form of a conditional expression (XPath 2.0, 3.8): the effective boolean value of the condition picks one branch, and
/// only that branch is evaluated.
/// </summary>
internal sealed class IfNode : ExecutableNode
{
    private readonly ExecutableNode _condition;
    private readonly ExecutableNode _then;
    private readonly ExecutableNode _else;

    /// <summary>Initializes a new instance of the <see cref="IfNode"/> class.</summary>
    /// <param name="condition">The condition.</param>
    /// <param name="then">The branch taken when the condition is true.</param>
    /// <param name="else">The branch taken when the condition is false.</param>
    /// <exception cref="System.ArgumentNullException">An argument is <see langword="null"/>.</exception>
    public IfNode(ExecutableNode condition, ExecutableNode then, ExecutableNode @else)
    {
        ArgumentNullException.ThrowIfNull(condition);
        ArgumentNullException.ThrowIfNull(then);
        ArgumentNullException.ThrowIfNull(@else);

        _condition = condition;
        _then = then;
        _else = @else;
    }

    /// <summary>Gets the condition.</summary>
    public ExecutableNode Condition => _condition;

    /// <summary>Gets the branch taken when the condition is true.</summary>
    public ExecutableNode Then => _then;

    /// <summary>Gets the branch taken when the condition is false.</summary>
    public ExecutableNode Else => _else;

    /// <inheritdoc />
    /// <exception cref="XPathException">The condition has no effective boolean value (FORG0006), or an evaluated operand raises an error.</exception>
    public override XdmSequence Evaluate(XPathEvaluationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return _condition.EvaluateBoolean(context) ? _then.Evaluate(context) : _else.Evaluate(context);
    }

    /// <inheritdoc />
    /// <exception cref="XPathException">The condition or the chosen branch has no effective boolean value (FORG0006), or an evaluated operand raises an error.</exception>
    public override bool EvaluateBoolean(XPathEvaluationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return _condition.EvaluateBoolean(context) ? _then.EvaluateBoolean(context) : _else.EvaluateBoolean(context);
    }
}
