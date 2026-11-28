namespace NetEvolve.Schemas.XPath.Functions.Sequences;

using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Errors;

/// <summary><c>fn:zero-or-one</c>: the argument, or FORG0003 when it has more than one item. The check belongs to the function, so the parameter is declared <c>item()*</c>.</summary>
internal sealed class ZeroOrOneFunction : SequenceFunction
{
    /// <summary>Initializes a new instance of the <see cref="ZeroOrOneFunction"/> class.</summary>
    public ZeroOrOneFunction()
        : base("zero-or-one", 1, 1, [Items]) { }

    /// <inheritdoc />
    /// <exception cref="XPathException">The argument has more than one item (FORG0003).</exception>
    public override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation)
    {
        var argument = invocation.Arguments[0];
        if (argument.IsUnreadLazy)
        {
            argument = SequenceStreaming.TakeUpToTwo(argument);
        }

        invocation.Result = argument.Count <= 1 ? argument : throw XPathErrors.Exception(XPathErrorCodes.FORG0003);
        return XPathEvaluationStatus.Ok;
    }
}
