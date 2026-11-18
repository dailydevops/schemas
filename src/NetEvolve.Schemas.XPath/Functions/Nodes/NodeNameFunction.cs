namespace NetEvolve.Schemas.XPath.Functions.Nodes;

using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Functions.Binding;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// A function that reads one part of the name of a node, the node of the argument or, without one, of the context item: the empty
/// sequence and a node without a name (document, text, comment) give the zero-length value.
/// </summary>
internal abstract class NodeNameFunction : NodeFunction
{
    /// <summary>Initializes a new instance of the <see cref="NodeNameFunction"/> class.</summary>
    /// <param name="localName">The local name.</param>
    /// <exception cref="System.ArgumentException"><paramref name="localName"/> is empty.</exception>
    protected NodeNameFunction(string localName)
        : base(localName, 0, 1, [new XPathParameterType(XPathItemType.Node, Occurrence.ZeroOrOne)]) { }

    /// <inheritdoc />
    public sealed override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation)
    {
        var text =
            TryGetNode(ref invocation, 0, out var node)
            && node.Kind is XdmNodeKind.Element or XdmNodeKind.Attribute or XdmNodeKind.ProcessingInstruction
                ? Part(in node)
                : string.Empty;
        invocation.Result = XdmSequence.Create(Create(text));
        return XPathEvaluationStatus.Ok;
    }

    /// <summary>Reads the part of the name of a node that has one.</summary>
    /// <param name="node">An element, an attribute, or a processing instruction.</param>
    /// <returns>The part.</returns>
    protected abstract string Part(in XPathNode node);

    /// <summary>Creates the result item.</summary>
    /// <param name="text">The text.</param>
    /// <returns>The item.</returns>
    protected abstract XdmItem Create(string text);
}
