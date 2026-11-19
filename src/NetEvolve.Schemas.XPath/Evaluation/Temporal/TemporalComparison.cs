namespace NetEvolve.Schemas.XPath.Evaluation.Temporal;

using System;
using NetEvolve.Arguments;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Evaluation.Comparison;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The comparison of dates, times, and durations (Functions and Operators 1.0, 10.4): two moments of one type are ordered on the
/// timeline, with the implicit timezone of the dynamic context for a value without a timezone, and two durations are equal when their
/// months and their seconds are equal and, for the two subtypes, ordered by the one part they have.
/// </summary>
/// <remarks>
/// <para>
/// The methods are the implementations of the operator mapping rows that <see cref="TemporalOperators"/> registers in the comparison
/// table: the six relations for <c>xs:dateTime</c>, <c>xs:date</c>, <c>xs:time</c>, <c>xs:yearMonthDuration</c>, and
/// <c>xs:dayTimeDuration</c>, and <c>eq</c> and <c>ne</c> for <c>xs:duration</c> and the five Gregorian types. A pair that has no row,
/// such as <c>lt</c> on <c>xs:duration</c>, never reaches them.
/// </para>
/// <para>
/// The implicit timezone replaces the plus or minus 14 hour window of the order in XML Schema (#197): a value with a timezone and one
/// without are ordered, never incomparable. Two values without a timezone are read in the same timezone, so their order does not depend on it.
/// </para>
/// </remarks>
internal static class TemporalComparison
{
    /// <summary>Compares two moments of one type by a relation.</summary>
    /// <param name="relation">The relation.</param>
    /// <param name="left">The left operand: a date, time, dateTime, or Gregorian value.</param>
    /// <param name="right">The right operand, of the type of <paramref name="left"/>.</param>
    /// <param name="context">The dynamic context, which gives the implicit timezone.</param>
    /// <returns>Whether the relation holds.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="relation"/> is not a defined relation.</exception>
    /// <exception cref="XPathException">An operand is no date or time value (XPTY0004); the comparison table maps these types only, so this is a registration error.</exception>
    public static bool CompareMoments(
        ComparisonRelation relation,
        in XdmItem left,
        in XdmItem right,
        XPathEvaluationContext context
    )
    {
        Argument.ThrowIfNull(context);
        ComparisonRelations.Check(relation);

        if (!left.TryGetTemporal(out var a) || !right.TryGetTemporal(out var b))
        {
            throw XPathErrors.Exception(XPathErrorCodes.XPTY0004);
        }

        var timezone = context.ImplicitTimezoneMinutes;
        return ComparisonRelations.Holds(
            relation,
            TemporalValues.Instant(in a, timezone).CompareTo(TemporalValues.Instant(in b, timezone))
        );
    }

    /// <summary>Compares two durations by a relation.</summary>
    /// <param name="relation">The relation.</param>
    /// <param name="left">The left operand: a duration.</param>
    /// <param name="right">The right operand: a duration.</param>
    /// <returns>Whether the relation holds.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="relation"/> is not a defined relation.</exception>
    /// <exception cref="XPathException">An operand is no duration (XPTY0004); the comparison table maps these types only, so this is a registration error.</exception>
    /// <remarks>
    /// Equality compares the months and the seconds. An ordering is registered for the two subtypes only, where one part is always zero,
    /// so the order of the months and then of the seconds is the total order of that subtype; the partial order of <c>xs:duration</c>
    /// has no operator in XPath 2.0 (#197).
    /// </remarks>
    public static bool CompareDurations(ComparisonRelation relation, in XdmItem left, in XdmItem right)
    {
        ComparisonRelations.Check(relation);

        if (!left.TryGetDuration(out var a) || !right.TryGetDuration(out var b))
        {
            throw XPathErrors.Exception(XPathErrorCodes.XPTY0004);
        }

        var order = a.Months.CompareTo(b.Months);
        if (order == 0)
        {
            order = a.Seconds.CompareTo(b.Seconds);
        }

        return ComparisonRelations.Holds(relation, order);
    }
}
