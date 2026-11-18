namespace NetEvolve.Schemas.XPath.Functions.Nodes;

using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Functions.Binding;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary><c>fn:root</c>: the topmost ancestor of a node, of the context item without an argument, and the empty sequence for the empty sequence.</summary>
internal sealed class RootFunction : NodeFunction
{
    /// <summary>Initializes a new instance of the <see cref="RootFunction"/> class.</summary>
    public RootFunction()
        : base("root", 0, 1, [new XPathParameterType(XPathItemType.Node, Occurrence.ZeroOrOne)]) { }

    /// <inheritdoc />
    public override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation)
    {
        if (TryGetNode(ref invocation, 0, out var node))
        {
            for (var parent = node.Parent(); !parent.IsEmpty(); parent = node.Parent())
            {
                node = parent;
            }

            invocation.Result = XdmSequence.Create(XdmItem.FromNode(in node));
        }

        return XPathEvaluationStatus.Ok;
    }
}
