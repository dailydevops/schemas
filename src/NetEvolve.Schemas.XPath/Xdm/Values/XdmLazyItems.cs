namespace NetEvolve.Schemas.XPath.Xdm.Values;

using System.Collections.Generic;

/// <summary>Holds the source of a lazy sequence: it is enumerated once, on the first read, and the result is kept.</summary>
/// <remarks>Limitation: not thread-safe; an evaluation reads its sequences from one thread. The first read drains the whole source.</remarks>
internal sealed class XdmLazyItems
{
    private IEnumerable<XdmItem>? _source;
    private XdmItem[]? _items;

    internal XdmLazyItems(IEnumerable<XdmItem> source) => _source = source;

    internal XdmItem[] Items => _items ??= Drain();

    private XdmItem[] Drain()
    {
        var list = new List<XdmItem>(_source!);
        _source = null;
        return list.ToArray();
    }
}
