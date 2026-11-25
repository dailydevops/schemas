namespace NetEvolve.Schemas.XPath.Functions.Maps;

using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary><c>map:keys</c> (Functions and Operators 3.1, 17.1.3): the keys of a map, in no specified order.</summary>
internal sealed class MapKeysFunction : MapFunction
{
    /// <summary>Initializes a new instance of the <see cref="MapKeysFunction"/> class.</summary>
    public MapKeysFunction()
        : base("keys", OneItem) { }

    /// <inheritdoc />
    public override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation)
    {
        var entries = RequireMap(invocation.Arguments[0]).Entries();
        var keys = new XdmItem[entries.Length];
        for (var i = 0; i < keys.Length; i++)
        {
            keys[i] = entries[i].Key;
        }

        invocation.Result = XdmSequence.Create(keys);
        return XPathEvaluationStatus.Ok;
    }
}
