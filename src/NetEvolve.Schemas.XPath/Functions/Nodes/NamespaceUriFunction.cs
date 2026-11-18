namespace NetEvolve.Schemas.XPath.Functions.Nodes;

using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary><c>fn:namespace-uri</c>: the namespace name of a node as <c>xs:anyURI</c>, zero-length for a name in no namespace.</summary>
internal sealed class NamespaceUriFunction : NodeNameFunction
{
    /// <summary>Initializes a new instance of the <see cref="NamespaceUriFunction"/> class.</summary>
    public NamespaceUriFunction()
        : base("namespace-uri") { }

    /// <inheritdoc />
    protected override string Part(in XPathNode node) => node.Tree!.Names.GetString(node.Name().NamespaceUri);

    /// <inheritdoc />
    protected override XdmItem Create(string text) => XdmItem.FromAnyUri(text);
}
