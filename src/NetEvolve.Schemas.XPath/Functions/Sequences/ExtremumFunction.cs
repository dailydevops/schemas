namespace NetEvolve.Schemas.XPath.Functions.Sequences;

using NetEvolve.Schemas.Xml.Datatypes.Decimals;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Evaluation.Arithmetic;
using NetEvolve.Schemas.XPath.Evaluation.Comparison;
using NetEvolve.Schemas.XPath.Evaluation.Temporal;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The base of <c>fn:min</c> and <c>fn:max</c> (Functions and Operators 1.0, 15.4.3 and 15.4.4): the least or the greatest value of the
/// argument. <c>xs:untypedAtomic</c> values are cast to <c>xs:double</c>; numbers are promoted to the type of the widest number in the
/// sequence and so is the result; a NaN makes the result NaN; strings and URIs compare by codepoint and the result is the selected
/// string with its own type (a derived string type survives), or an <c>xs:string</c> for a URI; booleans compare as booleans; dates,
/// times, dateTimes, and the two duration subtypes compare through the entries of the comparison operator table (<see cref="TemporalAggregation"/>), with the implicit timezone of the dynamic context for a value
/// without a timezone; and values that cannot be ordered together are FORG0006.
/// </summary>
/// <remarks>
/// Limitations: <c>xs:duration</c> and the Gregorian types have no order and are FORG0006, as are values of two types of the temporal
/// family; the codepoint collation is the only collation (<see cref="CollationArgument"/>). Equal values: the first of them is the result.
/// </remarks>
internal abstract class ExtremumFunction : SequenceFunction
{
    private readonly ComparisonRelation _better;

    /// <summary>Initializes a new instance of the <see cref="ExtremumFunction"/> class.</summary>
    /// <param name="localName">The local name.</param>
    /// <param name="better">The relation that a value has to a result that it replaces: less than for the least, greater than for the greatest.</param>
    /// <exception cref="System.ArgumentException"><paramref name="localName"/> is empty.</exception>
    protected ExtremumFunction(string localName, ComparisonRelation better)
        : base(localName, 1, 2, [Atomics, Collation]) => _better = better;

    /// <inheritdoc />
    /// <exception cref="XPathException">The values cannot be ordered together (FORG0006), an <c>xs:untypedAtomic</c> value is not an <c>xs:double</c> (FORG0001), or the collation is not the codepoint collation (FOCH0002).</exception>
    public sealed override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation)
    {
        CollationArgument.Check(invocation.Arguments, 1);

        var items = invocation.Arguments[0];
        if (items.IsEmpty)
        {
            return XPathEvaluationStatus.Ok;
        }

        var family = Classify(in items, out var rank);
        invocation.Result = XdmSequence.Create(
            family switch
            {
                Family.Numeric => PickNumber(in items, rank),
                Family.Text => PickText(in items),
                Family.Temporal => PickTemporal(in items, invocation.Context),
                _ => PickBoolean(in items),
            }
        );
        return XPathEvaluationStatus.Ok;
    }

    // The family of every item must be the one of the first, and the rank is the widest number: integer, decimal, float, or double.
    private static Family Classify(in XdmSequence items, out Rank rank)
    {
        rank = Rank.Integer;
        var family = FamilyOf(items[0]);
        var count = items.Count;
        for (var i = 0; i < count; i++)
        {
            var item = items[i];
            // The temporal types compare with their own type only: a date with a dateTime, or a year-month with a day-time duration, has no operator.
            if (FamilyOf(item) != family || (family == Family.Temporal && item.Kind != items[0].Kind))
            {
                throw XPathErrors.Exception(XPathErrorCodes.FORG0006);
            }

            if (family == Family.Numeric)
            {
                var itemRank = item.Kind switch
                {
                    XdmItemKind.XsInteger => Rank.Integer,
                    XdmItemKind.XsDecimal => Rank.Decimal,
                    XdmItemKind.XsFloat => Rank.Float,
                    _ => Rank.Double,
                };
                rank = rank > itemRank ? rank : itemRank;
            }
        }

        return family;
    }

    private static Family FamilyOf(in XdmItem item) =>
        item.Kind switch
        {
            XdmItemKind.XsInteger
            or XdmItemKind.XsDecimal
            or XdmItemKind.XsFloat
            or XdmItemKind.XsDouble
            or XdmItemKind.UntypedAtomic => Family.Numeric,
            XdmItemKind.XsString or XdmItemKind.AnyUri => Family.Text,
            XdmItemKind.Boolean => Family.Boolean,
            _ when TemporalAggregation.IsOrdered(in item) => Family.Temporal,
            _ => throw XPathErrors.Exception(XPathErrorCodes.FORG0006),
        };

    private XdmItem PickNumber(in XdmSequence items, Rank rank)
    {
        var best = default(XdmItem);
        var seenNaN = false;
        var count = items.Count;
        for (var i = 0; i < count; i++)
        {
            var value = Promote(AggregateInput.Number(items[i]), rank);
            if (IsNaN(in value))
            {
                seenNaN = true;
            }
            else if (best.Kind == XdmItemKind.None || NumericComparison.Compare(_better, value, best))
            {
                best = value;
            }
        }

        if (seenNaN)
        {
            return rank == Rank.Float ? XdmItem.FromFloat(float.NaN) : XdmItem.FromDouble(double.NaN);
        }

        return rank == Rank.Decimal && best.TryGetInteger(out var integer)
            ? XdmItem.FromDecimal(new XsdDecimal(integer, 0))
            : best;
    }

    // Promotion to the type of the widest number; integers and decimals compare exactly and need none.
    private static XdmItem Promote(in XdmItem item, Rank rank)
    {
        if (rank == Rank.Double && !item.TryGetDouble(out _) && NumericPromotion.TryToDouble(item, out var wide))
        {
            return XdmItem.FromDouble(wide);
        }

        if (rank == Rank.Float && !item.TryGetFloat(out _) && NumericPromotion.TryToSingle(item, out var narrow))
        {
            return XdmItem.FromFloat(narrow);
        }

        return item;
    }

    private static bool IsNaN(in XdmItem item) =>
        (item.TryGetDouble(out var wide) && double.IsNaN(wide))
        || (item.TryGetFloat(out var narrow) && float.IsNaN(narrow));

    private XdmItem PickText(in XdmSequence items)
    {
        var best = items[0];
        var count = items.Count;
        for (var i = 1; i < count; i++)
        {
            var item = items[i];
            if (ComparisonRelations.Holds(_better, CodepointComparison.Compare(TextOf(in item), TextOf(in best))))
            {
                best = item;
            }
        }

        // The selected item keeps its own type, a derived string type included; an xs:anyURI is promoted to xs:string.
        return best.Kind == XdmItemKind.XsString ? best : XdmItem.FromString(TextOf(in best));
    }

    private static string TextOf(in XdmItem item) => item.TryGetString(out var value) ? value! : AnyUriOf(in item);

    private static string AnyUriOf(in XdmItem item)
    {
        _ = item.TryGetAnyUri(out var uri);
        return uri!;
    }

    private XdmItem PickTemporal(in XdmSequence items, XPathEvaluationContext context)
    {
        var best = items[0];
        var count = items.Count;
        for (var i = 1; i < count; i++)
        {
            var item = items[i];
            if (TemporalAggregation.TryCompare(_better, item, best, context, out var holds) && holds)
            {
                best = item;
            }
        }

        return best;
    }

    private XdmItem PickBoolean(in XdmSequence items)
    {
        var best = items[0];
        var count = items.Count;
        for (var i = 1; i < count; i++)
        {
            var item = items[i];
            if (BooleanComparison.Compare(_better, item, best))
            {
                best = item;
            }
        }

        return best;
    }

    // The promotion order of XPath 2.0, B.1: the rank of a sequence is the rank of its widest number.
    private enum Rank
    {
        Integer,
        Decimal,
        Float,
        Double,
    }

    private enum Family
    {
        Numeric,
        Text,
        Temporal,
        Boolean,
    }
}
