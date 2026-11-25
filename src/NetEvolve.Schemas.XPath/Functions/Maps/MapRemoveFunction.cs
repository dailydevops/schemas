namespace NetEvolve.Schemas.XPath.Functions.Maps;

using NetEvolve.Schemas.XPath.Api;

/// <summary><c>map:remove</c> (Functions and Operators 3.1, 17.1.9): a map without the entries of the keys; keys that the map does not have are ignored.</summary>
internal sealed class MapRemoveFunction : MapFunction
{
    /// <summary>Initializes a new instance of the <see cref="MapRemoveFunction"/> class.</summary>
    public MapRemoveFunction()
        : base("remove", OneItem, Keys) { }

    /// <inheritdoc />
    public override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation)
    {
        var map = RequireMap(invocation.Arguments[0]);
        var keys = invocation.Arguments[1];
        for (var i = 0; i < keys.Count; i++)
        {
            map = map.Remove(keys[i]);
        }

        invocation.Result = map.ToSequence();
        return XPathEvaluationStatus.Ok;
    }
}
