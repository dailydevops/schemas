namespace NetEvolve.Schemas.XPath.Functions.Nodes;

using NetEvolve.Schemas.Xml.Datatypes.QNames;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Functions.Binding;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// <c>fn:node-name($arg as node()?) as xs:QName?</c> (Functions and Operators 1.0, 2.1): the expanded name of an element, an attribute,
/// or a processing instruction; the empty sequence for the empty sequence and for the other kinds of node.
/// </summary>
internal sealed class NodeQNameFunction : NodeFunction
{
    /// <summary>Initializes a new instance of the <see cref="NodeQNameFunction"/> class.</summary>
    public NodeQNameFunction()
        : base("node-name", 1, 1, [new XPathParameterType(XPathItemType.Node, Occurrence.ZeroOrOne)]) { }

    /// <inheritdoc />
    public override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation)
    {
        if (
            TryGetNode(ref invocation, 0, out var node)
            && node.Kind is XdmNodeKind.Element or XdmNodeKind.Attribute or XdmNodeKind.ProcessingInstruction
        )
        {
            var names = node.Tree!.Names;
            var name = node.Name();
            var ns = names.GetString(name.NamespaceUri);
            var prefix = names.GetString(name.Prefix);
            var qname = new XsdQName(ns.Length == 0 ? null : ns, names.GetString(name.Local));
            invocation.Result = XdmSequence.Create(
                XdmItem.FromQName(new XdmQName(qname, prefix.Length == 0 ? null : prefix))
            );
        }

        return XPathEvaluationStatus.Ok;
    }
}
