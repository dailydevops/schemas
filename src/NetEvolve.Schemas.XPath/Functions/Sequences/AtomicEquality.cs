namespace NetEvolve.Schemas.XPath.Functions.Sequences;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Evaluation.Arithmetic;
using NetEvolve.Schemas.XPath.Evaluation.Comparison;
using NetEvolve.Schemas.XPath.Evaluation.Temporal;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The equality of two atomic values as <c>fn:distinct-values</c>, <c>fn:index-of</c>, and <c>fn:deep-equal</c> use it (Functions and
/// Operators 1.0, 15.1.6, 15.1.3, and 15.3.1): the <c>eq</c> operator, except that two values whose types cannot be compared are
/// different and not an error. Numbers compare by value after type promotion, <c>xs:string</c>, <c>xs:anyURI</c>, and
/// <c>xs:untypedAtomic</c> compare as strings by codepoint, and booleans compare as booleans. Whether NaN equals NaN is the caller's
/// choice: it does in <c>fn:distinct-values</c> and <c>fn:deep-equal</c>, and does not in <c>fn:index-of</c>.
/// </summary>
/// <remarks>
/// <para>
/// Dates, times, dateTimes, and the Gregorian types compare through <see cref="TemporalAggregation.TryCompare"/> with the implicit timezone
/// of the dynamic context, so a value without a timezone is read in that timezone, and a date is different from a dateTime without an
/// error. The three duration types compare by their months and seconds. The hash of a date or time value is the hash of its position on the
/// timeline after the implicit timezone is applied, so it agrees with <c>eq</c> across timezones; the hash of a duration is the hash of its
/// months and seconds. Without a context, a date or time value is equal to another only by its representation.
/// </para>
/// <para>
/// Limitation: the hash of a number is the hash of the <c>xs:float</c> it rounds to, so that values that promotion makes equal collide;
/// a decimal that lies on a rounding boundary of <c>xs:float</c> and <c>xs:double</c> can in theory be equal to a float without the
/// same hash, and is then kept as a distinct value.
/// </para>
/// </remarks>
internal static class AtomicEquality
{
    private const int NaNHash = 0x7FF8;

    /// <summary>Gets the comparer of <see cref="AreEqual"/> with NaN equal to NaN, the one <c>fn:distinct-values</c> uses.</summary>
    /// <param name="context">The dynamic context, which gives the implicit timezone.</param>
    /// <returns>The comparer.</returns>
    public static IEqualityComparer<XdmItem> CreateDistinctComparer(XPathEvaluationContext context) =>
        new Comparer(context);

    /// <summary>Determines whether two atomic values are equal.</summary>
    /// <param name="left">The first value.</param>
    /// <param name="right">The second value.</param>
    /// <param name="nanEqual">Whether NaN is equal to NaN.</param>
    /// <param name="context">The dynamic context, which gives the implicit timezone.</param>
    /// <returns><see langword="true"/> when the values are equal; <see langword="false"/> for values that are different or that cannot be compared, and for nodes.</returns>
    public static bool AreEqual(
        in XdmItem left,
        in XdmItem right,
        bool nanEqual,
        XPathEvaluationContext? context = null
    )
    {
        var family = FamilyOf(left);
        if (family != FamilyOf(right))
        {
            return false;
        }

        switch (family)
        {
            case Family.Numeric:
                var leftNaN = IsNaN(in left);
                var rightNaN = IsNaN(in right);
                return leftNaN || rightNaN
                    ? nanEqual && leftNaN && rightNaN
                    : NumericComparison.Compare(ComparisonRelation.Equal, left, right);
            case Family.Text:
                return string.Equals(TextOf(in left), TextOf(in right), StringComparison.Ordinal);
            case Family.Boolean:
                _ = left.TryGetBoolean(out var a);
                _ = right.TryGetBoolean(out var b);
                return a == b;
            case Family.Duration:
                _ = left.TryGetDuration(out var x);
                _ = right.TryGetDuration(out var y);
                return x.Equals(y);
            case Family.Temporal:
                // Without a context there is no implicit timezone to read a value in, so the representation decides.
                return context is null
                    ? left.Equals(right)
                    : TemporalAggregation.TryCompare(
                        ComparisonRelation.Equal,
                        in left,
                        in right,
                        context,
                        out var holds
                    ) && holds;
            case Family.Other:
                return left.Equals(right);
            default:
                return false;
        }
    }

    /// <summary>Gets a hash code that is the same for every value that <see cref="AreEqual"/> finds equal, NaN included.</summary>
    /// <param name="item">The value.</param>
    /// <param name="implicitTimezoneMinutes">The implicit timezone in minutes from UTC.</param>
    /// <returns>The hash code.</returns>
    public static int HashCode(in XdmItem item, int implicitTimezoneMinutes = 0)
    {
        switch (FamilyOf(item))
        {
            case Family.Numeric:
                _ = NumericPromotion.TryToDouble(item, out var number);
                if (double.IsNaN(number))
                {
                    return NaNHash;
                }

                if (number == 0)
                {
                    return 0;
                }

                var narrow = (float)number;
                return float.IsInfinity(narrow) ? number.GetHashCode() : ((double)narrow).GetHashCode();
            case Family.Text:
                return StringComparer.Ordinal.GetHashCode(TextOf(in item));
            case Family.Boolean:
                _ = item.TryGetBoolean(out var truth);
                return truth ? 1 : 2;
            case Family.Duration:
                _ = item.TryGetDuration(out var duration);
                return duration.GetHashCode();
            case Family.Temporal:
                _ = item.TryGetTemporal(out var moment);
                return TemporalValues.Instant(in moment, implicitTimezoneMinutes).GetHashCode();
            default:
                return item.GetHashCode();
        }
    }

    private static bool IsNaN(in XdmItem item) =>
        (item.TryGetDouble(out var wide) && double.IsNaN(wide))
        || (item.TryGetFloat(out var narrow) && float.IsNaN(narrow));

    private static string TextOf(in XdmItem item) =>
        item.TryGetString(out var text) ? text!
        : item.TryGetUntypedAtomic(out var untyped) ? untyped!
        : item.TryGetAnyUri(out var uri) ? uri!
        : string.Empty;

    private static Family FamilyOf(in XdmItem item) =>
        item.Kind switch
        {
            XdmItemKind.XsInteger or XdmItemKind.XsDecimal or XdmItemKind.XsFloat or XdmItemKind.XsDouble =>
                Family.Numeric,
            XdmItemKind.XsString or XdmItemKind.UntypedAtomic or XdmItemKind.AnyUri => Family.Text,
            XdmItemKind.Boolean => Family.Boolean,
            XdmItemKind.Duration or XdmItemKind.YearMonthDuration or XdmItemKind.DayTimeDuration => Family.Duration,
            >= XdmItemKind.DateTime and <= XdmItemKind.GMonth => Family.Temporal,
            XdmItemKind.None or XdmItemKind.Node => Family.None,
            _ => Family.Other,
        };

    private enum Family
    {
        None,
        Numeric,
        Text,
        Boolean,
        Duration,
        Temporal,
        Other,
    }

    private sealed class Comparer(XPathEvaluationContext context) : IEqualityComparer<XdmItem>
    {
        public bool Equals(XdmItem x, XdmItem y) => AreEqual(in x, in y, nanEqual: true, context);

        public int GetHashCode(XdmItem obj) => HashCode(in obj, context.ImplicitTimezoneMinutes);
    }
}
