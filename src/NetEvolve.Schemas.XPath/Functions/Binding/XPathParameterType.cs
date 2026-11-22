namespace NetEvolve.Schemas.XPath.Functions.Binding;

using System.Collections.Generic;
using System.Runtime.InteropServices;
using NetEvolve.Arguments;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Types;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>The expected type of one function parameter: an item type and an occurrence indicator, or a bound sequence type.</summary>
/// <remarks>
/// <para><see langword="default"/> is <c>item()</c> exactly once.</para>
/// <para>
/// A signature of the library names the item types of <see cref="XPathItemType"/>. <see cref="FromSequenceType"/> gives any sequence type
/// that the shared matcher binds: the item types of the enumeration keep their conversion rules, a further atomic type is matched by
/// derivation after the atomization and the cast of <c>xs:untypedAtomic</c>, and a kind test or <c>empty-sequence()</c> is matched by
/// the matcher without atomization.
/// </para>
/// </remarks>
[StructLayout(LayoutKind.Auto)]
internal readonly struct XPathParameterType
{
    private static readonly Dictionary<XdmTypeId, XPathItemType> Named = NamedItemTypes();

    /// <summary>Initializes a new instance of the <see cref="XPathParameterType"/> struct.</summary>
    /// <param name="itemType">The item type.</param>
    /// <param name="occurrence">The occurrence indicator.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">An argument is not a defined value, or the item type is <see cref="XPathItemType.Other"/>.</exception>
    public XPathParameterType(XPathItemType itemType, Occurrence occurrence)
    {
        Argument.ThrowIfLessThan((int)itemType, (int)XPathItemType.Item);
        Argument.ThrowIfGreaterThan((int)itemType, (int)XPathItemType.DayTimeDuration);
        Argument.ThrowIfLessThan((int)occurrence, (int)Occurrence.ExactlyOne);
        Argument.ThrowIfGreaterThan((int)occurrence, (int)Occurrence.OneOrMore);

        ItemType = itemType;
        Occurrence = occurrence;
        AtomicTypeId = AtomicTypeOf(itemType);
        Matcher = null;
    }

    private XPathParameterType(
        XPathItemType itemType,
        Occurrence occurrence,
        XdmTypeId atomicType,
        SequenceTypeMatcher? matcher
    )
    {
        ItemType = itemType;
        Occurrence = occurrence;
        AtomicTypeId = atomicType;
        Matcher = matcher;
    }

    /// <summary>Gets the item type; <see cref="XPathItemType.Other"/> for a type that the enumeration does not name.</summary>
    public XPathItemType ItemType { get; }

    /// <summary>Gets the occurrence indicator; <see cref="Syntax.Tree.Occurrence.ZeroOrOne"/> stands for <c>empty-sequence()</c>, which <see cref="Matcher"/> decides.</summary>
    public Occurrence Occurrence { get; }

    /// <summary>Gets the handle of the atomic type that the item type names; <see langword="default"/> for <c>item()</c>, <c>node()</c>, <c>numeric</c>, a kind test, and <c>empty-sequence()</c>.</summary>
    public XdmTypeId AtomicTypeId { get; }

    /// <summary>Gets the matcher that decides a kind test or <c>empty-sequence()</c>; <see langword="null"/> for every other type.</summary>
    public SequenceTypeMatcher? Matcher { get; }

    /// <summary>Binds a sequence type as the type of a parameter.</summary>
    /// <param name="syntax">The syntax of the sequence type; no item type stands for <c>empty-sequence()</c>.</param>
    /// <param name="context">The static context that resolves prefixes and the default namespace.</param>
    /// <returns>The parameter type.</returns>
    /// <exception cref="System.ArgumentNullException">An argument is <see langword="null"/>.</exception>
    /// <exception cref="XPathException">A prefix is not bound (XPST0081), a type name is not a built-in type (XPST0051), or a schema declaration is named (XPST0008).</exception>
    public static XPathParameterType FromSequenceType(SequenceType syntax, XPathStaticContext context)
    {
        Argument.ThrowIfNull(syntax);
        Argument.ThrowIfNull(context);

        var occurrence = syntax.Occurrence;
        switch (syntax.ItemType)
        {
            case AnyItemType:
                return new(XPathItemType.Item, occurrence, default, null);
            case KindItemType { Test: AnyKindTest }:
                return new(XPathItemType.Node, occurrence, default, null);
            case AtomicType:
                var atomic = ItemTypeMatcher.Bind(syntax.ItemType, context).Type;
                return new(
                    Named.TryGetValue(atomic, out var named) ? named : XPathItemType.Other,
                    occurrence,
                    atomic,
                    null
                );
            default:
                // A kind test, or empty-sequence() with no item type: the matcher binds both and checks the occurrence itself.
                var matcher = SequenceTypeMatcher.Create(syntax, context);
                var indicator = syntax.ItemType is null ? Occurrence.ZeroOrOne : occurrence;
                return new(XPathItemType.Other, indicator, default, matcher);
        }
    }

    private static XdmTypeId AtomicTypeOf(XPathItemType itemType) =>
        itemType switch
        {
            XPathItemType.AnyAtomic => XdmTypes.Get("anyAtomicType"),
            XPathItemType.UntypedAtomic => XdmTypeId.UntypedAtomic,
            _ => UntypedAtomicCast.TypeOf(itemType) ?? default,
        };

    // The enumeration members that name an atomic type, by handle, so that a sequence type of such a type converts as a signature does.
    private static Dictionary<XdmTypeId, XPathItemType> NamedItemTypes()
    {
        var named = new Dictionary<XdmTypeId, XPathItemType>();
        for (var type = XPathItemType.AnyAtomic; type <= XPathItemType.DayTimeDuration; type++)
        {
            if (type != XPathItemType.Numeric)
            {
                named.Add(AtomicTypeOf(type), type);
            }
        }

        return named;
    }
}
