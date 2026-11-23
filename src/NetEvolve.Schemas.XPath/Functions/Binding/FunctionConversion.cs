namespace NetEvolve.Schemas.XPath.Functions.Binding;

using System;
using System.Buffers;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Evaluation.Arithmetic;
using NetEvolve.Schemas.XPath.Evaluation.Atomization;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Types;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The function conversion rules (XPath 2.0, 3.1.5, with the type promotion of B.1): the argument is atomized, each
/// <c>xs:untypedAtomic</c> item is cast to the expected atomic type through the casting table (<see cref="UntypedAtomicCast"/>), each numeric item is promoted, each <c>xs:anyURI</c> item is
/// promoted to <c>xs:string</c>, and the result must match the expected type, or XPTY0004 is raised. A kind test or <c>empty-sequence()</c>
/// is matched by the shared sequence type matcher (<see cref="SequenceTypeMatcher"/>) without atomization.
/// </summary>
/// <remarks>
/// Limitations: nodes atomize with the untyped default, so annotated nodes await schema-aware processing (DEF-40, #52); the
/// derivation check is the one of the built-in type table (<see cref="XdmTypes.IsDerivedFrom"/>), so an item of a derived type keeps its
/// type for every base type, and an atomic type outside the item types of <see cref="XPathItemType"/> is not promoted to; XPath 1.0
/// compatibility mode is not
/// applied here: <see cref="Compatibility.CompatibilityFunctionConversion"/> runs in front of these rules. A singleton argument allocates nothing in the binding layer unless an <c>xs:untypedAtomic</c> value is cast to a decimal, date and time, or
/// duration value, which is boxed (#661).
/// </remarks>
internal static class FunctionConversion
{
    /// <summary>Converts one argument to the expected type.</summary>
    /// <param name="argument">The argument as evaluated.</param>
    /// <param name="type">The expected type.</param>
    /// <returns>The converted argument; the argument itself when nothing changes.</returns>
    /// <exception cref="XPathException">The argument does not match the type (XPTY0004), or an <c>xs:untypedAtomic</c> value cannot be cast (FORG0001, FOCA0001, FOCA0003).</exception>
    public static XdmSequence Convert(in XdmSequence argument, XPathParameterType type)
    {
        // item()* takes every sequence as it is, so a lazy argument is not read here: fn:count, fn:exists, and fn:empty read it themselves.
        if (type is { ItemType: XPathItemType.Item, Occurrence: Occurrence.ZeroOrMore })
        {
            return argument;
        }

        // A kind test or empty-sequence(): the matcher decides, nothing is atomized.
        if (type.Matcher is { } matcher)
        {
            return matcher.Matches(argument) ? argument : throw XPathErrors.Exception(XPathErrorCodes.XPTY0004);
        }

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
                return ConvertAtomic(XPathAtomization.Atomize(argument), count, type);
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

    private static XdmSequence ConvertAtomic(in XdmSequence atomic, int count, XPathParameterType target)
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

    private static XdmItem ConvertItem(in XdmItem item, XPathParameterType target)
    {
        var itemType = target.ItemType;
        if (itemType == XPathItemType.AnyAtomic)
        {
            return item;
        }

        if (item.Kind == XdmItemKind.UntypedAtomic)
        {
            if (itemType == XPathItemType.UntypedAtomic)
            {
                return item;
            }

            _ = item.TryGetUntypedAtomic(out var lexical);
            return itemType == XPathItemType.Numeric
                ? UntypedAtomicCast.Cast(lexical!, XPathItemType.Double)
                : UntypedAtomicCast.Cast(lexical!, target.AtomicTypeId);
        }

        if (Matches(target, item))
        {
            return item;
        }

        return TryPromote(item, target.AtomicTypeId, out var promoted)
            ? promoted
            : throw XPathErrors.Exception(XPathErrorCodes.XPTY0004);
    }

    // Subtype substitution: the item keeps its own type when that type is the expected one or derives from it. The numeric pseudo type is
    // the union of the three primitive numeric types, which also covers the integers and the types derived from them.
    private static bool Matches(XPathParameterType target, in XdmItem item) =>
        target.ItemType == XPathItemType.Numeric
            ? XdmTypes.IsDerivedFrom(item.TypeId, XdmTypeId.XsDecimal)
                || XdmTypes.IsDerivedFrom(item.TypeId, XdmTypeId.XsFloat)
                || XdmTypes.IsDerivedFrom(item.TypeId, XdmTypeId.XsDouble)
            : XdmTypes.IsDerivedFrom(item.TypeId, target.AtomicTypeId);

    // Numeric promotion (B.1): float to double, decimal to float or double, with the integers as decimals; URI promotion: anyURI to string.
    private static bool TryPromote(in XdmItem item, XdmTypeId target, out XdmItem promoted)
    {
        promoted = default;
        if (target == XdmTypeId.XsDouble)
        {
            if (NumericPromotion.TryToDouble(item, out var number))
            {
                promoted = XdmItem.FromDouble(number);
            }
        }
        else if (target == XdmTypeId.XsFloat)
        {
            if (NumericPromotion.TryToSingle(item, out var single))
            {
                promoted = XdmItem.FromFloat(single);
            }
        }
        else if (target == XdmTypeId.XsString && item.TryGetAnyUri(out var uri))
        {
            promoted = XdmItem.FromString(uri!);
        }

        return promoted.Kind != XdmItemKind.None;
    }
}
