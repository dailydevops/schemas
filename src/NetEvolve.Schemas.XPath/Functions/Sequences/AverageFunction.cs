namespace NetEvolve.Schemas.XPath.Functions.Sequences;

using System;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Evaluation.Arithmetic;
using NetEvolve.Schemas.XPath.Evaluation.Temporal;
using NetEvolve.Schemas.XPath.Functions.Binding;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// <c>fn:avg</c>: the sum of the numbers of the argument divided by their count, as <c>op:numeric-divide</c> divides, so that the
/// average of integers is a decimal. <c>xs:untypedAtomic</c> values are cast to <c>xs:double</c>, a value that is not a number is
/// FORG0006, and the empty sequence gives the empty sequence. A sequence of <c>xs:yearMonthDuration</c> or of
/// <c>xs:dayTimeDuration</c> values gives the sum divided by the count, as <c>op:divide-yearMonthDuration</c> and
/// <c>op:divide-dayTimeDuration</c> divide.
/// </summary>
/// <remarks>Limitation: <c>xs:duration</c> and the dates and times have no average and are FORG0006 (<see cref="AggregateInput"/>).</remarks>
internal sealed class AverageFunction : SequenceFunction, IItemSpanFunction
{
    /// <summary>Initializes a new instance of the <see cref="AverageFunction"/> class.</summary>
    public AverageFunction()
        : base("avg", 1, 1, [Atomics]) { }

    /// <inheritdoc />
    public override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation)
    {
        invocation.Result = InvokeOver(this, in invocation.Arguments[0], invocation.Context);
        return XPathEvaluationStatus.Ok;
    }

    /// <inheritdoc />
    public XdmSequence InvokeOver(XPathEvaluationContext context, ReadOnlySpan<XdmItem> items) =>
        Average(items, context);

    private static XdmSequence Average(ReadOnlySpan<XdmItem> items, XPathEvaluationContext context)
    {
        if (items.IsEmpty)
        {
            return default;
        }

        var total = AggregateInput.Sum(items, context);
        return XdmSequence.Create(
            TemporalAggregation.IsSummable(in total)
                ? AggregateInput.DivideDuration(in total, items.Length, context)
                : NumericArithmetic.Divide(total, XdmItem.FromInteger(items.Length), context.Limits)
        );
    }
}
