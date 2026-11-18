namespace NetEvolve.Schemas.XPath.Functions.Sequences;

using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Errors;

/// <summary><c>fn:one-or-more</c>: the argument, or FORG0004 when it is the empty sequence. The check belongs to the function, so the parameter is declared <c>item()*</c>.</summary>
internal sealed class OneOrMoreFunction : SequenceFunction
{
    /// <summary>Initializes a new instance of the <see cref="OneOrMoreFunction"/> class.</summary>
    public OneOrMoreFunction()
        : base("one-or-more", 1, 1, [Items]) { }

    /// <inheritdoc />
    /// <exception cref="XPathException">The argument is the empty sequence (FORG0004).</exception>
    public override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation)
    {
        var argument = invocation.Arguments[0];
        invocation.Result = argument.HasItems() ? argument : throw XPathErrors.Exception(XPathErrorCodes.FORG0004);
        return XPathEvaluationStatus.Ok;
    }
}
