namespace NetEvolve.Schemas.XPath.Tests.Unit.Abstractions;

using System;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>A host function that holds one immutable constant and reads the run state through the host slot.</summary>
internal sealed class StateFunction(XPathQName name, long constant) : XPathHostFunction(name, 0, 0)
{
    public override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation)
    {
        var state = invocation.Context.Host.HostState;
        var value = state is Func<long> body ? body() : (long)state!;
        invocation.Result = XdmSequence.Create(XdmItem.FromInteger(constant + value));
        return XPathEvaluationStatus.Ok;
    }
}
