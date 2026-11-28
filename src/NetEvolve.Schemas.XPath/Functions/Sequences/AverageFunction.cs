namespace NetEvolve.Schemas.XPath.Functions.Sequences;

using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Evaluation.Arithmetic;
using NetEvolve.Schemas.XPath.Evaluation.Temporal;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// <c>fn:avg</c>: the sum of the numbers of the argument divided by their count, as <c>op:numeric-divide</c> divides, so that the
/// average of integers is a decimal. <c>xs:untypedAtomic</c> values are cast to <c>xs:double</c>, a value that is not a number is
/// FORG0006, and the empty sequence gives the empty sequence. A sequence of <c>xs:yearMonthDuration</c> or of
/// <c>xs:dayTimeDuration</c> values gives the sum divided by the count, as <c>op:divide-yearMonthDuration</c> and
/// <c>op:divide-dayTimeDuration</c> divide.
/// </summary>
/// <remarks>Limitation: <c>xs:duration</c> and the dates and times have no average and are FORG0006 (<see cref="AggregateInput"/>).</remarks>
internal sealed class AverageFunction : SequenceFunction
{
    /// <summary>Initializes a new instance of the <see cref="AverageFunction"/> class.</summary>
    public AverageFunction()
        : base("avg", 1, 1, [Atomics]) { }

    /// <inheritdoc />
    public override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation)
    {
        var items = invocation.Arguments[0];
        if (!items.IsEmpty)
        {
            var total = AggregateInput.Sum(in items, invocation.Context);
            invocation.Result = XdmSequence.Create(
                TemporalAggregation.IsSummable(in total)
                    ? AggregateInput.DivideDuration(in total, items.Count, invocation.Context)
                    : NumericArithmetic.Divide(total, XdmItem.FromInteger(items.Count), invocation.Context.Limits)
            );
        }

        return XPathEvaluationStatus.Ok;
    }
}
