namespace NetEvolve.Schemas.XPath.Functions.Sequences;

using System;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Functions.Binding;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary><c>fn:exists</c>: whether the argument has an item. A lazy argument that nobody has read is asked for one item and no more.</summary>
internal sealed class ExistsFunction : SequenceFunction, IItemSpanFunction
{
    /// <summary>Initializes a new instance of the <see cref="ExistsFunction"/> class.</summary>
    public ExistsFunction()
        : base("exists", 1, 1, [Items]) { }

    /// <inheritdoc />
    public override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation)
    {
        invocation.Result = XdmSequence.Create(XdmItem.FromBoolean(invocation.Arguments[0].HasItems()));
        return XPathEvaluationStatus.Ok;
    }

    /// <inheritdoc />
    public XdmSequence InvokeOver(XPathEvaluationContext context, ReadOnlySpan<XdmItem> items) =>
        XdmSequence.Create(XdmItem.FromBoolean(items.Length != 0));
}
