#pragma warning disable CA1815, CA1034 // Approved shape (#123): the struct defines no value equality and exposes its struct enumerator as a nested type.

namespace NetEvolve.Schemas.XPath.Xdm.Values;

using System;
using System.Runtime.InteropServices;
using NetEvolve.Arguments;

/// <summary>A flat sequence of items: never nested, and a singleton equals its item.</summary>
/// <remarks>Limitation: this contract copies the items into an array; the pooled sequence of XP-06 replaces the storage behind the same members.</remarks>
[StructLayout(LayoutKind.Auto)]
public readonly struct XdmSequence
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
    public Enumerator GetEnumerator() => new(_items ?? []);

    /// <summary>Enumerates the items of a sequence without allocating.</summary>
    [StructLayout(LayoutKind.Auto)]
    public struct Enumerator
    {
        private readonly XdmItem[] _items;
        private int _index;

        internal Enumerator(XdmItem[] items)
        {
            _items = items;
            _index = -1;
        }

        /// <summary>Gets the current item.</summary>
        public readonly XdmItem Current => _items[_index];

        /// <summary>Advances to the next item.</summary>
        /// <returns><see langword="true"/> when there is one.</returns>
        public bool MoveNext() => ++_index < _items.Length;
    }
}
