namespace NetEvolve.Schemas.XPath.Functions.Maps;

using NetEvolve.Schemas.XPath.Api;

/// <summary><c>map:put</c> (Functions and Operators 3.1, 17.1.8): a map with an entry added or replaced; the source map is unchanged and shared.</summary>
internal sealed class MapPutFunction : MapFunction
{
    /// <summary>Initializes a new instance of the <see cref="MapPutFunction"/> class.</summary>
    public MapPutFunction()
        : base("put", OneItem, OneKey, AnyItems) { }

    /// <inheritdoc />
    public override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation)
    {
        invocation.Result = RequireMap(invocation.Arguments[0])
            .Put(invocation.Arguments[1][0], invocation.Arguments[2])
            .ToSequence();
        return XPathEvaluationStatus.Ok;
    }
}
