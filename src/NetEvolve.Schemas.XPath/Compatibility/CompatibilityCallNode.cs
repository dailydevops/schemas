namespace NetEvolve.Schemas.XPath.Compatibility;

using System;
using System.Buffers;
using NetEvolve.Schemas.XPath.Compilation;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Functions.Binding;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The executable form of a function call in XPath 1.0 compatibility mode (XPath 2.0, 3.1.5): each argument is evaluated, converted by
/// <see cref="CompatibilityFunctionConversion"/> against the declared parameter type, and passed to <see cref="XPathFunction.Call"/>, which applies
/// the function conversion rules of the mode without compatibility. The node of the mode without it is a plain call of
/// <see cref="XPathFunction.Call"/>.
/// </summary>
/// <remarks>
/// Limitation: the lowering of a function call is not part of this package: it belongs to the public API (XP-19, #297), which must create this node
/// when <see cref="XPathStaticContext.XPath10Compatibility"/> is on and the resolved function is an <see cref="XPathFunction"/>. The
/// arguments are held in a pooled array, so a call allocates nothing in steady state.
/// </remarks>
internal sealed class CompatibilityCallNode : ExecutableNode
{
    private readonly XPathFunction _function;
    private readonly ExecutableNode[] _arguments;

    /// <summary>Initializes a new instance of the <see cref="CompatibilityCallNode"/> class.</summary>
    /// <param name="function">The function.</param>
    /// <param name="arguments">The argument expressions, one per argument; the node takes ownership of the array, so the caller must not change it afterwards.</param>
    /// <exception cref="ArgumentNullException">An argument or one of the expressions is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The number of arguments lies outside the arity range of the function.</exception>
    public CompatibilityCallNode(XPathFunction function, ExecutableNode[] arguments)
    {
        ArgumentNullException.ThrowIfNull(function);
        ArgumentNullException.ThrowIfNull(arguments);
        ArgumentOutOfRangeException.ThrowIfLessThan(arguments.Length, function.MinArity);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(arguments.Length, function.MaxArity);

        _function = function;
        for (var i = 0; i < arguments.Length; i++)
        {
            ArgumentNullException.ThrowIfNull(arguments[i]);
        }

        _arguments = arguments;
    }

    /// <inheritdoc />
    /// <exception cref="XPathException">An argument does not match its type (XPTY0004) or cannot be cast (FORG0001), or the function reports an error.</exception>
    public override XdmSequence Evaluate(XPathEvaluationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var count = _arguments.Length;
        if (count == 0)
        {
            return _function.Call(context, default);
        }

        // Pooled and cleared, because a pooled array must not keep sequences alive.
        var pool = ArrayPool<XdmSequence>.Shared;
        var values = pool.Rent(count);
        try
        {
            for (var i = 0; i < count; i++)
            {
                values[i] = CompatibilityFunctionConversion.Convert(
                    _arguments[i].Evaluate(context),
                    _function.ParameterTypeAt(i),
                    context.Annotations
                );
            }

            return _function.Call(context, new ReadOnlySpan<XdmSequence>(values, 0, count));
        }
        finally
        {
            pool.Return(values, clearArray: true);
        }
    }
}
