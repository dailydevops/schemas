namespace NetEvolve.Schemas.XPath.Tests.Unit.Api;

using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>A host function that returns one fixed integer, whatever its arguments.</summary>
internal sealed class ConstantFunction(
    XPathQName name,
    int minArity,
    int maxArity,
    long value,
    XPathVersionMask versions = XPathVersionMask.All,
    XPathHostRequirements requirements = XPathHostRequirements.None
) : XPathHostFunction(name, minArity, maxArity)
{
    public override XPathVersionMask Versions => versions;

    public override XPathHostRequirements HostRequirements => requirements;

    public override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation)
    {
        invocation.Result = XdmSequence.Create(XdmItem.FromInteger(value));
        return XPathEvaluationStatus.Ok;
    }
}
