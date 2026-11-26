namespace NetEvolve.Schemas.XPath.Evaluation.Paths;

using System;
using System.Runtime.InteropServices;
using NetEvolve.Arguments;
using NetEvolve.Schemas.XPath.Limits;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// A growable list of items in a pooled array; dispose it to return the array. A struct, so that holding one allocates nothing: pass it
/// by <see langword="ref"/>, never copy it while it is in use.
/// </summary>
/// <remarks>
/// A list made with <see cref="XdmItemList(int, in SourceSpan)"/> holds at most that many items; one more raises
/// <see cref="XPathLimitErrors.SequenceSizeExceeded"/> with the given location. The bound costs nothing per item: the usable capacity is
/// the smaller of the rented array and the limit, so the comparison that tells a full array from room also tells the limit. Limitation:
/// a <see langword="default"/> list is not bounded; the nodes that multiply items create their lists from the limits of the context.
/// </remarks>
[StructLayout(LayoutKind.Auto)]
internal struct XdmItemList : IDisposable
{
    private XdmItem[]? _array;
    private int _capacity;

    // The most items the array has held since it was rented or last released; the items beyond Count may still be set.
    private int _used;
    private readonly int _limit;
    private readonly SourceSpan _location;

    /// <summary>Initializes a new instance of the <see cref="XdmItemList"/> struct that holds at most <paramref name="limit"/> items.</summary>
    /// <param name="limit">The most items the list holds, at least one.</param>
    /// <param name="location">The location reported when the list would grow beyond the limit.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="limit"/> is below one.</exception>
    public XdmItemList(int limit, in SourceSpan location)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(limit, 1);

        _array = null;
        _capacity = 0;
        _limit = limit;
        _location = location;
        Count = 0;
    }

    /// <summary>Gets the number of items.</summary>
    public int Count { get; private set; }

    /// <summary>Gets the items; valid until the list changes or is disposed.</summary>
    public readonly ReadOnlySpan<XdmItem> Items => new(_array, 0, Count);

    /// <summary>Gets the items for in-place changes; valid until the list grows or is disposed.</summary>
    /// <returns>The items.</returns>
    public readonly Span<XdmItem> AsSpan() => new(_array, 0, Count);

    /// <summary>Adds an item.</summary>
    /// <param name="item">The item.</param>
    public void Add(in XdmItem item)
    {
        if (Count == _capacity)
        {
            Grow(Count + 1);
        }

        _array![Count++] = item;
    }

    /// <summary>Adds items.</summary>
    /// <param name="items">The items.</param>
    public void AddRange(ReadOnlySpan<XdmItem> items)
    {
        if (items.IsEmpty)
        {
            return;
        }

        if (Count + items.Length > _capacity)
        {
            Grow(Count + items.Length);
        }

        items.CopyTo(new Span<XdmItem>(_array, Count, items.Length));
        Count += items.Length;
    }

    /// <summary>Adds the items of a sequence.</summary>
    /// <param name="sequence">The sequence.</param>
    public void AddRange(in XdmSequence sequence)
    {
        var count = sequence.Count;
        for (var i = 0; i < count; i++)
        {
            Add(sequence[i]);
        }
    }

    /// <summary>Removes all items; the array stays.</summary>
    public void Clear()
    {
        _used = Math.Max(_used, Count);
        Count = 0;
    }

    /// <summary>Keeps the first items only.</summary>
    /// <param name="count">The number of items to keep, not more than <see cref="Count"/>.</param>
    /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="count"/> is negative or above <see cref="Count"/>.</exception>
    public void Truncate(int count)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(count, 0);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(count, Count);

        _used = Math.Max(_used, Count);
        Count = count;
    }

    /// <summary>Copies the items into a sequence.</summary>
    /// <returns>The sequence; empty and singleton results allocate nothing.</returns>
    public readonly XdmSequence ToSequence() => XdmSequence.Create(Items);

    /// <summary>Creates a source of the nodes of the list; every item must be a node.</summary>
    /// <returns>The source.</returns>
    public readonly XdmItemListNodeSource GetNodeSource() => new(_array, Count);

    /// <summary>Returns the array to the pool. Subsequent calls are no-ops.</summary>
    public void Dispose()
    {
        if (_array is { } array)
        {
            // Cleared up to the high-water mark: the array holds tree references that a pooled array must not keep alive.
            SmallArrayCache<XdmItem>.Return(array, Math.Max(_used, Count));
            _array = null;
            _capacity = 0;
            Count = 0;
            _used = 0;
        }
    }

    private void Grow(int needed)
    {
        // A list without a limit (default) is bounded by the array size only.
        var limit = _limit == 0 ? int.MaxValue : _limit;
        if (needed > limit)
        {
            throw XPathLimitErrors.SequenceSize(limit, _location);
        }

        var bigger = SmallArrayCache<XdmItem>.Rent(Math.Min(Math.Max(needed, Count * 2), limit));
        if (_array is not null)
        {
            Array.Copy(_array, bigger, Count);
            SmallArrayCache<XdmItem>.Return(_array, Math.Max(_used, Count));
            _used = 0;
        }

        _array = bigger;
        _capacity = Math.Min(bigger.Length, limit);
    }
}
