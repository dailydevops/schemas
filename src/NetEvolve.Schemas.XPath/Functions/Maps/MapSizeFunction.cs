namespace NetEvolve.Schemas.XPath.Functions.Maps;

using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary><c>map:size</c> (Functions and Operators 3.1, 17.1.3): the number of entries of a map.</summary>
internal sealed class MapSizeFunction : MapFunction
{
    /// <summary>Initializes a new instance of the <see cref="MapSizeFunction"/> class.</summary>
    public MapSizeFunction()
        : base("size", OneItem) { }

    /// <inheritdoc />
    public override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation)
    {
        invocation.Result = XdmSequence.Create(XdmItem.FromInteger(RequireMap(invocation.Arguments[0]).Count));
        return XPathEvaluationStatus.Ok;
    }
}
