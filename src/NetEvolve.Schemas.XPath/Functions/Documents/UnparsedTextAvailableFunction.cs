namespace NetEvolve.Schemas.XPath.Functions.Documents;

using System;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// <c>fn:unparsed-text-available($href as xs:string?[, $encoding as xs:string]) as xs:boolean</c> (Functions and Operators 3.1, 14.9.2;
/// XSLT 2.0, 16.2): whether <c>fn:unparsed-text</c> with the same arguments returns a string, which it does for a resource that loads
/// and is pooled for the later call. It never raises an error: the empty sequence and every condition for which <c>fn:unparsed-text</c>
/// raises an error give <see langword="false"/>. It is available in XPath 3.0 and 3.1; <see cref="Xslt20UnparsedTextAvailableFunction"/>
/// adds XPath 2.0 under the XSLT 2.0 host flag.
/// </summary>
internal class UnparsedTextAvailableFunction : TextResourceFunction
{
    /// <summary>Initializes a new instance of the <see cref="UnparsedTextAvailableFunction"/> class.</summary>
    /// <param name="staticBaseUri">The static base URI, <see langword="null"/> for none.</param>
    public UnparsedTextAvailableFunction(Uri? staticBaseUri = null)
        : base("unparsed-text-available", staticBaseUri) { }

    /// <inheritdoc />
    public override XPathVersionMask Versions => XPathVersionMask.V30 | XPathVersionMask.V31;

    /// <inheritdoc />
    public sealed override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation)
    {
        var available = !invocation.Arguments[0].IsEmpty && TryLoadText(ref invocation, out _, out _);
        invocation.Result = XdmSequence.Create(XdmItem.FromBoolean(available));
        return XPathEvaluationStatus.Ok;
    }
}
