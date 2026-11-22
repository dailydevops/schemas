namespace NetEvolve.Schemas.XPath.Functions.Binding;

using System.Runtime.InteropServices;
using NetEvolve.Arguments;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Types;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>The expected type of one function parameter: an item type and an occurrence indicator.</summary>
/// <remarks><see langword="default"/> is <c>item()</c> exactly once.</remarks>
[StructLayout(LayoutKind.Auto)]
internal readonly struct XPathParameterType
{
    /// <summary>Initializes a new instance of the <see cref="XPathParameterType"/> struct.</summary>
    /// <param name="itemType">The item type.</param>
    /// <param name="occurrence">The occurrence indicator.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">An argument is not a defined value.</exception>
    public XPathParameterType(XPathItemType itemType, Occurrence occurrence)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan((int)itemType, (int)XPathItemType.Item);
        ArgumentOutOfRangeException.ThrowIfGreaterThan((int)itemType, (int)XPathItemType.DayTimeDuration);
        ArgumentOutOfRangeException.ThrowIfLessThan((int)occurrence, (int)Occurrence.ExactlyOne);
        ArgumentOutOfRangeException.ThrowIfGreaterThan((int)occurrence, (int)Occurrence.OneOrMore);

        ItemType = itemType;
        Occurrence = occurrence;
        AtomicType = AtomicTypeOf(itemType);
    }

    /// <summary>Gets the item type.</summary>
    public XPathItemType ItemType { get; }

    /// <summary>Gets the occurrence indicator.</summary>
    public Occurrence Occurrence { get; }

    /// <summary>Gets the handle of the atomic type that the item type names; <see langword="default"/> for <c>item()</c>, <c>node()</c>, and <c>numeric</c>.</summary>
    public XdmTypeId AtomicType { get; }

    private static XdmTypeId AtomicTypeOf(XPathItemType itemType) =>
        itemType switch
        {
            XPathItemType.AnyAtomic => XdmTypes.Get("anyAtomicType"),
            XPathItemType.UntypedAtomic => XdmTypeId.UntypedAtomic,
            _ => UntypedAtomicCast.TypeOf(itemType) ?? default,
        };
}
