namespace NetEvolve.Schemas.Schematron.Xslt.Instructions.Grouping;

using System.Collections.Generic;
using NetEvolve.Schemas.Schematron.Xslt.Instructions.Flow;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// One group of <c>xsl:for-each-group</c>: the positions of its items in the population, in population order, and the grouping key of
/// the group (that of its initial item), or no key for a grouping by pattern. The sequence of the items is built once, on the first
/// request, because a sort of the groups and the body ask for it.
/// </summary>
internal sealed class XsltGroup
{
    private XdmSequence? _sequence;

    /// <summary>Initializes a new instance of the <see cref="XsltGroup"/> class without a grouping key.</summary>
    internal XsltGroup() { }

    /// <summary>Initializes a new instance of the <see cref="XsltGroup"/> class with a grouping key.</summary>
    /// <param name="key">The atomic grouping key.</param>
    internal XsltGroup(in XdmItem key) => Key = XdmSequence.Create(in key);

    /// <summary>Gets the positions of the items of the group in the population, zero-based.</summary>
    internal List<int> Members { get; } = [];

    /// <summary>Gets the grouping key as a sequence of one item, or <see langword="null"/> when the group has none.</summary>
    internal XdmSequence? Key { get; }

    /// <summary>Gets the items of the group as a sequence.</summary>
    /// <param name="population">The items of the population.</param>
    /// <returns>The sequence.</returns>
    internal XdmSequence GetSequence(ItemBuffer population)
    {
        ArgumentNullException.ThrowIfNull(population);

        if (_sequence is { } built)
        {
            return built;
        }

        var items = new XdmItem[Members.Count];
        for (var i = 0; i < items.Length; i++)
        {
            items[i] = population.Get(Members[i]);
        }

        var sequence = XdmSequence.Create(items);
        _sequence = sequence;
        return sequence;
    }
}
