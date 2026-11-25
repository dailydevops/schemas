namespace NetEvolve.Schemas.XPath.Functions.Maps;

using NetEvolve.Schemas.XPath.Api;

/// <summary><c>map:get</c> (Functions and Operators 3.1, 17.1.6): the value of a key, the empty sequence when the map has no such entry.</summary>
internal sealed class MapGetFunction : MapFunction
{
    /// <summary>Initializes a new instance of the <see cref="MapGetFunction"/> class.</summary>
    public MapGetFunction()
        : base("get", OneItem, OneKey) { }

    /// <inheritdoc />
    public override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation)
    {
        _ = RequireMap(invocation.Arguments[0]).TryGet(invocation.Arguments[1][0], out var value);
        invocation.Result = value;
        return XPathEvaluationStatus.Ok;
    }
}
