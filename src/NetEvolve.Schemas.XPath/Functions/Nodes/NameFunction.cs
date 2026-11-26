namespace NetEvolve.Schemas.XPath.Functions.Nodes;

using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary><c>fn:name</c>: the lexical QName of a node as <c>xs:string</c>, the prefix as it appears in the document.</summary>
internal sealed class NameFunction : NodeNameFunction
{
    /// <summary>Initializes a new instance of the <see cref="NameFunction"/> class.</summary>
    public NameFunction()
        : base("name") { }

    /// <inheritdoc />
    protected override string Part(in XPathNode node) => LexicalNames.Get(node.Tree!.Names, node.Name());

    /// <inheritdoc />
    protected override XdmItem Create(string text) => XdmItem.FromString(text);
}
