namespace NetEvolve.Schemas.XPath.Functions.Nodes;

using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Functions.Binding;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// <c>fn:document-uri($arg as node()?) as xs:anyURI?</c> (Functions and Operators 1.0, 2.3): the document URI of a document node that
/// has one; the empty sequence for the empty sequence, for a document node without a URI, and for the other kinds of node.
/// </summary>
internal class DocumentUriFunction : NodeFunction
{
    /// <summary>Initializes a new instance of the <see cref="DocumentUriFunction"/> class.</summary>
    public DocumentUriFunction()
        : this(1, [new XPathParameterType(XPathItemType.Node, Occurrence.ZeroOrOne)]) { }

    /// <summary>Initializes a new instance of the <see cref="DocumentUriFunction"/> class with one fixed arity.</summary>
    /// <param name="arity">The number of arguments.</param>
    /// <param name="parameters">The declared parameter types.</param>
    private protected DocumentUriFunction(int arity, XPathParameterType[] parameters)
        : base("document-uri", arity, arity, parameters) { }

    /// <inheritdoc />
    public override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation)
    {
        if (
            TryGetNode(ref invocation, 0, out var node)
            && node.Kind == XdmNodeKind.Document
            && node.Node.DocumentUri is { } uri
        )
        {
            invocation.Result = XdmSequence.Create(XdmItem.FromAnyUri(uri.OriginalString));
        }

        return XPathEvaluationStatus.Ok;
    }
}
