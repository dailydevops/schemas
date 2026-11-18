namespace NetEvolve.Schemas.XPath.Functions.Sequences;

using System.Collections.Generic;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Evaluation.Paths;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// <c>fn:distinct-values</c> (Functions and Operators 1.0, 15.1.6): the values of the argument without the repeats, in the order
/// of their first occurrence, which keeps the value and the type of the first of a group of equal values. Equality is
/// <see cref="AtomicEquality"/> with NaN equal to NaN.
/// </summary>
/// <remarks>The values go through a hash set, so the cost is linear in the number of values; the set is the one allocation of a call that has two or more values.</remarks>
internal sealed class DistinctValuesFunction : SequenceFunction
{
    /// <summary>Initializes a new instance of the <see cref="DistinctValuesFunction"/> class.</summary>
    public DistinctValuesFunction()
        : base("distinct-values", 1, 2, [Atomics, Collation]) { }

    /// <inheritdoc />
    /// <exception cref="XPathException">The collation is not the codepoint collation (FOCH0002).</exception>
    public override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation)
    {
        CollationArgument.Check(invocation.Arguments, 1);

        var items = invocation.Arguments[0];
        var count = items.Count;
        if (count <= 1)
        {
            invocation.Result = items;
            return XPathEvaluationStatus.Ok;
        }

        var seen = new HashSet<XdmItem>(AtomicEquality.DistinctComparer);
        var kept = default(XdmItemList);
        try
        {
            for (var i = 0; i < count; i++)
            {
                var item = items[i];
                if (seen.Add(item))
                {
                    kept.Add(item);
                }
            }

            invocation.Result = kept.ToSequence();
        }
        finally
        {
            kept.Dispose();
        }

        return XPathEvaluationStatus.Ok;
    }
}
