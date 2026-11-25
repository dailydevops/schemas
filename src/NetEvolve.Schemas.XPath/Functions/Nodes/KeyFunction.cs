namespace NetEvolve.Schemas.XPath.Functions.Nodes;

using NetEvolve.Schemas.XPath.Abstractions;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Casting.Core;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Formatting.Numbers;
using NetEvolve.Schemas.XPath.Functions.Binding;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// <c>key($name as xs:string, $value as xs:anyAtomicType*[, $top as node()]) as node()*</c> (XSLT 2.0, 16.3): the nodes of the key of the
/// name whose key value is one of the values, in the document of the context node, or, with the third argument, in the subtree of
/// <c>$top</c>. The function is visible only in a 2.0 static context under <see cref="XPathHostRequirements.Xslt20Host"/>.
/// </summary>
/// <remarks>
/// <para>
/// The key index lives in the provider of the slot <see cref="XPathHostSlots.Keys"/>, which is an
/// <see cref="IXPathKeyProvider"/>: the function hands it the context node, or the top node with the subtree flag (#777). An
/// empty slot is XPDY0002. A name that is not a valid QName, or that no declaration has, is
/// XTDE1260; the provider reports a tree without a document node as XTDE1270 through an <see cref="XPathException"/>.
/// </para>
/// <para>
/// Limitations: a prefixed name is resolved by no namespace context until a call reaches the static namespaces (#752), as for
/// <c>format-number</c>. <c>key</c> as the start of a rule context or of an <c>xsl:key</c> match pattern is not supported (#780).
/// </para>
/// </remarks>
internal sealed class KeyFunction : NodeFunction
{
    private const string NoKey = "XTDE1260";

    /// <summary>Initializes a new instance of the <see cref="KeyFunction"/> class.</summary>
    public KeyFunction()
        : base(
            "key",
            2,
            3,
            [
                new XPathParameterType(XPathItemType.String, Occurrence.ExactlyOne),
                new XPathParameterType(XPathItemType.AnyAtomic, Occurrence.ZeroOrMore),
                new XPathParameterType(XPathItemType.Node, Occurrence.ExactlyOne),
            ]
        ) { }

    /// <summary>Gets the language versions in which the function is visible: XPath 2.0, as the XSLT 2.0 host.</summary>
    public override XPathVersionMask Versions => XPathVersionMask.V20;

    /// <summary>Gets the host flag: the function is visible only when the host sets <see cref="XPathHostRequirements.Xslt20Host"/>.</summary>
    public override XPathHostRequirements HostRequirements => XPathHostRequirements.Xslt20Host;

    /// <inheritdoc />
    /// <exception cref="XPathException">
    /// The slot is empty (XPDY0002), the name is invalid or unknown (XTDE1260), the
    /// context item is absent (XPDY0002) or no node (XPTY0004) in the two-argument form, or the provider fails (XTDE1270).
    /// </exception>
    public override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation)
    {
        var provider = invocation.Context.Host.Keys ?? throw XPathErrors.Exception(XPathErrorCodes.XPDY0002);
        var arguments = invocation.Arguments;
        _ = arguments[0][0].TryGetString(out var text);
        if (!DecimalFormatName.TryParse(text!, true, NoNamespaceContext.Instance, out var name))
        {
            throw KeyError();
        }

        _ = TryGetNode(ref invocation, 2, out var node);
        if (!provider.TryLookup(in name, in arguments[1], in node, arguments.Length > 2, out var nodes))
        {
            throw KeyError();
        }

        invocation.Result = nodes;
        return XPathEvaluationStatus.Ok;
    }

    private static XPathException KeyError() =>
        new(
            new XPathDiagnostic(
                new XPathErrorCode(NoKey),
                XPathErrorKind.Dynamic,
                "The key name is not a valid name or names no key.",
                -1,
                0
            )
        );
}
