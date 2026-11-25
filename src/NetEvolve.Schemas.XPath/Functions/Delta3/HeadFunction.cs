namespace NetEvolve.Schemas.XPath.Functions.Delta3;

using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary><c>fn:head</c> (Functions and Operators 3.1, 14.1.1): the first item of the sequence, the empty sequence for the empty sequence.</summary>
internal sealed class HeadFunction : Delta3Function
{
    /// <summary>Initializes a new instance of the <see cref="HeadFunction"/> class.</summary>
    public HeadFunction()
        : base("head", 1, 1, [Items]) { }

    /// <inheritdoc />
    public override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation)
    {
        var argument = invocation.Arguments[0];
        if (!argument.IsEmpty)
        {
            invocation.Result = XdmSequence.Create(argument[0]);
        }

        return XPathEvaluationStatus.Ok;
    }
}
