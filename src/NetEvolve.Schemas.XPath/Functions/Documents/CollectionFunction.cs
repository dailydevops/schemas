namespace NetEvolve.Schemas.XPath.Functions.Documents;

using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Functions.Nodes;

/// <summary><c>fn:collection</c>; the body follows with the implementation.</summary>
internal sealed class CollectionFunction : NodeFunction
{
    /// <summary>Initializes a new instance of the <see cref="CollectionFunction"/> class.</summary>
    public CollectionFunction()
        : base("collection", 0, 1, []) { }

    /// <inheritdoc />
    public override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation) => XPathEvaluationStatus.Ok;
}
