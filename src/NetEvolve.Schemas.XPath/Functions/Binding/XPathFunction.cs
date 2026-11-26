namespace NetEvolve.Schemas.XPath.Functions.Binding;

using System;
using System.Collections.Generic;
using NetEvolve.Arguments;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// A function with a declared signature: the one descriptor of built-in, host, and user-defined functions. A function states its
/// expanded name, arity range, availability flags, and one parameter type per declared parameter; <see cref="Call"/> is the single
/// call path that converts the arguments by the function conversion rules (XPath 2.0, 3.1.5) before <c>Invoke</c> runs.
/// </summary>
/// <remarks>
/// Implementations are stateless or thread-safe. For a variadic function the last declared parameter type applies to every further
/// argument. Limitation: <c>Invoke</c> alone does not convert; every caller goes through <see cref="Call"/>. A function of the
/// public contract (<see cref="XPathHostFunction"/>) declares no parameter types and is wrapped by <see cref="HostFunctionAdapter"/>.
/// </remarks>
internal abstract class XPathFunction : XPathHostFunction
{
    private static readonly XPathParameterType AnyItems = new(XPathItemType.Item, Occurrence.ZeroOrMore);

    private readonly XPathParameterType[] _parameters;

    /// <summary>Initializes a new instance of the <see cref="XPathFunction"/> class.</summary>
    /// <param name="name">The expanded name.</param>
    /// <param name="minArity">The minimum number of arguments.</param>
    /// <param name="maxArity">The maximum number of arguments; <see cref="int.MaxValue"/> for a variadic function.</param>
    /// <param name="parameters">The declared parameter types: one per argument, or one per argument up to the last one that repeats; empty for <c>item()*</c> everywhere. The list is copied.</param>
    /// <exception cref="ArgumentNullException"><paramref name="parameters"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The arity range is invalid, or the number of parameter types lies outside it.</exception>
    protected XPathFunction(XPathQName name, int minArity, int maxArity, IReadOnlyList<XPathParameterType> parameters)
        : base(name, minArity, maxArity)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        if (parameters.Count > 0)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(parameters.Count, minArity);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(parameters.Count, maxArity);
        }

        _parameters = new XPathParameterType[parameters.Count];
        for (var i = 0; i < _parameters.Length; i++)
        {
            _parameters[i] = parameters[i];
        }
    }

    /// <summary>Gets the type expected for an argument.</summary>
    /// <param name="index">The zero-based position of the argument.</param>
    /// <returns>The declared type; the last declared type for a position beyond the declared parameters.</returns>
    public XPathParameterType ParameterTypeAt(int index)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(index, 0);

        return _parameters.Length == 0 ? AnyItems : _parameters[Math.Min(index, _parameters.Length - 1)];
    }

    /// <summary>Converts the arguments to the declared parameter types and invokes the function.</summary>
    /// <param name="context">The evaluation context.</param>
    /// <param name="arguments">The arguments as evaluated, one sequence each.</param>
    /// <returns>The result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The number of arguments lies outside the arity range.</exception>
    /// <exception cref="XPathException">An argument does not match its type (XPTY0004) or cannot be cast (FORG0001), or the function reports an error.</exception>
    public XdmSequence Call(XPathEvaluationContext context, ReadOnlySpan<XdmSequence> arguments)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentOutOfRangeException.ThrowIfLessThan(arguments.Length, MinArity);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(arguments.Length, MaxArity);

        var count = arguments.Length;
        if (count == 0)
        {
            return Run(context, default);
        }

        // Cached, so that a call allocates nothing in steady state; cleared, because a cached array must not keep sequences alive.
        var converted = SmallArrayCache<XdmSequence>.Rent(count);
        try
        {
            for (var i = 0; i < count; i++)
            {
                converted[i] = FunctionConversion.Convert(arguments[i], ParameterTypeAt(i), context.Annotations);
            }

            return Run(context, new ReadOnlySpan<XdmSequence>(converted, 0, count));
        }
        finally
        {
            SmallArrayCache<XdmSequence>.Return(converted, count);
        }
    }

    private XdmSequence Run(XPathEvaluationContext context, ReadOnlySpan<XdmSequence> converted)
    {
        var call = new XPathFunctionCall(context, converted);
        return Invoke(ref call) == XPathEvaluationStatus.Ok ? call.Result : throw new XPathException(call.Diagnostic);
    }
}
