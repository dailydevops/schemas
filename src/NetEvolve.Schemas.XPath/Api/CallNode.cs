namespace NetEvolve.Schemas.XPath.Api;

using System;
using System.Buffers;
using System.Collections.Generic;
using NetEvolve.Arguments;
using NetEvolve.Schemas.XPath.Compilation;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Functions.Binding;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The executable form of a function call (XPath 2.0, 3.1.5): each argument is evaluated and passed to <see cref="XPathFunction.Call"/>,
/// which applies the function conversion rules before the function runs. The node holds no evaluation state, so one tree is safe for
/// concurrent evaluation; the arguments are held in a pooled array, so a call allocates nothing in steady state.
/// </summary>
internal sealed class CallNode : ExecutableNode
{
    private readonly XPathFunction _function;
    private readonly ExecutableNode[] _arguments;

    /// <summary>Initializes a new instance of the <see cref="CallNode"/> class.</summary>
    /// <param name="function">The function.</param>
    /// <param name="arguments">The argument expressions, one per argument; the list is copied.</param>
    /// <exception cref="ArgumentNullException">An argument or one of the expressions is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The number of arguments lies outside the arity range of the function.</exception>
    public CallNode(XPathFunction function, ExecutableNode[] arguments)
    {
        ArgumentNullException.ThrowIfNull(function);
        ArgumentNullException.ThrowIfNull(arguments);
        ArgumentOutOfRangeException.ThrowIfLessThan(arguments.Length, function.MinArity);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(arguments.Length, function.MaxArity);

        _function = function;
        _arguments = new ExecutableNode[arguments.Length];
        for (var i = 0; i < arguments.Length; i++)
        {
            ArgumentNullException.ThrowIfNull(arguments[i]);

            _arguments[i] = arguments[i];
        }
    }

    /// <summary>Gets the function.</summary>
    public XPathFunction Function => _function;

    /// <summary>Gets the argument expressions.</summary>
    public IReadOnlyList<ExecutableNode> Arguments => _arguments;

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
                values[i] = _arguments[i].Evaluate(context);
            }

            return _function.Call(context, new ReadOnlySpan<XdmSequence>(values, 0, count));
        }
        finally
        {
            pool.Return(values, clearArray: true);
        }
    }
}
