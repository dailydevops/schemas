namespace NetEvolve.Schemas.Schematron.Xslt.Instructions.Flow;

using System;
using System.Buffers;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// An <see cref="SequenceSinkKind.Items"/> sink that holds at most a given number of items in a pooled array: the select sequence
/// of <c>xsl:for-each</c> and the sort key of one item. It answers the size of the sequence when <c>last()</c> asks for it
/// (design #150 RD-7). A push beyond the limit is dropped and flagged, so the owner ends the instruction with the coded error.
/// </summary>
internal sealed class ItemBuffer : ISequenceSink, IFocusSizeSource, IDisposable
{
    private readonly int _max;
    private XdmItem[] _items;
    private int _count;

    /// <summary>Initializes a new instance of the <see cref="ItemBuffer"/> class.</summary>
    /// <param name="max">The most items the buffer holds.</param>
    internal ItemBuffer(int max)
    {
        Argument.ThrowIfLessThan(max, 1);

        _max = max;
        _items = ArrayPool<XdmItem>.Shared.Rent(Math.Min(max, 16));
    }

    /// <inheritdoc />
    public SequenceSinkKind Kind => SequenceSinkKind.Items;

    /// <inheritdoc />
    public IXmlSink Content => throw new InvalidOperationException("Only a tree sink has content.");

    /// <summary>Gets the number of items held.</summary>
    internal int Count => _count;

    /// <summary>Gets a value indicating whether an item was dropped because the buffer was full.</summary>
    internal bool Overflow { get; private set; }

    /// <summary>Gets an item.</summary>
    /// <param name="index">The zero-based index.</param>
    /// <returns>The item.</returns>
    internal XdmItem Get(int index)
    {
        Argument.ThrowIfLessThan(index, 0);
        Argument.ThrowIfGreaterThanOrEqual(index, _count);

        return _items[index];
    }

    /// <summary>Empties the buffer and keeps its array.</summary>
    internal void Reset()
    {
        Array.Clear(_items, 0, _count);
        _count = 0;
        Overflow = false;
    }

    /// <inheritdoc />
    public int GetSize() => _count;

    /// <inheritdoc />
    public void Item(in XdmItem item)
    {
        if (_count >= _max)
        {
            Overflow = true;
            return;
        }

        if (_count == _items.Length)
        {
            var grown = ArrayPool<XdmItem>.Shared.Rent(Math.Min(_max, _items.Length * 2));
            Array.Copy(_items, grown, _count);
            Array.Clear(_items, 0, _count);
            ArrayPool<XdmItem>.Shared.Return(_items);
            _items = grown;
        }

        _items[_count++] = item;
    }

    /// <inheritdoc />
    public void Text(ReadOnlySpan<char> text)
    {
        if (!text.IsEmpty)
        {
            throw new InvalidOperationException("An expression pushes items, not text.");
        }
    }

    /// <inheritdoc />
    public IXmlSink BeginTree() => throw new InvalidOperationException("An expression builds no temporary tree.");

    /// <inheritdoc />
    public XdmItem EndTree(IXmlSink tree) =>
        throw new InvalidOperationException("An expression builds no temporary tree.");

    /// <inheritdoc />
    public void Dispose()
    {
        var items = _items;
        _items = [];
        if (items.Length > 0)
        {
            Array.Clear(items, 0, _count);
            ArrayPool<XdmItem>.Shared.Return(items);
        }

        _count = 0;
    }
}
