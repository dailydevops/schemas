namespace NetEvolve.Schemas.XPath.Tests.Unit.Functions.Binding;

using NetEvolve.Schemas.XPath.Api;

/// <summary>A host function of the public contract: it declares no parameter types and returns its first argument unchanged.</summary>
internal sealed class FakePlainHostFunction : XPathHostFunction
{
    public FakePlainHostFunction(string local, int arity)
        : base(new XPathQName(FakeBindingFunction.Namespace, local), arity, arity) { }

    public override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation)
    {
        invocation.Result = invocation.Arguments[0];
        return XPathEvaluationStatus.Ok;
    }
}
