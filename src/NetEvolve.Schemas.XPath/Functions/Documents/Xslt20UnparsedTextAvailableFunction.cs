namespace NetEvolve.Schemas.XPath.Functions.Documents;

using System;

/// <summary><c>fn:unparsed-text-available</c> for XPath 2.0 under the XSLT 2.0 host flag; the body follows with the implementation.</summary>
internal sealed class Xslt20UnparsedTextAvailableFunction : UnparsedTextAvailableFunction
{
    /// <summary>Initializes a new instance of the <see cref="Xslt20UnparsedTextAvailableFunction"/> class.</summary>
    /// <param name="staticBaseUri">The static base URI, <see langword="null"/> for none.</param>
    public Xslt20UnparsedTextAvailableFunction(Uri? staticBaseUri = null)
        : base(staticBaseUri) { }
}
