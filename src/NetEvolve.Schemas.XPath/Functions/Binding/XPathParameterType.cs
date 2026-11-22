namespace NetEvolve.Schemas.XPath.Functions.Binding;

using System.Runtime.InteropServices;
using NetEvolve.Arguments;
using NetEvolve.Schemas.XPath.Syntax.Tree;

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
    }

    /// <summary>Gets the item type.</summary>
    public XPathItemType ItemType { get; }

    /// <summary>Gets the occurrence indicator.</summary>
    public Occurrence Occurrence { get; }
}
