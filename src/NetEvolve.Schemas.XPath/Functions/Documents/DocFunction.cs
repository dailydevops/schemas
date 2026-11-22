namespace NetEvolve.Schemas.XPath.Functions.Documents;

using System;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// <c>fn:doc($uri as xs:string?) as document-node()?</c> (Functions and Operators 1.0, 15.5.4): the document that the reference names,
/// loaded through the resolver of the evaluation context and pooled, so the same absolute URI gives the identical node. The empty
/// sequence gives the empty sequence.
/// </summary>
/// <remarks>
/// <para>
/// Errors: FODC0005 for a reference that is no URI reference or has a fragment identifier; FODC0002 when the resource cannot be
/// retrieved: the context has no resolver, the resolver denies or does not know the resource, the reference is relative and the
/// function holds no absolute static base URI, the resource exceeds the size limit, or it is not well formed. The default resolver
/// denies every URI, so nothing is accessed.
/// </para>
/// <para>
/// Limitations: the document is loaded with the reader options the function holds, which are the secure defaults for the one in the
/// default catalog, so a document type declaration needs an explicit option (<see cref="DocumentFunction"/>). A failed load is
/// pooled, so the error is stable. Fragment identifiers are refused and not resolved against the document.
/// </para>
/// </remarks>
internal sealed class DocFunction : DocumentFunction
{
    /// <summary>Initializes a new instance of the <see cref="DocFunction"/> class.</summary>
    /// <param name="staticBaseUri">The static base URI, <see langword="null"/> for none.</param>
    /// <param name="options">The reader options, <see langword="null"/> for the secure defaults.</param>
    public DocFunction(Uri? staticBaseUri = null, XmlReaderOptions? options = null)
        : base("doc", 1, 1, [OptionalString], staticBaseUri, options) { }

    /// <inheritdoc />
    /// <exception cref="XPathException">The reference is no URI (FODC0005) or the resource cannot be retrieved (FODC0002).</exception>
    public override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation)
    {
        var argument = invocation.Arguments[0];
        if (argument.IsEmpty)
        {
            return XPathEvaluationStatus.Ok;
        }

        var status = TryLoad(invocation.Context, Text(argument), out var tree);
        if (status != DocumentStatus.Loaded)
        {
            throw XPathErrors.Exception(
                status == DocumentStatus.InvalidUri ? XPathErrorCodes.FODC0005 : XPathErrorCodes.FODC0002
            );
        }

        invocation.Result = XdmSequence.Create(XdmItem.FromNode(new XPathNode(tree!.Document)));
        return XPathEvaluationStatus.Ok;
    }
}
