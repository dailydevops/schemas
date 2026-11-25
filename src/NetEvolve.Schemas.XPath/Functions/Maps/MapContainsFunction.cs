namespace NetEvolve.Schemas.XPath.Functions.Maps;

using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary><c>map:contains</c> (Functions and Operators 3.1, 17.1.5): whether a map has an entry with a key.</summary>
internal sealed class MapContainsFunction : MapFunction
{
    /// <summary>Initializes a new instance of the <see cref="MapContainsFunction"/> class.</summary>
    public MapContainsFunction()
        : base("contains", OneItem, OneKey) { }

    /// <inheritdoc />
    public override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation)
    {
        var map = RequireMap(invocation.Arguments[0]);
        invocation.Result = XdmSequence.Create(XdmItem.FromBoolean(map.TryGetEntry(invocation.Arguments[1][0], out _)));
        return XPathEvaluationStatus.Ok;
    }
}
