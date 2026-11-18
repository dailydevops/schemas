namespace NetEvolve.Schemas.XPath.Functions.Nodes;

using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary><c>fn:position</c>: the one-based position of the context item in the focus as <c>xs:integer</c>.</summary>
internal sealed class PositionFunction : NodeFunction
{
    /// <summary>Initializes a new instance of the <see cref="PositionFunction"/> class.</summary>
    public PositionFunction()
        : base("position", 0, 0, []) { }

    /// <inheritdoc />
    public override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation)
    {
        var context = invocation.Context;
        if (context.ContextItem is null)
        {
            throw XPathErrors.Exception(XPathErrorCodes.XPDY0002);
        }

        invocation.Result = XdmSequence.Create(XdmItem.FromInteger(context.Position));
        return XPathEvaluationStatus.Ok;
    }
}
