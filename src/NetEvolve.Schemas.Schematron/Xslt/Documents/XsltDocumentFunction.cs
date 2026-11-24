namespace NetEvolve.Schemas.Schematron.Xslt.Documents;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Functions.Binding;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// <c>document($uri-sequence as item()*, $base-node as node()) as node()*</c> (XSLT 2.0, 16.1): the document of every reference, loaded
/// by <c>fn:doc</c> of the XPath package, so the load goes through the resolver of the evaluation context and the document pool that
/// <c>fn:doc</c> shares, and the same absolute URI is the same node. A relative reference is resolved against the base URI of
/// <c>$base-node</c> when that argument is given, otherwise against the base URI of the node it came from, otherwise against the schema
/// base URI that the function holds, never against the instance.
/// </summary>
/// <remarks>
/// <para>
/// Errors: XTRE1160 for every reference that cannot be turned into a document: it is no URI reference or has a fragment identifier, it is
/// relative against no base, the context has no resolver, or the resolver denies it, does not know it, or serves a resource that exceeds
/// the size limit or is not well formed. The default resolver denies every URI, so nothing is accessed. The request limit of the core
/// (<c>MaxExternalResources</c>) is enforced by the shared pool and surfaces as the same error.
/// </para>
/// <para>
/// Limitations: XSLT 2.0 makes XTRE1160 recoverable (the reference then yields nothing); this function raises it, as the work package
/// requires a coded error. A fragment identifier is refused and not resolved against the document. The documents are loaded with the
/// secure reader defaults, so a document type declaration is refused. The order of the result is the order of first occurrence, not
/// document order across documents.
/// </para>
/// </remarks>
internal sealed class XsltDocumentFunction : XPathFunction
{
    private static readonly XPathErrorCode Xtre1160 = new("XTRE1160");

    private readonly XPathFunction _doc = SchemaBase.Catalog("doc", 1);
    private readonly string? _schemaBase;

    internal XsltDocumentFunction(Uri? schemaBaseUri)
        : base(
            new XPathQName(XPathStaticContext.Default.DefaultFunctionNamespace, "document"),
            1,
            2,
            [
                new XPathParameterType(XPathItemType.Item, Occurrence.ZeroOrMore),
                new XPathParameterType(XPathItemType.Node, Occurrence.ExactlyOne),
            ]
        ) => _schemaBase = SchemaBase.Text(schemaBaseUri);

    /// <inheritdoc />
    /// <exception cref="XPathException">A reference cannot be turned into a document (XTRE1160).</exception>
    public override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation)
    {
        var arguments = invocation.Arguments;
        var uris = arguments[0];
        var hasBase = arguments.Length > 1;
        string? explicitBase = null;
        if (hasBase)
        {
            _ = arguments[1][0].TryGetNode(out var baseNode);
            explicitBase = SchemaBase.Text(baseNode.BaseUri());
        }

        var found = new List<XPathNode>(uris.Count);
        for (var i = 0; i < uris.Count; i++)
        {
            string reference;
            var baseUri = hasBase ? explicitBase : _schemaBase;
            if (uris[i].TryGetNode(out var source))
            {
                reference = source.StringValue();
                baseUri = hasBase ? explicitBase : SchemaBase.Text(source.BaseUri());
            }
            else if (uris[i].TryGetString(out var text))
            {
                reference = text!;
            }
            else
            {
                throw Error(
                    new XPathErrorCode("XPTY0004"),
                    XPathErrorKind.Type,
                    "A reference of document() is a node or a string."
                );
            }

            Add(invocation.Context, SchemaBase.Resolve(reference, baseUri), found);
        }

        var items = new XdmItem[found.Count];
        for (var i = 0; i < items.Length; i++)
        {
            items[i] = XdmItem.FromNode(found[i]);
        }

        invocation.Result = XdmSequence.Create(items);
        return XPathEvaluationStatus.Ok;
    }

    private static XPathException Error(XPathErrorCode code, XPathErrorKind kind, string message) =>
        new(new XPathDiagnostic(code, kind, message, -1, 0));

    private void Add(XPathEvaluationContext context, string uri, List<XPathNode> found)
    {
        XdmSequence loaded;
        try
        {
            loaded = _doc.Call(context, [XdmSequence.Create(XdmItem.FromString(uri))]);
        }
        catch (XPathException)
        {
            throw Error(Xtre1160, XPathErrorKind.Dynamic, "The document '" + uri + "' cannot be retrieved.");
        }

        _ = loaded[0].TryGetNode(out var node);
        for (var i = 0; i < found.Count; i++)
        {
            if (found[i].IsSameNode(in node))
            {
                return;
            }
        }

        found.Add(node);
    }
}
