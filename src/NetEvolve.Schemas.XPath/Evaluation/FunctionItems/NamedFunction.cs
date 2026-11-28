namespace NetEvolve.Schemas.XPath.Evaluation.FunctionItems;

using System;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Functions.Binding;
using NetEvolve.Schemas.XPath.Types;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The function item of a named function reference (XPath 3.0, 3.1.6), of <c>fn:function-lookup</c>, and of a static call with a
/// placeholder: a function of the library with a fixed arity. The item keeps the focus that was in force when it was created, so a
/// context-dependent function such as <c>fn:position</c> reads the same focus however late it is called.
/// </summary>
/// <remarks>Limitation: a built-in function declares no result type, so the signature of the item gives <c>item()*</c> as the result (see <see cref="FunctionSignature"/>).</remarks>
internal sealed class NamedFunction : XdmFunction
{
    private readonly XPathFunction _function;
    private readonly XPathFocus _focus;

    /// <summary>Initializes a new instance of the <see cref="NamedFunction"/> class.</summary>
    /// <param name="function">The function.</param>
    /// <param name="signature">The signature, see <see cref="SignatureOf"/>.</param>
    /// <param name="focus">The focus the function runs with.</param>
    /// <exception cref="ArgumentNullException"><paramref name="function"/> or <paramref name="signature"/> is <see langword="null"/>.</exception>
    public NamedFunction(XPathFunction function, FunctionSignature signature, in XPathFocus focus)
        : base(function?.Name, signature)
    {
        ArgumentNullException.ThrowIfNull(function);

        _function = function;
        _focus = focus;
    }

    /// <summary>Describes the signature of a library function of a given arity from its declared parameter types.</summary>
    /// <param name="function">The function.</param>
    /// <param name="arity">The arity, within the range of the function.</param>
    /// <returns>The signature.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="function"/> is <see langword="null"/>.</exception>
    public static FunctionSignature SignatureOf(XPathFunction function, int arity)
    {
        ArgumentNullException.ThrowIfNull(function);

        var parameters = new SequenceTypeInfo[arity];
        for (var i = 0; i < parameters.Length; i++)
        {
            parameters[i] = SequenceTypeInfo.FromParameter(function.ParameterTypeAt(i));
        }

        return new FunctionSignature(parameters, SequenceTypeInfo.AnyItems);
    }

    /// <inheritdoc />
    public override XdmSequence Invoke(XPathEvaluationContext context, ReadOnlySpan<XdmSequence> arguments)
    {
        ArgumentNullException.ThrowIfNull(context);

        var previous = context.Focus;
        context.RestoreFocus(_focus);
        try
        {
            return _function.Call(context, arguments);
        }
        finally
        {
            context.RestoreFocus(previous);
        }
    }
}
