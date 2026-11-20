namespace NetEvolve.Schemas.XPath.Functions.QNames;

using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Functions.Binding;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// <c>fn:base-uri($arg as node()?) as xs:anyURI?</c> (Functions and Operators 1.0, 14.5): the base URI of the node, of the context item
/// without an argument. The empty sequence, and a node without a base URI, give the empty sequence.
/// </summary>
/// <remarks>The result is the original string of the base URI of the node, so that the value does not depend on the target framework.</remarks>
internal sealed class BaseUriFunction : QNameFunction
{
    /// <summary>Initializes a new instance of the <see cref="BaseUriFunction"/> class.</summary>
    public BaseUriFunction()
        : base("base-uri", 0, 1, [new XPathParameterType(XPathItemType.Node, Occurrence.ZeroOrOne)]) { }

    /// <inheritdoc />
    /// <exception cref="XPathException">There is no argument and the context item is absent (XPDY0002) or not a node (XPTY0004).</exception>
    public override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation)
    {
        if (TryGetNode(ref invocation, 0, out var node) && node.BaseUri() is { } baseUri)
        {
            invocation.Result = XdmSequence.Create(XdmItem.FromAnyUri(baseUri.OriginalString));
        }

        return XPathEvaluationStatus.Ok;
    }
}
