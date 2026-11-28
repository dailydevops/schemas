namespace NetEvolve.Schemas.XPath.Functions.Sequences;

using NetEvolve.Arguments;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Evaluation.Arithmetic;
using NetEvolve.Schemas.XPath.Evaluation.Temporal;
using NetEvolve.Schemas.XPath.Functions.Binding;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The preparation that <c>fn:sum</c>, <c>fn:avg</c>, <c>fn:min</c>, and <c>fn:max</c> share (Functions and Operators 1.0, 15.4): an
/// <c>xs:untypedAtomic</c> value is cast to <c>xs:double</c>, and a value that is neither a number nor a duration of one of the two subtypes
/// cannot be added, which is FORG0006. The durations are added through the entries of the arithmetic operator table
/// (<see cref="TemporalAggregation"/>).
/// </summary>
/// <remarks>Limitation: <c>xs:duration</c> has no addition and is FORG0006 here, as the specification defines the sum of the two subtypes only.</remarks>
internal static class AggregateInput
{
    /// <summary>Casts an <c>xs:untypedAtomic</c> value to <c>xs:double</c>.</summary>
    /// <param name="item">The atomic value.</param>
    /// <returns>The value, cast when it was <c>xs:untypedAtomic</c>.</returns>
    /// <exception cref="XPathException">The lexical form is not an <c>xs:double</c> (FORG0001).</exception>
    public static XdmItem Normalize(in XdmItem item) =>
        item.TryGetUntypedAtomic(out var lexical) ? UntypedAtomicCast.Cast(lexical!, XPathItemType.Double) : item;

    /// <summary>Prepares a value that must be a number.</summary>
    /// <param name="item">The atomic value.</param>
    /// <returns>The number, an <c>xs:untypedAtomic</c> value cast to <c>xs:double</c>.</returns>
    /// <exception cref="XPathException">The lexical form is not an <c>xs:double</c> (FORG0001), or the value is not a number (FORG0006).</exception>
    public static XdmItem Number(in XdmItem item)
    {
        var value = Normalize(in item);
        return IsNumber(in value) ? value : throw XPathErrors.Exception(XPathErrorCodes.FORG0006);
    }

    /// <summary>Adds the numbers or the durations of a sequence in order, with type promotion for the numbers.</summary>
    /// <param name="items">The atomized items, not empty.</param>
    /// <param name="context">The dynamic context.</param>
    /// <returns>The sum, in the promoted type, or a duration of the type of the items.</returns>
    /// <exception cref="System.ArgumentNullException"><paramref name="context"/> is <see langword="null"/>.</exception>
    /// <exception cref="XPathException">An item is not a number (FORG0006) or cannot be cast (FORG0001), the sum overflows (FOAR0002 for a number, FODT0002 for a duration), or the durations are of two subtypes (FORG0006).</exception>
    public static XdmItem Sum(in XdmSequence items, XPathEvaluationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (TemporalAggregation.IsSummable(items[0]))
        {
            return SumDurations(in items, context);
        }

        var total = Number(items[0]);
        var count = items.Count;
        for (var i = 1; i < count; i++)
        {
            total = NumericArithmetic.Add(total, Number(items[i]), context.Limits);
        }

        return total;
    }

    private static XdmItem SumDurations(in XdmSequence items, XPathEvaluationContext context)
    {
        var total = items[0];
        var count = items.Count;
        for (var i = 1; i < count; i++)
        {
            // A year-month and a day-time duration, or a duration and a number, have no sum: the operator table has no entry for the pair.
            if (
                !TemporalAggregation.IsSummable(items[i])
                || !TemporalAggregation.TryApply(ArithmeticOperator.Add, total, items[i], context, out total)
            )
            {
                throw XPathErrors.Exception(XPathErrorCodes.FORG0006);
            }
        }

        return total;
    }

    /// <summary>Divides a duration by the count of the items that were added: the average of durations.</summary>
    /// <param name="total">The sum, a duration.</param>
    /// <param name="count">The number of items.</param>
    /// <param name="context">The dynamic context.</param>
    /// <returns>The average, a duration of the type of the sum.</returns>
    /// <exception cref="System.ArgumentNullException"><paramref name="context"/> is <see langword="null"/>.</exception>
    /// <exception cref="XPathException">The quotient is beyond the range (FODT0002).</exception>
    public static XdmItem DivideDuration(in XdmItem total, int count, XPathEvaluationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return TemporalAggregation.TryApply(
            ArithmeticOperator.Divide,
            total,
            XdmItem.FromInteger(count),
            context,
            out var average
        )
            ? average
            : throw XPathErrors.Exception(XPathErrorCodes.FORG0006);
    }

    /// <summary>Determines whether an item is an <c>xs:integer</c>, <c>xs:decimal</c>, <c>xs:float</c>, or <c>xs:double</c>.</summary>
    /// <param name="item">The item.</param>
    /// <returns><see langword="true"/> for a number.</returns>
    public static bool IsNumber(in XdmItem item) =>
        item.Kind is XdmItemKind.XsInteger or XdmItemKind.XsDecimal or XdmItemKind.XsFloat or XdmItemKind.XsDouble;
}
