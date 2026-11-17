namespace NetEvolve.Schemas.XPath.Tests.Unit.Fakes;

using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>A fake function group member: returns a fixed string, or the string length of its first argument.</summary>
internal sealed class FakeFunction : XPathHostFunction
{
    public const string Namespace = "urn:fake";

    private readonly string? _result;

    public FakeFunction(string local, int minArity, int maxArity, string? result = null)
        : base(new XPathQName(Namespace, local), minArity, maxArity) => _result = result;

    public XPathVersionMask VersionMask { get; init; } = XPathVersionMask.All;

    public override XPathVersionMask Versions => VersionMask;

    public override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation)
    {
        if (_result is not null)
        {
            invocation.Result = XdmSequence.Create(XdmItem.FromString(_result));
            return XPathEvaluationStatus.Ok;
        }

        var text = string.Empty;
        if (invocation.Arguments[0].Count > 0)
        {
            var item = invocation.Arguments[0][0];
            text =
                item.TryGetNode(out var node) ? node.Node.StringValue
                : item.TryGetString(out var value) ? value ?? string.Empty
                : string.Empty;
        }

        invocation.Result = XdmSequence.Create(XdmItem.FromInteger(text.Length));
        return XPathEvaluationStatus.Ok;
    }
}
