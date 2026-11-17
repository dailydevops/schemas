namespace NetEvolve.Schemas.XPath.Xdm.Values;

using System;
using System.Runtime.InteropServices;
using NetEvolve.Arguments;

/// <summary>A flat sequence of items: never nested, and a singleton equals its item.</summary>
/// <remarks>Limitation: this contract copies the items into an array; the pooled sequence of XP-06 replaces the storage behind the same members.</remarks>
[StructLayout(LayoutKind.Auto)]
public readonly struct XdmSequence : IEquatable<XdmSequence>
{
    private readonly XdmItem[]? _items;

    private XdmSequence(XdmItem[] items) => _items = items;

    /// <summary>Gets the empty sequence; <see langword="default"/> is empty too.</summary>
    public static XdmSequence Empty => default;

    /// <summary>Gets the number of items.</summary>
    public int Count => _items?.Length ?? 0;

    /// <summary>Gets a value indicating whether the sequence has no item.</summary>
    public bool IsEmpty => Count == 0;

    /// <summary>Gets an item.</summary>
    /// <param name="index">The zero-based position.</param>
    /// <returns>The item.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is outside the sequence.</exception>
    public XdmItem this[int index]
    {
        get
        {
            Argument.ThrowIfLessThan(index, 0);
            Argument.ThrowIfGreaterThanOrEqual(index, Count);

            return _items![index];
        }
    }

    /// <summary>Creates a sequence of one item.</summary>
    /// <param name="item">The item.</param>
    /// <returns>The sequence.</returns>
    public static XdmSequence Create(in XdmItem item) => new([item]);

    /// <summary>Creates a sequence of the items; the items are copied.</summary>
    /// <param name="items">The items.</param>
    /// <returns>The sequence.</returns>
    public static XdmSequence Create(ReadOnlySpan<XdmItem> items) => items.IsEmpty ? default : new(items.ToArray());

    /// <summary>Gets the enumerator.</summary>
    /// <returns>A struct enumerator.</returns>
    public XdmSequenceEnumerator GetEnumerator() => new(_items ?? []);

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
        for (var i = 0; i < Count; i++)
        {
            if (!_items![i].Equals(other._items![i]))
            {
                return false;
            }
        }

        return true;
    }

    private int HashItems()
    {
        var hash = Count;
        for (var i = 0; i < Count; i++)
        {
            hash = unchecked((hash * 31) + _items![i].GetHashCode());
        }

        return hash;
    }
}
