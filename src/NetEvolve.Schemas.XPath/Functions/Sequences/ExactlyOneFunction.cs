namespace NetEvolve.Schemas.XPath.Functions.Sequences;

using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Errors;

/// <summary><c>fn:exactly-one</c>: the argument, or FORG0005 when it does not have exactly one item. The check belongs to the function, so the parameter is declared <c>item()*</c>.</summary>
internal sealed class ExactlyOneFunction : SequenceFunction
{
    /// <summary>Initializes a new instance of the <see cref="ExactlyOneFunction"/> class.</summary>
    public ExactlyOneFunction()
        : base("exactly-one", 1, 1, [Items]) { }

    /// <inheritdoc />
    /// <exception cref="XPathException">The argument has no item or more than one (FORG0005).</exception>
    public override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation)
    {
        var argument = invocation.Arguments[0];
        if (argument.IsUnreadLazy)
        {
            argument = SequenceStreaming.TakeUpToTwo(argument);
        }

        invocation.Result = argument.Count == 1 ? argument : throw XPathErrors.Exception(XPathErrorCodes.FORG0005);
        return XPathEvaluationStatus.Ok;
    }
}
