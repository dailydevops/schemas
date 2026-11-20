namespace NetEvolve.Schemas.XPath.Functions.QNames;

using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// <c>fn:namespace-uri-for-prefix($prefix as xs:string?, $element as element()) as xs:anyURI?</c> (Functions and Operators 1.0, 11.1.3):
/// the namespace name that the in-scope namespaces of an element bind to a prefix. The empty sequence and the zero-length string ask
/// for the default namespace; a prefix without a binding, and a default namespace that is not declared, give the empty sequence.
/// </summary>
internal sealed class NamespaceUriForPrefixFunction : QNameFunction
{
    /// <summary>Initializes a new instance of the <see cref="NamespaceUriForPrefixFunction"/> class.</summary>
    public NamespaceUriForPrefixFunction()
        : base("namespace-uri-for-prefix", 2, 2, [OptionalString, OneElement]) { }

    /// <inheritdoc />
    /// <exception cref="XPathException">The element argument is no element (XPTY0004).</exception>
    public override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation)
    {
        var element = Element(invocation.Arguments[1]);
        if (InScopeNamespaces.TryResolve(in element, Text(invocation.Arguments[0]).AsSpan(), out var namespaceName))
        {
            invocation.Result = XdmSequence.Create(XdmItem.FromAnyUri(namespaceName));
        }

        return XPathEvaluationStatus.Ok;
    }
}
