namespace NetEvolve.Schemas.XPath.Evaluation.FunctionItems;

using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Datatypes.QNames;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Functions.Binding;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// <c>fn:function-lookup</c> (Functions and Operators 3.0, 16.3.1): the function of the static context with a name and an arity, as a function
/// item that keeps the focus of the call; the empty sequence when there is none.
/// </summary>
/// <remarks>
/// The function needs the static context, which a call does not carry, so the lowering of a call or a reference binds a fresh instance with
/// <see cref="Bind"/>; an instance that is not bound looks in the default static context. Limitation: the static context is the one of the
/// expression that contains the call, and the host functions that need a slot are visible exactly when the compilation made the slot visible.
/// </remarks>
internal sealed class FunctionLookupFunction : FunctionItemFunction
{
    private readonly XPathStaticContext _statics;
    private readonly XPathHostRequirements _activeHost;

    /// <summary>Initializes a new instance of the <see cref="FunctionLookupFunction"/> class, looking in the default static context.</summary>
    public FunctionLookupFunction()
        : this(XPathStaticContext.Default, XPathHostRequirements.None) { }

    private FunctionLookupFunction(XPathStaticContext statics, XPathHostRequirements activeHost)
        : base(
            "function-lookup",
            [
                new XPathParameterType(XPathItemType.AnyAtomic, Occurrence.ExactlyOne),
                new(XPathItemType.Integer, Occurrence.ExactlyOne),
            ]
        )
    {
        _statics = statics;
        _activeHost = activeHost;
    }

    /// <summary>Binds a function of the catalog to the static context of one compilation.</summary>
    /// <param name="function">The function.</param>
    /// <param name="statics">The static context.</param>
    /// <param name="activeHost">The host slots and flags that the host provides.</param>
    /// <returns>A bound copy for <c>fn:function-lookup</c>; every other function as it is.</returns>
    public static XPathFunction Bind(
        XPathFunction function,
        XPathStaticContext statics,
        XPathHostRequirements activeHost
    )
    {
        ArgumentNullException.ThrowIfNull(function);
        ArgumentNullException.ThrowIfNull(statics);

        return function is FunctionLookupFunction ? new FunctionLookupFunction(statics, activeHost) : function;
    }

    /// <inheritdoc />
    public override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation)
    {
        if (!invocation.Arguments[0][0].TryGetQName(out var qname))
        {
            throw XPathErrors.Exception(XPathErrorCodes.XPTY0004);
        }

        var arity = invocation.Arguments[1][0].GetSaturatedInteger();
        var name = new XPathQName(qname!.Name.NamespaceName ?? string.Empty, qname.Name.LocalName);
        if (
            arity >= 0
            && arity <= int.MaxValue
            && FunctionCatalog.Default.Registry.TryResolve(_statics, name, (int)arity, _activeHost, out var found)
        )
        {
            var function = Bind(found!, _statics, _activeHost);
            var signature = NamedFunction.SignatureOf(function, (int)arity);
            invocation.Result = XdmSequence.Create(
                XdmItem.FromFunction(new NamedFunction(function, signature, invocation.Context.Focus))
            );
        }

        return XPathEvaluationStatus.Ok;
    }
}
