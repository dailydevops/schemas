namespace NetEvolve.Schemas.XPath.Xdm.Values;

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using NetEvolve.Arguments;

/// <summary>A flat sequence of items: never nested, and a singleton equals its item.</summary>
/// <remarks>
/// The empty sequence is <see langword="default"/>, and a singleton keeps its item inline, so neither allocates. Limitation:
/// two or more items are copied into an array. The size of a sequence an evaluation builds is bounded by the sequence size limit of the evaluation (XP-75, #229) where results are collected (path steps); a sequence a host passes in is not checked. Lazy sequences and concatenation are internal.
/// </remarks>
[StructLayout(LayoutKind.Auto)]
public readonly struct XdmSequence : IEquatable<XdmSequence>
{
    // _many is null (empty or singleton), an XdmItem[] (two or more items), or an XdmLazyItems (count unknown until read: _count is -1).
    private readonly XdmItem _single;
    private readonly object? _many;
    private readonly int _count;

    private XdmSequence(in XdmItem single)
    {
        _single = single;
        _many = null;
        _count = 1;
    }

    private XdmSequence(XdmItem[] items)
    {
        _single = default;
        _many = items;
        _count = items.Length;
    }

    private XdmSequence(XdmLazyItems lazy)
    {
        _single = default;
        _many = lazy;
        _count = -1;
    }

    /// <summary>Gets the empty sequence; <see langword="default"/> is empty too.</summary>
    public static XdmSequence Empty => default;

    /// <summary>Gets the number of items.</summary>
    public int Count => _count >= 0 ? _count : ((XdmLazyItems)_many!).Items.Length;

    /// <summary>Gets a value indicating whether the sequence has no item.</summary>
    public bool IsEmpty => Count == 0;

    /// <summary>Determines whether the sequence has an item; a lazy sequence that nobody has read is asked for one item and not read.</summary>
    /// <returns><see langword="true"/> when there is an item.</returns>
    /// <remarks>Limitation: such a lazy source is enumerated again when the sequence is read afterwards, because nothing is kept.</remarks>
    internal bool HasItems()
    {
        if (_many is XdmLazyItems lazy && lazy.UnreadSource is { } source)
        {
            using var items = source.GetEnumerator();
            return items.MoveNext();
        }

        return Count != 0;
    }

    /// <summary>Counts the items; a lazy sequence that nobody has read is counted as it is enumerated, and its items are not kept.</summary>
    /// <returns>The number of items.</returns>
    /// <remarks>Limitation: such a lazy source is enumerated again when the sequence is read afterwards, because nothing is kept.</remarks>
    internal int CountItems()
    {
        if (_many is XdmLazyItems lazy && lazy.UnreadSource is { } source)
        {
            var count = 0;
            using var items = source.GetEnumerator();
            while (items.MoveNext())
            {
                count++;
            }

            return count;
        }

        return Count;
    }

    /// <summary>Gets an item.</summary>
    /// <param name="index">The zero-based position.</param>
    /// <returns>The item.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is outside the sequence.</exception>
    public XdmItem this[int index]
    {
        get
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(index, 0);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, Count);

            return _many switch
            {
                null => _single,
                XdmItem[] items => items[index],
                _ => ((XdmLazyItems)_many).Items[index],
            };
        }
    }

    /// <summary>Creates a sequence of one item.</summary>
    /// <param name="item">The item.</param>
    /// <returns>The sequence.</returns>
    public static XdmSequence Create(in XdmItem item) => new(item);

    /// <summary>Creates a sequence of the items; the items are copied.</summary>
    /// <param name="items">The items.</param>
    /// <returns>The sequence.</returns>
    public static XdmSequence Create(ReadOnlySpan<XdmItem> items) =>
        items.Length switch
        {
            0 => default,
            1 => new(items[0]),
            _ => new(items.ToArray()),
        };

    /// <summary>Creates a lazy sequence: the source is enumerated once, when the sequence is first read.</summary>
    /// <param name="source">The source.</param>
    /// <returns>The sequence.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
    internal static XdmSequence FromLazy(IEnumerable<XdmItem> source)
    {
        ArgumentNullException.ThrowIfNull(source);

        return new(new XdmLazyItems(source));
    }

    /// <summary>Concatenates two sequences into one flat sequence; lazy operands are read.</summary>
    /// <param name="left">The first sequence.</param>
    /// <param name="right">The second sequence.</param>
    /// <returns>The sequence; an empty operand yields the other one without copying.</returns>
    internal static XdmSequence Concat(in XdmSequence left, in XdmSequence right)
    {
        var leftCount = left.Count;
        var rightCount = right.Count;
        if (leftCount == 0)
        {
            return right.Materialize();
        }

        if (rightCount == 0)
        {
            return left.Materialize();
        }

        var items = new XdmItem[leftCount + rightCount];
        for (var i = 0; i < leftCount; i++)
        {
            items[i] = left[i];
        }

        for (var i = 0; i < rightCount; i++)
        {
            items[leftCount + i] = right[i];
        }

        return new(items);
    }

    /// <summary>Reads a lazy sequence completely and returns the sequence without the lazy source.</summary>
    /// <returns>This sequence when it is not lazy.</returns>
    internal XdmSequence Materialize() =>
        _many is XdmLazyItems lazy
            ? lazy.Items.Length switch
            {
                0 => default,
                1 => new(lazy.Items[0]),
                _ => new(lazy.Items),
            }
            : this;

    /// <summary>Gets the enumerator.</summary>
    /// <returns>A struct enumerator.</returns>
    public XdmSequenceEnumerator GetEnumerator() => new(this);

    /// <inheritdoc />
    public bool Equals(XdmSequence other) => Count == other.Count && ItemsEqual(other);

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is XdmSequence other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => HashItems();

    /// <summary>Determines whether two sequences are equal.</summary>
    /// <param name="left">The first value.</param>
    /// <param name="right">The second value.</param>
    /// <returns><see langword="true"/> when equal.</returns>
    public static bool operator ==(XdmSequence left, XdmSequence right) => left.Equals(right);

    /// <summary>Determines whether two sequences differ.</summary>
    /// <param name="left">The first value.</param>
    /// <param name="right">The second value.</param>
    /// <returns><see langword="true"/> when different.</returns>
    public static bool operator !=(XdmSequence left, XdmSequence right) => !left.Equals(right);

    private bool ItemsEqual(XdmSequence other)
    {
        var count = Count;
        for (var i = 0; i < count; i++)
        {
            if (!this[i].Equals(other[i]))
            {
                return false;
            }
        }

        return true;
    }

    private int HashItems()
    {
        var count = Count;
        var hash = count;
        for (var i = 0; i < count; i++)
        {
            hash = unchecked((hash * 31) + this[i].GetHashCode());
        }

        return hash;
    }
}
