namespace NetEvolve.Schemas.Schematron.Xslt.Library.Core;

using NetEvolve.Arguments;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Functions.Binding;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// <c>function-available()</c> (XSLT 2.0, 18.1.1): whether a function of an expanded name, and with the second argument of an arity, is
/// in scope. An unprefixed name is in the default function namespace. XTDE1400 for a name that is not a lexical QName or whose prefix
/// is not bound. The answer comes from the <see cref="XsltFunctionScope"/> of the binding, so it agrees with the lookup that binds a call.
/// </summary>
internal sealed class FunctionAvailableFunction : XPathFunction
{
    private readonly XsltFunctionScope _scope;

    internal FunctionAvailableFunction(XsltFunctionScope scope)
        : base(
            XsltFunctionScope.Name("function-available"),
            1,
            2,
            [
                new XPathParameterType(XPathItemType.String, Occurrence.ExactlyOne),
                new XPathParameterType(XPathItemType.Integer, Occurrence.ExactlyOne),
            ]
        )
    {
        Argument.ThrowIfNull(scope);

        _scope = scope;
    }

    /// <inheritdoc />
    public override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation)
    {
        _ = invocation.Arguments[0][0].TryGetString(out var lexical);
        if (
            !XsltLexicalName.TryExpand(
                _scope.StaticContext,
                lexical!,
                _scope.StaticContext.DefaultFunctionNamespace,
                out var name
            )
        )
        {
            return XsltLexicalName.Reject(ref invocation, "XTDE1400", lexical!);
        }

        bool available;
        if (invocation.Arguments.Length == 1)
        {
            available = _scope.IsAvailable(name);
        }
        else
        {
            _ = invocation.Arguments[1][0].TryGetInteger(out var arity);
            available = arity is >= 0 and <= int.MaxValue && _scope.IsAvailable(name, (int)arity);
        }

        invocation.Result = XdmSequence.Create(XdmItem.FromBoolean(available));
        return XPathEvaluationStatus.Ok;
    }
}
