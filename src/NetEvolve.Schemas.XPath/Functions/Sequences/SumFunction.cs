namespace NetEvolve.Schemas.XPath.Functions.Sequences;

using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// <c>fn:sum</c>: the sum of the numbers of the argument with type promotion. <c>xs:untypedAtomic</c> values are cast to
/// <c>xs:double</c>, and a value that is not a number is FORG0006. The empty sequence gives the zero value: the second argument, or
/// the integer 0 without one.
/// </summary>
/// <remarks>Limitation: sums of durations are left to XP-26 (<see cref="AggregateInput"/>).</remarks>
internal sealed class SumFunction : SequenceFunction
{
    /// <summary>Initializes a new instance of the <see cref="SumFunction"/> class.</summary>
    public SumFunction()
        : base("sum", 1, 2, [Atomics, OptionalAtomic]) { }

    /// <inheritdoc />
    public override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation)
    {
        var items = invocation.Arguments[0];
        if (items.IsEmpty)
        {
            invocation.Result =
                invocation.Arguments.Length > 1 ? invocation.Arguments[1] : XdmSequence.Create(XdmItem.FromInteger(0));
            return XPathEvaluationStatus.Ok;
        }

        invocation.Result = XdmSequence.Create(AggregateInput.Sum(in items));
        return XPathEvaluationStatus.Ok;
    }
}
