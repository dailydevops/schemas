namespace NetEvolve.Schemas.XPath.Functions.QNames;

using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Errors;

/// <summary>
/// <c>fn:in-scope-prefixes($element as element()) as xs:string*</c> (Functions and Operators 1.0, 11.2.1): the prefixes of the
/// in-scope namespaces of an element, each once and in no particular order, the default namespace as the zero-length string and the
/// <c>xml</c> prefix always.
/// </summary>
internal sealed class InScopePrefixesFunction : QNameFunction
{
    /// <summary>Initializes a new instance of the <see cref="InScopePrefixesFunction"/> class.</summary>
    public InScopePrefixesFunction()
        : base("in-scope-prefixes", 1, 1, [OneElement]) { }

    /// <inheritdoc />
    /// <exception cref="XPathException">The argument is no element (XPTY0004).</exception>
    public override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation)
    {
        var element = Element(invocation.Arguments[0]);
        invocation.Result = InScopeNamespaces.Prefixes(in element);
        return XPathEvaluationStatus.Ok;
    }
}
