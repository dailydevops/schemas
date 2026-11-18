namespace NetEvolve.Schemas.XPath.Functions.Sequences;

using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Evaluation.Arithmetic;
using NetEvolve.Schemas.XPath.Functions.Binding;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The preparation that <c>fn:sum</c>, <c>fn:avg</c>, <c>fn:min</c>, and <c>fn:max</c> share (Functions and Operators 1.0, 15.4): an
/// <c>xs:untypedAtomic</c> value is cast to <c>xs:double</c>, and a value that is not a number cannot be added, which is FORG0006.
/// </summary>
/// <remarks>Limitation: the date, time, and duration types are left to XP-26, so a value of one of them is FORG0006 here, where the specification adds and orders durations and orders dates and times.</remarks>
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

    /// <summary>Adds the numbers of a sequence in order, with type promotion.</summary>
    /// <param name="items">The atomized items, not empty.</param>
    /// <returns>The sum, in the promoted type.</returns>
    /// <exception cref="XPathException">An item is not a number (FORG0006) or cannot be cast (FORG0001), or the sum overflows (FOAR0002).</exception>
    public static XdmItem Sum(in XdmSequence items)
    {
        var total = Number(items[0]);
        var count = items.Count;
        for (var i = 1; i < count; i++)
        {
            total = NumericArithmetic.Add(total, Number(items[i]));
        }

        return total;
    }

    /// <summary>Determines whether an item is an <c>xs:integer</c>, <c>xs:decimal</c>, <c>xs:float</c>, or <c>xs:double</c>.</summary>
    /// <param name="item">The item.</param>
    /// <returns><see langword="true"/> for a number.</returns>
    public static bool IsNumber(in XdmItem item) =>
        item.Kind is XdmItemKind.XsInteger or XdmItemKind.XsDecimal or XdmItemKind.XsFloat or XdmItemKind.XsDouble;
}
