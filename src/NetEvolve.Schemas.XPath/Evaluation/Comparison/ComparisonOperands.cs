namespace NetEvolve.Schemas.XPath.Evaluation.Comparison;

using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Functions.Binding;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The operand preparation of the comparisons (XPath 2.0, 3.5.1 and 3.5.2): the single item of a value comparison with its
/// <c>xs:untypedAtomic</c> cast to <c>xs:string</c>, and the casts of a pair of a general comparison.
/// </summary>
/// <remarks>
/// Limitation: XPath 1.0 compatibility mode, which compares by the first item and converts the operands to a number, a string, or a
/// boolean, is not applied (XP-20).
/// </remarks>
internal static class ComparisonOperands
{
    /// <summary>Checks that an atomized operand of a value comparison is one item, and casts it when it is <c>xs:untypedAtomic</c>.</summary>
    /// <param name="atomized">The atomized operand, not empty.</param>
    /// <returns>The item, an <c>xs:string</c> when the operand was <c>xs:untypedAtomic</c>.</returns>
    /// <exception cref="XPathException">The operand has more than one item (XPTY0004).</exception>
    public static XdmItem ValueOperand(in XdmSequence atomized)
    {
        if (atomized.Count != 1)
        {
            throw XPathErrors.Exception(XPathErrorCodes.XPTY0004);
        }

        var item = atomized[0];
        return item.TryGetUntypedAtomic(out var lexical) ? XdmItem.FromString(lexical!) : item;
    }

    /// <summary>Casts the <c>xs:untypedAtomic</c> items of a pair of a general comparison by the rules of XPath 2.0, 3.5.2.</summary>
    /// <param name="left">The left item, replaced when it is <c>xs:untypedAtomic</c>.</param>
    /// <param name="right">The right item, replaced when it is <c>xs:untypedAtomic</c>.</param>
    /// <remarks>
    /// Two <c>xs:untypedAtomic</c> items become strings. One against another type is cast to <c>xs:double</c> for a number, and to the
    /// type of the other item otherwise (<c>xs:string</c> for a string, the primitive type for a type derived from it).
    /// </remarks>
    /// <exception cref="XPathException">The text is not a value of the type it is cast to (FORG0001), or the other item has a type without a cast (XPTY0004).</exception>
    public static void CastUntypedAtomic(ref XdmItem left, ref XdmItem right)
    {
        var leftUntyped = left.TryGetUntypedAtomic(out var leftText);
        var rightUntyped = right.TryGetUntypedAtomic(out var rightText);
        if (leftUntyped && rightUntyped)
        {
            left = XdmItem.FromString(leftText!);
            right = XdmItem.FromString(rightText!);
        }
        else if (leftUntyped)
        {
            left = UntypedAtomicCast.Cast(leftText!, TargetOf(right));
        }
        else if (rightUntyped)
        {
            right = UntypedAtomicCast.Cast(rightText!, TargetOf(left));
        }
    }

    // The kind of an item is its primitive type, and the two duration types keep their own cast (XPath 2.0, 3.5.2).
    private static XPathItemType TargetOf(in XdmItem other) =>
        other.Kind switch
        {
            XdmItemKind.XsInteger or XdmItemKind.XsDecimal or XdmItemKind.XsFloat or XdmItemKind.XsDouble =>
                XPathItemType.Double,
            XdmItemKind.XsString => XPathItemType.String,
            XdmItemKind.Boolean => XPathItemType.Boolean,
            XdmItemKind.AnyUri => XPathItemType.AnyUri,
            XdmItemKind.DateTime => XPathItemType.DateTime,
            XdmItemKind.Date => XPathItemType.Date,
            XdmItemKind.Time => XPathItemType.Time,
            XdmItemKind.Duration => XPathItemType.Duration,
            XdmItemKind.YearMonthDuration => XPathItemType.YearMonthDuration,
            XdmItemKind.DayTimeDuration => XPathItemType.DayTimeDuration,
            _ => throw XPathErrors.Exception(XPathErrorCodes.XPTY0004),
        };
}
