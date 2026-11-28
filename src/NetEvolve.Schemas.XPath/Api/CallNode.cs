namespace NetEvolve.Schemas.XPath.Api;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.XPath.Compilation;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Evaluation.Paths;
using NetEvolve.Schemas.XPath.Functions.Binding;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
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

    // Set for a built-in span function over a single path argument: the path delivers into a pooled list that the function folds over.
    private readonly IItemListProducer? _producer;
    private readonly IItemSpanFunction? _spanFunction;

    /// <summary>Initializes a new instance of the <see cref="CallNode"/> class.</summary>
    /// <param name="function">The function.</param>
    /// <param name="arguments">The argument expressions, one per argument; the node takes ownership of the array, so the caller must not change it afterwards.</param>
    /// <exception cref="ArgumentNullException">An argument or one of the expressions is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The number of arguments lies outside the arity range of the function.</exception>
    public CallNode(XPathFunction function, ExecutableNode[] arguments)
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
        if (arguments.Length == 1 && arguments[0] is IItemListProducer producer && function is IItemSpanFunction span)
        {
            _producer = producer;
            _spanFunction = span;
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

        if (_producer is not null && ReferenceEquals(context.Annotations, UntypedAnnotationSource.Instance))
        {
            return EvaluateOverList(context);
        }

        var count = _arguments.Length;
        if (count == 0)
        {
            return _function.Call(context, default);
        }

        // Cached and cleared, because a cached array must not keep sequences alive.
        var values = SmallArrayCache<XdmSequence>.Rent(count);
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
            SmallArrayCache<XdmSequence>.Return(values, count);
        }
    }

    // The same result as the generic call without the item array of the path result, for nodes with the untyped default. A function item
    // (an array expands when atomized) needs the conversion rules, so the generic call runs for it. The list is bounded as the producer's own
    // evaluation bounds it: a set operation collects without a bound.
    private XdmSequence EvaluateOverList(XPathEvaluationContext context)
    {
        var list = _producer is PathNode path
            ? new XdmItemList(context.Limits.MaxSequenceSize, path.Location)
            : default;
        try
        {
            _producer!.EvaluateInto(context, ref list);
            var items = list.Items;
            if (!HasFunctionItem(items))
            {
                return _spanFunction!.InvokeOver(context, items);
            }

            var values = SmallArrayCache<XdmSequence>.Rent(1);
            try
            {
                values[0] = list.ToSequence();
                return _function.Call(context, new ReadOnlySpan<XdmSequence>(values, 0, 1));
            }
            finally
            {
                SmallArrayCache<XdmSequence>.Return(values, 1);
            }
        }
        finally
        {
            list.Dispose();
        }
    }

    private static bool HasFunctionItem(ReadOnlySpan<XdmItem> items)
    {
        foreach (var item in items)
        {
            if (item.Kind == XdmItemKind.Function)
            {
                return true;
            }
        }

        return false;
    }
}
