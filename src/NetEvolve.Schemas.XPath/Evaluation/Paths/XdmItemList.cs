namespace NetEvolve.Schemas.XPath.Evaluation.Paths;

using System;
using System.Buffers;
using System.Runtime.InteropServices;
using NetEvolve.Arguments;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// A growable list of items in a pooled array; dispose it to return the array. A struct, so that holding one allocates nothing: pass it
/// by <see langword="ref"/>, never copy it while it is in use.
/// </summary>
/// <remarks>Limitation: the list is not bounded; sequence size is limited by XP-75 (#229).</remarks>
[StructLayout(LayoutKind.Auto)]
internal struct XdmItemList : IDisposable
{
    private XdmItem[]? _array;

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
        if (_array is null || Count == _array.Length)
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

        if (_array is null || Count + items.Length > _array.Length)
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
    public void Clear() => Count = 0;

    /// <summary>Keeps the first items only.</summary>
    /// <param name="count">The number of items to keep, not more than <see cref="Count"/>.</param>
    /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="count"/> is negative or above <see cref="Count"/>.</exception>
    public void Truncate(int count)
    {
        Argument.ThrowIfLessThan(count, 0);
        Argument.ThrowIfGreaterThan(count, Count);

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
            _array = null;
            Count = 0;
            // Cleared: the array holds tree references that a pooled array must not keep alive.
            ArrayPool<XdmItem>.Shared.Return(array, clearArray: true);
        }
    }

    private void Grow(int needed)
    {
        var pool = ArrayPool<XdmItem>.Shared;
        var bigger = pool.Rent(Math.Max(needed, Count * 2));
        if (_array is not null)
        {
            Array.Copy(_array, bigger, Count);
            pool.Return(_array, clearArray: true);
        }

        _array = bigger;
    }
}
