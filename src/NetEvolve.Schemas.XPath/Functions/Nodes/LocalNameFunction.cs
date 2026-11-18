namespace NetEvolve.Schemas.XPath.Functions.Nodes;

using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary><c>fn:local-name</c>: the local part of the name of a node as <c>xs:string</c>.</summary>
internal sealed class LocalNameFunction : NodeNameFunction
{
    /// <summary>Initializes a new instance of the <see cref="LocalNameFunction"/> class.</summary>
    public LocalNameFunction()
        : base("local-name") { }

    /// <inheritdoc />
    protected override string Part(in XPathNode node) => node.Tree!.Names.GetString(node.Name().Local);

    /// <inheritdoc />
    protected override XdmItem Create(string text) => XdmItem.FromString(text);
}
