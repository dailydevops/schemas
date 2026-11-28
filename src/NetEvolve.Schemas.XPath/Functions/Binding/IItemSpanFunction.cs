namespace NetEvolve.Schemas.XPath.Functions.Binding;

using System;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>A built-in function that folds over its single argument as a span of items, so that a path result needs no sequence array.</summary>
/// <remarks>The items are not atomized and hold no function item: the function atomizes what it needs, and the caller falls back to the generic call otherwise.</remarks>
internal interface IItemSpanFunction
{
    /// <summary>Invokes the one-argument form over the items of the argument.</summary>
    /// <param name="context">The evaluation context.</param>
    /// <param name="items">The items of the argument; valid for the call only.</param>
    /// <returns>The result, as the generic call gives it.</returns>
    XdmSequence InvokeOver(XPathEvaluationContext context, ReadOnlySpan<XdmItem> items);
}
