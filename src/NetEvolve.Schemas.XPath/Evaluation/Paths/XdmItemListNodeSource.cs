namespace NetEvolve.Schemas.XPath.Evaluation.Paths;

using System.Runtime.InteropServices;
using NetEvolve.Schemas.XPath.Evaluation.Ordering;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>A pull source over the nodes of an <see cref="XdmItemList"/>; the caller has checked that every item is a node.</summary>
[StructLayout(LayoutKind.Auto)]
internal struct XdmItemListNodeSource : IXPathNodeSource
{
    private readonly XdmItem[]? _items;
    private readonly int _count;
    private int _index;

    internal XdmItemListNodeSource(XdmItem[]? items, int count)
    {
        _items = items;
        _count = count;
        _index = 0;
    }

    /// <inheritdoc />
    public bool TryGetNext(out XPathNode node)
    {
        if (_index < _count)
        {
            _ = _items![_index++].TryGetNode(out node);
            return true;
        }

        node = default;
        return false;
    }
}
