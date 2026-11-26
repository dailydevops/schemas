namespace NetEvolve.Schemas.XPath.Functions.Sequences;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Evaluation.Arithmetic;
using NetEvolve.Schemas.XPath.Evaluation.Comparison;
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
/// Limitation: the date, time, and duration types are left to #732, so two such values are equal when they have the same type and the same
/// representation, which is stricter than <c>eq</c> (<c>xs:date("2000-01-01Z")</c> and <c>xs:date("2000-01-01+00:00")</c> are different here).
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
        _ = context;
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
        _ = implicitTimezoneMinutes;
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
            XdmItemKind.None or XdmItemKind.Node => Family.None,
            _ => Family.Other,
        };

    private enum Family
    {
        None,
        Numeric,
        Text,
        Boolean,
        Other,
    }

    private sealed class Comparer(XPathEvaluationContext context) : IEqualityComparer<XdmItem>
    {
        public bool Equals(XdmItem x, XdmItem y) => AreEqual(in x, in y, nanEqual: true, context);

        public int GetHashCode(XdmItem obj) => HashCode(in obj, context.ImplicitTimezoneMinutes);
    }
}
