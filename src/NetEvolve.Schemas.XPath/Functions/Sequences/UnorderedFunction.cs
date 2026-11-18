namespace NetEvolve.Schemas.XPath.Functions.Sequences;

using NetEvolve.Schemas.XPath.Api;

/// <summary><c>fn:unordered</c>: the items of the argument in an implementation-dependent order, which here is the order they have.</summary>
internal sealed class UnorderedFunction : SequenceFunction
{
    /// <summary>Initializes a new instance of the <see cref="UnorderedFunction"/> class.</summary>
    public UnorderedFunction()
        : base("unordered", 1, 1, [Items]) { }

    /// <inheritdoc />
    public override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation)
    {
        invocation.Result = invocation.Arguments[0];
        return XPathEvaluationStatus.Ok;
    }
}
