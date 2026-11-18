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
    protected override string Part(in XPathNode node)
    {
        var names = node.Tree!.Names;
        var name = node.Name();
        var local = names.GetString(name.Local);
        var prefix = names.GetString(name.Prefix);
        return prefix.Length == 0 ? local : prefix + ":" + local;
    }

    /// <inheritdoc />
    protected override XdmItem Create(string text) => XdmItem.FromString(text);
}
