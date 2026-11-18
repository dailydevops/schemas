namespace NetEvolve.Schemas.XPath.Functions.Nodes;

using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary><c>fn:last</c>: the size of the sequence that the context item came from, as <c>xs:integer</c>.</summary>
internal sealed class LastFunction : NodeFunction
{
    /// <summary>Initializes a new instance of the <see cref="LastFunction"/> class.</summary>
    public LastFunction()
        : base("last", 0, 0, []) { }

    /// <inheritdoc />
    public override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation)
    {
        var context = invocation.Context;
        if (context.ContextItem is null)
        {
            throw XPathErrors.Exception(XPathErrorCodes.XPDY0002);
        }

        invocation.Result = XdmSequence.Create(XdmItem.FromInteger(context.Size));
        return XPathEvaluationStatus.Ok;
    }
}
