namespace NetEvolve.Schemas.XPath.Functions.Binding;

using System;
using System.Buffers;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Evaluation.Arithmetic;
using NetEvolve.Schemas.XPath.Evaluation.Atomization;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The function conversion rules (XPath 2.0, 3.1.5, with the type promotion of B.1): the argument is atomized, each
/// <c>xs:untypedAtomic</c> item is cast to the expected atomic type, each numeric item is promoted, each <c>xs:anyURI</c> item is
/// promoted to <c>xs:string</c>, and the result must match the expected type, or XPTY0004 is raised.
/// </summary>
/// <remarks>
/// Limitations: nodes atomize with the untyped default, so annotated nodes await schema-aware processing (DEF-40, #52); the
/// derivation check covers the built-in atomic types a function signature names (<c>xs:integer</c> is a <c>xs:decimal</c>, the two
/// duration types are <c>xs:duration</c>), and the shared matcher of XP-21 (#227) replaces it; XPath 1.0 compatibility mode is not
/// applied (XP-20). A singleton argument allocates nothing in the binding layer unless an <c>xs:untypedAtomic</c> value is cast, which allocates in the
/// float and double parsers of the Xml package (#591), or the cast target is a decimal, date and time, or duration value, which is boxed.
/// </remarks>
internal static class FunctionConversion
{
    /// <summary>Converts one argument to the expected type.</summary>
    /// <param name="argument">The argument as evaluated.</param>
    /// <param name="type">The expected type.</param>
    /// <returns>The converted argument; the argument itself when nothing changes.</returns>
    /// <exception cref="XPathException">The argument does not match the type (XPTY0004), or an <c>xs:untypedAtomic</c> value cannot be cast (FORG0001, FOCA0003).</exception>
    public static XdmSequence Convert(in XdmSequence argument, XPathParameterType type)
    {
        var count = argument.Count;
        if (!MatchesOccurrence(count, type.Occurrence))
        {
            throw XPathErrors.Exception(XPathErrorCodes.XPTY0004);
        }

        if (count == 0)
        {
            return argument;
        }

        switch (type.ItemType)
        {
            case XPathItemType.Item:
                return argument;
            case XPathItemType.Node:
                for (var i = 0; i < count; i++)
                {
                    if (!argument[i].IsNode)
                    {
                        throw XPathErrors.Exception(XPathErrorCodes.XPTY0004);
                    }
                }

                return argument;
            default:
                return ConvertAtomic(XPathAtomization.Atomize(argument), count, type.ItemType);
        }
    }

    private static bool MatchesOccurrence(int count, Occurrence occurrence) =>
        occurrence switch
        {
            Occurrence.ExactlyOne => count == 1,
            Occurrence.ZeroOrOne => count <= 1,
            Occurrence.OneOrMore => count >= 1,
            _ => true,
        };

    private static XdmSequence ConvertAtomic(in XdmSequence atomic, int count, XPathItemType target)
    {
        if (count == 1)
        {
            var only = atomic[0];
            var converted = ConvertItem(only, target);
            return converted.Kind == only.Kind ? atomic : XdmSequence.Create(converted);
        }

        // Copy on the first change: a sequence that needs no conversion is returned as it is.
        var pool = ArrayPool<XdmItem>.Shared;
        XdmItem[]? items = null;
        try
        {
            for (var i = 0; i < count; i++)
            {
                var item = atomic[i];
                var converted = ConvertItem(item, target);
                if (items is null)
                {
                    if (converted.Kind == item.Kind)
                    {
                        continue;
                    }

                    items = pool.Rent(count);
                    for (var j = 0; j < i; j++)
                    {
                        items[j] = atomic[j];
                    }
                }

                items[i] = converted;
            }

            if (items is null)
            {
                return atomic;
            }

            return XdmSequence.Create(new ReadOnlySpan<XdmItem>(items, 0, count));
        }
        finally
        {
            if (items is not null)
            {
                pool.Return(items, clearArray: true);
            }
        }
    }

    private static XdmItem ConvertItem(in XdmItem item, XPathItemType target)
    {
        if (target == XPathItemType.AnyAtomic)
        {
            return item;
        }

        if (item.Kind == XdmItemKind.UntypedAtomic)
        {
            if (target == XPathItemType.UntypedAtomic)
            {
                return item;
            }

            _ = item.TryGetUntypedAtomic(out var lexical);
            return UntypedAtomicCast.Cast(lexical!, target == XPathItemType.Numeric ? XPathItemType.Double : target);
        }

        if (Matches(target, item.Kind))
        {
            return item;
        }

        return TryPromote(item, target, out var promoted)
            ? promoted
            : throw XPathErrors.Exception(XPathErrorCodes.XPTY0004);
    }

    // Subtype substitution: the item keeps its own type when that type is the expected one or derives from it.
    private static bool Matches(XPathItemType target, XdmItemKind kind) =>
        target switch
        {
            XPathItemType.Numeric => kind
                is XdmItemKind.XsInteger
                    or XdmItemKind.XsDecimal
                    or XdmItemKind.XsFloat
                    or XdmItemKind.XsDouble,
            XPathItemType.Decimal => kind is XdmItemKind.XsDecimal or XdmItemKind.XsInteger,
            XPathItemType.Integer => kind == XdmItemKind.XsInteger,
            XPathItemType.Float => kind == XdmItemKind.XsFloat,
            XPathItemType.Double => kind == XdmItemKind.XsDouble,
            XPathItemType.String => kind == XdmItemKind.XsString,
            XPathItemType.Boolean => kind == XdmItemKind.Boolean,
            XPathItemType.UntypedAtomic => kind == XdmItemKind.UntypedAtomic,
            XPathItemType.AnyUri => kind == XdmItemKind.AnyUri,
            XPathItemType.DateTime => kind == XdmItemKind.DateTime,
            XPathItemType.Date => kind == XdmItemKind.Date,
            XPathItemType.Time => kind == XdmItemKind.Time,
            XPathItemType.Duration => kind
                is XdmItemKind.Duration
                    or XdmItemKind.YearMonthDuration
                    or XdmItemKind.DayTimeDuration,
            XPathItemType.YearMonthDuration => kind == XdmItemKind.YearMonthDuration,
            XPathItemType.DayTimeDuration => kind == XdmItemKind.DayTimeDuration,
            _ => false,
        };

    // Numeric promotion (B.1): float to double, decimal to float or double, with the integers as decimals; URI promotion: anyURI to string.
    private static bool TryPromote(in XdmItem item, XPathItemType target, out XdmItem promoted)
    {
        promoted = default;
        switch (target)
        {
            case XPathItemType.Double:
                if (NumericPromotion.TryToDouble(item, out var number))
                {
                    promoted = XdmItem.FromDouble(number);
                }

                break;
            case XPathItemType.Float:
                if (NumericPromotion.TryToSingle(item, out var single))
                {
                    promoted = XdmItem.FromFloat(single);
                }

                break;
            case XPathItemType.String:
                if (item.TryGetAnyUri(out var uri))
                {
                    promoted = XdmItem.FromString(uri!);
                }

                break;
            default:
                break;
        }

        return promoted.Kind != XdmItemKind.None;
    }
}
