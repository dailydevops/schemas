namespace NetEvolve.Schemas.XPath.Functions.Nodes;

using System.Globalization;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Functions.Binding;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// <c>generate-id</c> (XSLT 2.0, 16.6; Functions and Operators 3.0, 14.6): the one implementation for the XSLT 2.0 host and for
/// XPath 3.0 and later. The id is made of the key of the tree and the index of the node, so it is the same for the same node in
/// every evaluation, differs for every node of every tree, and consists of ASCII letters and digits that start with a letter, a valid NCName.
/// This class is the XPath 3.0 and 3.1 registration; <see cref="Xslt20GenerateIdFunction"/> adds the XSLT 2.0 host.
/// </summary>
/// <remarks>Limitation: the id says nothing about the position of the node; two ids are compared for equality only.</remarks>
internal class GenerateIdFunction : NodeFunction
{
    /// <summary>Initializes a new instance of the <see cref="GenerateIdFunction"/> class.</summary>
    public GenerateIdFunction()
        : base("generate-id", 0, 1, [new XPathParameterType(XPathItemType.Node, Occurrence.ZeroOrOne)]) { }

    /// <inheritdoc />
    public override XPathVersionMask Versions => XPathVersionMask.V30 | XPathVersionMask.V31;

    /// <inheritdoc />
    public sealed override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation)
    {
        invocation.Result = XdmSequence.Create(
            XdmItem.FromString(TryGetNode(ref invocation, 0, out var node) ? IdOf(in node) : string.Empty)
        );
        return XPathEvaluationStatus.Ok;
    }

    private static string IdOf(in XPathNode node) =>
        string.Concat(
            "d",
            node.Tree!.TreeId.ToString(CultureInfo.InvariantCulture),
            "n",
            node.Node.Index.ToString(CultureInfo.InvariantCulture)
        );
}
