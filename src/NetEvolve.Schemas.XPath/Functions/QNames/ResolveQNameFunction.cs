namespace NetEvolve.Schemas.XPath.Functions.QNames;

using System;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Core;
using NetEvolve.Schemas.Xml.Datatypes.QNames;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Casting.Core;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// <c>fn:resolve-QName($qname as xs:string?, $element as element()) as xs:QName?</c> (Functions and Operators 1.0, 11.1.1): the QName
/// that a lexical QName denotes in the in-scope namespaces of an element, with the prefix it was written with. An unprefixed name takes
/// the default namespace of the element when there is one; the empty sequence gives the empty sequence.
/// </summary>
/// <remarks>The lexical form is collapsed as the cast to <c>xs:QName</c> does (#270, decision).</remarks>
internal sealed class ResolveQNameFunction : QNameFunction
{
    /// <summary>Initializes a new instance of the <see cref="ResolveQNameFunction"/> class.</summary>
    public ResolveQNameFunction()
        : base("resolve-QName", 2, 2, [OptionalString, OneElement]) { }

    /// <inheritdoc />
    /// <exception cref="XPathException">The element argument is no element (XPTY0004), the lexical form is no QName (FOCA0002), or its prefix is not bound (FONS0004).</exception>
    public override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation)
    {
        var element = Element(invocation.Arguments[1]);
        if (invocation.Arguments[0].IsEmpty)
        {
            return XPathEvaluationStatus.Ok;
        }

        var lexical = WhitespaceText.Apply(Text(invocation.Arguments[0]), WhitespaceMode.Collapse);
        var parsed = XsdQName.Parse(lexical.AsSpan(), new ElementNamespaces(element));
        if (!parsed.Success)
        {
            throw XPathErrors.Exception(
                parsed.Error == DatatypeError.UnboundPrefix ? XPathErrorCodes.FONS0004 : XPathErrorCodes.FOCA0002
            );
        }

        var colon = lexical.AsSpan().IndexOf(':');
        invocation.Result = XdmSequence.Create(
            XdmItem.FromQName(new XdmQName(parsed.Value, colon < 0 ? null : lexical.Substring(0, colon)))
        );
        return XPathEvaluationStatus.Ok;
    }

    private sealed class ElementNamespaces : INamespaceContext
    {
        private readonly XPathNode _element;

        public ElementNamespaces(XPathNode element) => _element = element;

        public bool TryResolvePrefix(ReadOnlySpan<char> prefix, out string? namespaceName)
        {
            var found = InScopeNamespaces.TryResolve(in _element, prefix, out var resolved);
            namespaceName = found ? resolved : null;
            return found;
        }
    }
}
