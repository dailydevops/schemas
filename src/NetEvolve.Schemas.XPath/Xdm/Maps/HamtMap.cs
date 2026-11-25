namespace NetEvolve.Schemas.XPath.Xdm.Maps;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// An immutable hash map from keys to sequences, a hash array mapped trie: a node holds a bitmap and the entries and child nodes that
/// the bitmaps select, five bits of the key hash pick the slot at each level. <see cref="Put"/> and <see cref="Remove"/> copy only the
/// nodes on the path to the changed entry, so a changed map shares the rest with its source (XPath 3.1, 3.11.1; Functions and Operators
/// 3.1, 17.1). Keys are compared by <see cref="MapKeys"/>.
/// </summary>
/// <remarks>
/// ponytail: no transient builder, so a map of N entries built entry by entry copies O(log N) small arrays per entry; add a mutable
/// builder that is frozen once when a measured construction cost matters. After a remove a node is not folded back into its parent when
/// it keeps a single entry, so the shape is not canonical; lookups stay correct and the depth stays bounded by the hash width.
/// </remarks>
internal sealed class HamtMap
{
    private const int Bits = 5;
    private const int HashWidth = 32;

    private readonly Node? _root;

    private HamtMap(Node? root, int count)
    {
        _root = root;
        Count = count;
    }

    /// <summary>Gets the map without entries.</summary>
    public static HamtMap Empty { get; } = new(null, 0);

    /// <summary>Gets the number of entries.</summary>
    public int Count { get; }

    /// <summary>Looks up the entry of a key.</summary>
    /// <param name="key">The key.</param>
    /// <param name="entry">The entry, <see langword="default"/> when there is none.</param>
    /// <returns><see langword="true"/> when the map has an entry with the same key.</returns>
    public bool TryGetEntry(in XdmItem key, out HamtEntry entry)
    {
        var node = _root;
        if (node is null)
        {
            entry = default;
            return false;
        }

        var hash = MapKeys.Hash(in key);
        var shift = 0;
        while (true)
        {
            if (node.IsCollision)
            {
                return TryFind(node.Entries, hash, in key, out entry);
            }

            var bit = Bit(hash, shift);
            if ((node.DataMap & bit) != 0)
            {
                entry = node.Entries[Index(node.DataMap, bit)];
                return entry.Hash == hash && MapKeys.SameKey(in entry.Key, in key);
            }

            if ((node.NodeMap & bit) == 0)
            {
                entry = default;
                return false;
            }

            node = node.Children[Index(node.NodeMap, bit)];
            shift += Bits;
        }
    }

    /// <summary>Looks up the value of a key.</summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value, empty when there is no entry.</param>
    /// <returns><see langword="true"/> when the map has an entry with the same key.</returns>
    public bool TryGet(in XdmItem key, out XdmSequence value)
    {
        var found = TryGetEntry(in key, out var entry);
        value = entry.Value;
        return found;
    }

    /// <summary>Returns the map with an entry; an entry with the same key is replaced, key included.</summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    /// <returns>The new map; this map is unchanged.</returns>
    public HamtMap Put(in XdmItem key, in XdmSequence value)
    {
        var entry = new HamtEntry(key, value, MapKeys.Hash(in key));
        if (_root is null)
        {
            return new(Node.Single(entry), 1);
        }

        var root = PutAt(_root, entry, 0, out var added);
        return new(root, added ? Count + 1 : Count);
    }

    /// <summary>Returns the map without the entry of a key.</summary>
    /// <param name="key">The key.</param>
    /// <returns>The new map; this map itself when it has no such entry.</returns>
    public HamtMap Remove(in XdmItem key)
    {
        if (_root is null)
        {
            return this;
        }

        var root = RemoveAt(_root, MapKeys.Hash(in key), in key, 0, out var removed);
        return removed ? (root is null ? Empty : new(root, Count - 1)) : this;
    }

    /// <summary>Gets the entries in the order of the trie, which depends on the hashes of the keys.</summary>
    /// <returns>A new array of <see cref="Count"/> entries.</returns>
    public HamtEntry[] Entries()
    {
        var entries = new List<HamtEntry>(Count);
        if (_root is not null)
        {
            Collect(_root, entries);
        }

        return [.. entries];
    }

    private static void Collect(Node node, List<HamtEntry> entries)
    {
        entries.AddRange(node.Entries);
        for (var i = 0; i < node.Children.Length; i++)
        {
            Collect(node.Children[i], entries);
        }
    }

    private static Node PutAt(Node node, in HamtEntry entry, int shift, out bool added)
    {
        if (node.IsCollision)
        {
            var at = IndexOf(node.Entries, entry.Hash, in entry.Key);
            added = at < 0;
            return Node.Collision(at < 0 ? Append(node.Entries, entry) : Replace(node.Entries, at, entry));
        }

        var bit = Bit(entry.Hash, shift);
        if ((node.NodeMap & bit) != 0)
        {
            var child = Index(node.NodeMap, bit);
            return node.WithChild(child, PutAt(node.Children[child], entry, shift + Bits, out added));
        }

        if ((node.DataMap & bit) == 0)
        {
            added = true;
            return node.InsertEntry(bit, Index(node.DataMap, bit), entry);
        }

        var index = Index(node.DataMap, bit);
        var existing = node.Entries[index];
        if (existing.Hash == entry.Hash && MapKeys.SameKey(in existing.Key, in entry.Key))
        {
            added = false;
            return node.WithEntry(index, entry);
        }

        added = true;
        return node.MoveToChild(bit, index, Merge(existing, entry, shift + Bits));
    }

    // Two entries whose hashes agree on every bit above this level end in a collision node at the bottom.
    private static Node Merge(in HamtEntry first, in HamtEntry second, int shift)
    {
        if (shift >= HashWidth)
        {
            return Node.Collision([first, second]);
        }

        var a = Bit(first.Hash, shift);
        var b = Bit(second.Hash, shift);
        return a == b
            ? Node.Nested(a, Merge(first, second, shift + Bits))
            : Node.Pair(a | b, a < b ? first : second, a < b ? second : first);
    }

    private static Node? RemoveAt(Node node, int hash, in XdmItem key, int shift, out bool removed)
    {
        if (node.IsCollision)
        {
            var at = IndexOf(node.Entries, hash, in key);
            removed = at >= 0;
            return at < 0 ? node
                : node.Entries.Length == 1 ? null
                : Node.Collision(Without(node.Entries, at));
        }

        var bit = Bit(hash, shift);
        if ((node.NodeMap & bit) != 0)
        {
            var child = Index(node.NodeMap, bit);
            var reduced = RemoveAt(node.Children[child], hash, in key, shift + Bits, out removed);
            if (!removed)
            {
                return node;
            }

            return reduced is null ? node.WithoutChild(bit, child) : node.WithChild(child, reduced);
        }

        if ((node.DataMap & bit) == 0)
        {
            removed = false;
            return node;
        }

        var index = Index(node.DataMap, bit);
        var existing = node.Entries[index];
        removed = existing.Hash == hash && MapKeys.SameKey(in existing.Key, in key);
        return removed ? node.WithoutEntry(bit, index) : node;
    }

    private static bool TryFind(HamtEntry[] entries, int hash, in XdmItem key, out HamtEntry entry)
    {
        var at = IndexOf(entries, hash, in key);
        entry = at >= 0 ? entries[at] : default;
        return at >= 0;
    }

    private static int IndexOf(HamtEntry[] entries, int hash, in XdmItem key)
    {
        for (var i = 0; i < entries.Length; i++)
        {
            if (entries[i].Hash == hash && MapKeys.SameKey(in entries[i].Key, in key))
            {
                return i;
            }
        }

        return -1;
    }

    private static HamtEntry[] Append(HamtEntry[] entries, in HamtEntry entry)
    {
        var copy = new HamtEntry[entries.Length + 1];
        Array.Copy(entries, copy, entries.Length);
        copy[entries.Length] = entry;
        return copy;
    }

    private static HamtEntry[] Replace(HamtEntry[] entries, int index, in HamtEntry entry)
    {
        var copy = (HamtEntry[])entries.Clone();
        copy[index] = entry;
        return copy;
    }

    private static HamtEntry[] Without(HamtEntry[] entries, int index)
    {
        var copy = new HamtEntry[entries.Length - 1];
        Array.Copy(entries, copy, index);
        Array.Copy(entries, index + 1, copy, index, copy.Length - index);
        return copy;
    }

    private static uint Bit(int hash, int shift) => 1u << (int)(((uint)hash >> shift) & 31);

    // The position of the slot of a bit among the slots that the bitmap holds.
    private static int Index(uint map, uint bit)
    {
        var below = map & (bit - 1);
        below -= (below >> 1) & 0x55555555u;
        below = (below & 0x33333333u) + ((below >> 2) & 0x33333333u);
        below = (below + (below >> 4)) & 0x0F0F0F0Fu;
        return (int)((below * 0x01010101u) >> 24);
    }

    /// <summary>One node of the trie: entries and children by bitmap, or, below the last level of the hash, the entries that share the whole hash.</summary>
    private sealed class Node
    {
        private static readonly HamtEntry[] NoEntries = [];
        private static readonly Node[] NoChildren = [];

        private Node(uint dataMap, uint nodeMap, HamtEntry[] entries, Node[] children, bool isCollision)
        {
            DataMap = dataMap;
            NodeMap = nodeMap;
            Entries = entries;
            Children = children;
            IsCollision = isCollision;
        }

        public uint DataMap { get; }

        public uint NodeMap { get; }

        public HamtEntry[] Entries { get; }

        public Node[] Children { get; }

        public bool IsCollision { get; }

        public static Node Single(in HamtEntry entry) =>
            new(Bit(entry.Hash, 0), 0, [entry], NoChildren, isCollision: false);

        public static Node Collision(HamtEntry[] entries) => new(0, 0, entries, NoChildren, isCollision: true);

        public static Node Nested(uint bit, Node child) => new(0, bit, NoEntries, [child], isCollision: false);

        public static Node Pair(uint bits, in HamtEntry first, in HamtEntry second) =>
            new(bits, 0, [first, second], NoChildren, isCollision: false);

        public Node InsertEntry(uint bit, int index, in HamtEntry entry)
        {
            var entries = new HamtEntry[Entries.Length + 1];
            Array.Copy(Entries, entries, index);
            entries[index] = entry;
            Array.Copy(Entries, index, entries, index + 1, Entries.Length - index);
            return new(DataMap | bit, NodeMap, entries, Children, isCollision: false);
        }

        public Node WithEntry(int index, in HamtEntry entry) =>
            new(DataMap, NodeMap, Replace(Entries, index, entry), Children, isCollision: false);

        public Node WithChild(int index, Node child)
        {
            var children = (Node[])Children.Clone();
            children[index] = child;
            return new(DataMap, NodeMap, Entries, children, isCollision: false);
        }

        // The entry at the bit gives way to a child node at the same bit.
        public Node MoveToChild(uint bit, int entryIndex, Node child)
        {
            var at = Index(NodeMap | bit, bit);
            var children = new Node[Children.Length + 1];
            Array.Copy(Children, children, at);
            children[at] = child;
            Array.Copy(Children, at, children, at + 1, Children.Length - at);
            return new(DataMap & ~bit, NodeMap | bit, Without(Entries, entryIndex), children, isCollision: false);
        }

        public Node? WithoutEntry(uint bit, int index) =>
            Entries.Length == 1 && Children.Length == 0
                ? null
                : new(DataMap & ~bit, NodeMap, Without(Entries, index), Children, isCollision: false);

        public Node? WithoutChild(uint bit, int index)
        {
            if (Children.Length == 1 && Entries.Length == 0)
            {
                return null;
            }

            var children = new Node[Children.Length - 1];
            Array.Copy(Children, children, index);
            Array.Copy(Children, index + 1, children, index, children.Length - index);
            return new(DataMap, NodeMap & ~bit, Entries, children, isCollision: false);
        }
    }
}
