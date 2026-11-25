namespace NetEvolve.Schemas.XPath.Functions.Maps;

using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Xdm.Maps;

/// <summary><c>map:entry</c> (Functions and Operators 3.1, 17.1.7): a map of one entry.</summary>
internal sealed class MapEntryFunction : MapFunction
{
    /// <summary>Initializes a new instance of the <see cref="MapEntryFunction"/> class.</summary>
    public MapEntryFunction()
        : base("entry", OneKey, AnyItems) { }

    /// <inheritdoc />
    public override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation)
    {
        invocation.Result = XdmMap.Empty.Put(invocation.Arguments[0][0], invocation.Arguments[1]).ToSequence();
        return XPathEvaluationStatus.Ok;
    }
}
