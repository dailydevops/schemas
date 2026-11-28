namespace NetEvolve.Schemas.XPath.Functions.Nodes;

using NetEvolve.Schemas.XPath.Api;

/// <summary>
/// <c>fn:nilled()</c> without an argument (XPath 3.0): the nilled property of the context node. An absent context item is XPDY0002,
/// and a context item that is not a node is XPTY0004.
/// </summary>
internal sealed class NilledContextItemFunction : NilledFunction
{
    /// <summary>Initializes a new instance of the <see cref="NilledContextItemFunction"/> class.</summary>
    public NilledContextItemFunction()
        : base(0, []) { }

    /// <inheritdoc />
    public override XPathVersionMask Versions => XPathVersionMask.V30 | XPathVersionMask.V31;
}
