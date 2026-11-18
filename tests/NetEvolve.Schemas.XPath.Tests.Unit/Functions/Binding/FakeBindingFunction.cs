namespace NetEvolve.Schemas.XPath.Tests.Unit.Functions.Binding;

using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Functions.Binding;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>A fake function with a declared signature: it returns a fixed result, or its first argument, or fails with a code.</summary>
internal sealed class FakeBindingFunction : XPathFunction
{
    public const string Namespace = "urn:fake";

    private readonly XdmSequence? _fixedResult;
    private readonly XPathErrorCode? _failWith;

    public FakeBindingFunction(
        string local,
        int minArity,
        int maxArity,
        XPathParameterType[] parameters,
        XdmSequence? fixedResult = null,
        XPathErrorCode? failWith = null,
        string? ns = null
    )
        : base(new XPathQName(ns ?? Namespace, local), minArity, maxArity, parameters)
    {
        _fixedResult = fixedResult;
        _failWith = failWith;
    }

    public XPathVersionMask VersionMask { get; init; } = XPathVersionMask.All;

    public XPathHostRequirements Requires { get; init; }

    public override XPathVersionMask Versions => VersionMask;

    public override XPathHostRequirements HostRequirements => Requires;

    public override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation)
    {
        if (_failWith is { } code)
        {
            invocation.Diagnostic = XPathDiagnostic.Create(code);
            return XPathEvaluationStatus.Error;
        }

        invocation.Result = _fixedResult ?? (invocation.Arguments.Length > 0 ? invocation.Arguments[0] : default);
        return XPathEvaluationStatus.Ok;
    }
}
