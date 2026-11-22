namespace NetEvolve.Schemas.XPath.Functions.Documents;

using System;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// <c>fn:doc-available($uri as xs:string?) as xs:boolean</c> (Functions and Operators 1.0, 15.5.5): whether <c>fn:doc</c> with the same
/// argument returns a document node, which it does for a resource that loads and is pooled for the later <c>fn:doc</c>. The empty
/// sequence gives <see langword="false"/>, and so does every condition for which <c>fn:doc</c> raises FODC0002.
/// </summary>
/// <remarks>
/// Errors: FODC0005 for a reference that is no URI reference or has a fragment identifier, as for <c>fn:doc</c>; the QT3 cases of
/// the specification erratum FO.E26 accept either that error or <see langword="false"/>, and the one error keeps both functions in step.
/// </remarks>
internal sealed class DocAvailableFunction : DocumentFunction
{
    /// <summary>Initializes a new instance of the <see cref="DocAvailableFunction"/> class.</summary>
    /// <param name="staticBaseUri">The static base URI, <see langword="null"/> for none.</param>
    /// <param name="options">The reader options, <see langword="null"/> for the secure defaults.</param>
    public DocAvailableFunction(Uri? staticBaseUri = null, XmlReaderOptions? options = null)
        : base("doc-available", 1, 1, [OptionalString], staticBaseUri, options) { }

    /// <inheritdoc />
    /// <exception cref="XPathException">The reference is no URI (FODC0005).</exception>
    public override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation)
    {
        var argument = invocation.Arguments[0];
        var status = argument.IsEmpty ? DocumentStatus.Unavailable : TryLoad(invocation.Context, Text(argument), out _);
        if (status == DocumentStatus.InvalidUri)
        {
            throw XPathErrors.Exception(XPathErrorCodes.FODC0005);
        }

        invocation.Result = XdmSequence.Create(XdmItem.FromBoolean(status == DocumentStatus.Loaded));
        return XPathEvaluationStatus.Ok;
    }
}
