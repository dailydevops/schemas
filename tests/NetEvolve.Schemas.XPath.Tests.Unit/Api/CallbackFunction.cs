namespace NetEvolve.Schemas.XPath.Tests.Unit.Api;

using NetEvolve.Schemas.XPath.Api;

/// <summary>A host function that evaluates another compiled expression on the same context, as a function body does.</summary>
internal sealed class CallbackFunction(XPathQName name, XPathExpression inner) : XPathHostFunction(name, 0, 0)
{
    public override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation)
    {
        invocation.Result = inner.Evaluate(invocation.Context);
        return XPathEvaluationStatus.Ok;
    }
}
