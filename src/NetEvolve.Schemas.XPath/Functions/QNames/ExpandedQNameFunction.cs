namespace NetEvolve.Schemas.XPath.Functions.QNames;

using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Core;
using NetEvolve.Schemas.Xml.Datatypes.QNames;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Casting.Core;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// <c>fn:QName($paramURI as xs:string?, $paramQName as xs:string) as xs:QName</c> (Functions and Operators 1.0, 11.1.2): the QName with
/// the namespace name of the first argument and the prefix and local name of the lexical QName of the second. The empty sequence and the
/// zero-length string both stand for no namespace, which a prefixed name does not allow.
/// </summary>
/// <remarks>The lexical form is collapsed as the cast to <c>xs:QName</c> does (#270, decision).</remarks>
internal sealed class ExpandedQNameFunction : QNameFunction
{
    /// <summary>Initializes a new instance of the <see cref="ExpandedQNameFunction"/> class.</summary>
    public ExpandedQNameFunction()
        : base("QName", 2, 2, [OptionalString, OneString]) { }

    /// <inheritdoc />
    /// <exception cref="XPathException">The lexical form is no QName, or it has a prefix and the namespace name is empty (FOCA0002).</exception>
    public override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation)
    {
        var namespaceName = Text(invocation.Arguments[0]);
        var lexical = WhitespaceText.Apply(Text(invocation.Arguments[1]), WhitespaceMode.Collapse);
        if (
            XsdQName.Scan(lexical.AsSpan(), out var colon, out _) != DatatypeError.None
            || (colon >= 0 && namespaceName.Length == 0)
        )
        {
            throw XPathErrors.Exception(XPathErrorCodes.FOCA0002);
        }

        var name = new XsdQName(namespaceName.Length == 0 ? null : namespaceName, lexical.Substring(colon + 1));
        invocation.Result = XdmSequence.Create(
            XdmItem.FromQName(new XdmQName(name, colon < 0 ? null : lexical.Substring(0, colon)))
        );
        return XPathEvaluationStatus.Ok;
    }
}
