namespace NetEvolve.Schemas.XPath.Xdm.Values;

using System.Collections.Generic;

/// <summary>Holds the source of a lazy sequence: it is enumerated once, on the first read, and the result is kept.</summary>
/// <remarks>
/// Limitation: not thread-safe; an evaluation reads its sequences from one thread. The first read drains the whole source. The one
/// exception is <see cref="UnreadSource"/>: <c>fn:count</c>, <c>fn:exists</c>, <c>fn:empty</c>, <c>fn:subsequence</c>, <c>fn:zero-or-one</c>, <c>fn:one-or-more</c>, and <c>fn:exactly-one</c>
/// pull from a source that nobody has read without keeping the items (<see cref="XdmSequenceCursor"/>), so a source can be enumerated more than once.
/// </remarks>
internal sealed class XdmLazyItems
{
    private IEnumerable<XdmItem>? _source;
    private XdmItem[]? _items;

    internal XdmLazyItems(IEnumerable<XdmItem> source) => _source = source;

    internal XdmItem[] Items => _items ??= Drain();

    /// <summary>Gets the source while nobody has read the sequence; <see langword="null"/> afterwards, when <see cref="Items"/> holds the items.</summary>
    internal IEnumerable<XdmItem>? UnreadSource => _items is null ? _source : null;

    private XdmItem[] Drain()
    {
        var list = new List<XdmItem>(_source!);
        _source = null;
        return list.ToArray();
    }
}
