namespace NetEvolve.Schemas.XPath.Functions.Delta3;

using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary><c>fn:has-children</c> (Functions and Operators 3.1, 14.1.4): whether the node has at least one child; false for the empty sequence. Without an argument it tests the context item.</summary>
internal sealed class HasChildrenFunction : Delta3Function
{
    /// <summary>Initializes a new instance of the <see cref="HasChildrenFunction"/> class.</summary>
    public HasChildrenFunction()
        : base("has-children", 0, 1, [OptionalNode]) { }

    /// <inheritdoc />
    public override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation)
    {
        var has = TryGetNode(ref invocation, 0, out var node) && !node.FirstChild().IsEmpty();
        invocation.Result = XdmSequence.Create(XdmItem.FromBoolean(has));
        return XPathEvaluationStatus.Ok;
    }
}
