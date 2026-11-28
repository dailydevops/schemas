namespace NetEvolve.Schemas.Schematron.Xslt.Library.Core;

using System;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Functions.Binding;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Types;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// <c>type-available()</c> (XSLT 3.0, 20.1.2): whether a type name is a built-in type of the XML Schema namespace. An unprefixed name
/// is in the default element namespace of the static context, or in no namespace, as the type names of an XPath expression are.
/// <c>xs:dateTimeStamp</c> is a type of XSD 1.1 and so available only where the static context is XPath 3.1, which agrees with the
/// binder of the item types. XTDE1428 for a name that is not a lexical QName or whose prefix is not bound.
/// </summary>
internal sealed class TypeAvailableFunction : XPathFunction
{
    private readonly XPathStaticContext _statics;

    internal TypeAvailableFunction(XPathStaticContext statics)
        : base(
            XsltFunctionScope.Name("type-available"),
            1,
            1,
            [new XPathParameterType(XPathItemType.String, Occurrence.ExactlyOne)]
        )
    {
        ArgumentNullException.ThrowIfNull(statics);

        _statics = statics;
    }

    /// <inheritdoc />
    public override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation)
    {
        _ = invocation.Arguments[0][0].TryGetString(out var lexical);
        if (
            !XsltLexicalName.TryExpand(
                _statics,
                lexical!,
                _statics.DefaultElementNamespace ?? string.Empty,
                out var name
            )
        )
        {
            return XsltLexicalName.Reject(ref invocation, "XTDE1428", lexical!);
        }

        var available =
            XdmTypes.TryGet(name.NamespaceName, name.LocalName, out _)
            && (
                _statics.Version == XPathVersion.V31
                || !string.Equals(name.LocalName, "dateTimeStamp", StringComparison.Ordinal)
            );
        invocation.Result = XdmSequence.Create(XdmItem.FromBoolean(available));
        return XPathEvaluationStatus.Ok;
    }
}
