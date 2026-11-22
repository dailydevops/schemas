namespace NetEvolve.Schemas.XPath.Functions.Documents;

using System;
using NetEvolve.Schemas.XPath.Api;

/// <summary>
/// <c>fn:unparsed-text</c> for XPath 2.0 under the XSLT 2.0 host flag (XSLT 2.0, 16.2): the same function, visible in a 2.0 static
/// context only when the host sets <see cref="XPathHostRequirements.Xslt20Host"/>.
/// </summary>
internal sealed class Xslt20UnparsedTextFunction : UnparsedTextFunction
{
    /// <summary>Initializes a new instance of the <see cref="Xslt20UnparsedTextFunction"/> class.</summary>
    /// <param name="staticBaseUri">The static base URI, <see langword="null"/> for none.</param>
    public Xslt20UnparsedTextFunction(Uri? staticBaseUri = null)
        : base(staticBaseUri) { }

    /// <inheritdoc />
    public override XPathVersionMask Versions => XPathVersionMask.V20;

    /// <inheritdoc />
    public override XPathHostRequirements HostRequirements => XPathHostRequirements.Xslt20Host;
}
