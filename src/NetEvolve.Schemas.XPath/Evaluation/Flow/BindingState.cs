namespace NetEvolve.Schemas.XPath.Evaluation.Flow;

using System.Runtime.InteropServices;
using NetEvolve.Schemas.XPath.Evaluation.Paths;

/// <summary>
/// What a <see cref="BindingNode"/> carries from binding to binding: the items a <see langword="for"/> expression collects, and the truth value a
/// quantified expression settles. A struct passed by <see langword="ref"/>, so a loop allocates nothing for it.
/// </summary>
[StructLayout(LayoutKind.Auto)]
internal struct BindingState
{
    /// <summary>The items collected so far; the owner disposes the list.</summary>
    public XdmItemList Items;

    /// <summary>The truth value so far.</summary>
    public bool Result;

    /// <summary>Initializes a new instance of the <see cref="BindingState"/> struct.</summary>
    /// <param name="items">The list the items are collected in; <see langword="default"/> when none are collected.</param>
    /// <param name="result">The initial truth value.</param>
    public BindingState(XdmItemList items, bool result)
    {
        Items = items;
        Result = result;
    }
}
