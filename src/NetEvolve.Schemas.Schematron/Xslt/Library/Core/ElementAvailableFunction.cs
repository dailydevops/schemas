namespace NetEvolve.Schemas.Schematron.Xslt.Library.Core;

using System;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Functions.Binding;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// <c>element-available()</c> (XSLT 2.0, 18.1.1): whether an XSLT instruction of an expanded name is supported. An unprefixed name is
/// in the default element namespace of the static context, or in no namespace. XTDE1440 for a name that is not a lexical QName or
/// whose prefix is not bound. The answer comes from the instruction registry, so an instruction that is known but deferred is not available.
/// </summary>
internal sealed class ElementAvailableFunction : XPathFunction
{
    private readonly XPathStaticContext _statics;
    private readonly Func<IXsltInstructionRegistry> _registry;

    internal ElementAvailableFunction(XPathStaticContext statics, Func<IXsltInstructionRegistry> registry)
        : base(
            XsltFunctionScope.Name("element-available"),
            1,
            1,
            [new XPathParameterType(XPathItemType.String, Occurrence.ExactlyOne)]
        )
    {
        ArgumentNullException.ThrowIfNull(statics);
        ArgumentNullException.ThrowIfNull(registry);

        _statics = statics;
        _registry = registry;
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
            return XsltLexicalName.Reject(ref invocation, "XTDE1440", lexical!);
        }

        invocation.Result = XdmSequence.Create(XdmItem.FromBoolean(_registry().IsAvailable(in name)));
        return XPathEvaluationStatus.Ok;
    }
}
