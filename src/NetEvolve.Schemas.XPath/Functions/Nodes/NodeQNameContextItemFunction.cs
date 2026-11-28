namespace NetEvolve.Schemas.XPath.Functions.Nodes;

using NetEvolve.Schemas.XPath.Api;

/// <summary>
/// <c>fn:node-name()</c> without an argument (XPath 3.0): the name of the context node. An absent context item is XPDY0002, and a
/// context item that is not a node is XPTY0004.
/// </summary>
internal sealed class NodeQNameContextItemFunction : NodeQNameFunction
{
    /// <summary>Initializes a new instance of the <see cref="NodeQNameContextItemFunction"/> class.</summary>
    public NodeQNameContextItemFunction()
        : base(0, []) { }

    /// <inheritdoc />
    public override XPathVersionMask Versions => XPathVersionMask.V30 | XPathVersionMask.V31;
}
