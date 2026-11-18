namespace NetEvolve.Schemas.XPath.Functions.Nodes;

using NetEvolve.Schemas.XPath.Api;

/// <summary><c>generate-id</c> for XPath 2.0 under the XSLT 2.0 host flag: the same function, visible in a 2.0 static context only when the host sets <see cref="XPathHostRequirements.Xslt20Host"/>.</summary>
internal sealed class Xslt20GenerateIdFunction : GenerateIdFunction
{
    /// <inheritdoc />
    public override XPathVersionMask Versions => XPathVersionMask.V20;

    /// <inheritdoc />
    public override XPathHostRequirements HostRequirements => XPathHostRequirements.Xslt20Host;
}
