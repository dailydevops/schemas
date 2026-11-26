namespace NetEvolve.Schemas.Xml.Model;

using System;
using NetEvolve.Schemas.Xml.Contracts;

/// <summary>
/// The rows of the elements, or of the attributes, of one <see cref="XmlDocumentTree"/> grouped by the low bits of the local name symbol, each group in ascending row (document) order, so a
/// walk for the descendants with one name reads only those elements instead of every row of the subtree.
/// </summary>
/// <remarks>Immutable once built, so any number of threads can read it. It is built from the arrays of the tree and holds one integer per node of its kind.</remarks>
internal sealed class XmlNameIndex
{
    private const int Buckets = 64; // a power of two

    private readonly int[] _starts; // starts[b] to starts[b + 1]: the slots of the nodes in bucket b
    private readonly int[] _rows;

    private XmlNameIndex(int[] starts, int[] rows)
    {
        _starts = starts;
        _rows = rows;
    }

    /// <summary>Builds the index with two passes over the rows.</summary>
    /// <param name="kinds">The node kind per row.</param>
    /// <param name="names">The name per row.</param>
    /// <param name="kind">The kind of the rows to index, <see cref="XmlNodeKind.Element"/> or <see cref="XmlNodeKind.Attribute"/>.</param>
    /// <returns>The index.</returns>
    internal static XmlNameIndex Build(byte[] kinds, XmlName[] names, XmlNodeKind kind)
    {
        var starts = new int[Buckets + 1];
        for (var row = 0; row < kinds.Length; row++)
        {
            if ((XmlNodeKind)kinds[row] == kind)
            {
                starts[BucketOf(names[row].Local) + 1]++;
            }
        }

        for (var bucket = 1; bucket < starts.Length; bucket++)
        {
            starts[bucket] += starts[bucket - 1];
        }

        var rows = new int[starts[Buckets]];
        Span<int> next = stackalloc int[Buckets];
        starts.AsSpan(0, Buckets).CopyTo(next);
        for (var row = 0; row < kinds.Length; row++)
        {
            if ((XmlNodeKind)kinds[row] == kind)
            {
                rows[next[BucketOf(names[row].Local)]++] = row;
            }
        }

        return new XmlNameIndex(starts, rows);
    }

    /// <summary>Finds the candidates for the nodes with a local name symbol in the rows after <paramref name="context"/> and before <paramref name="end"/>.</summary>
    /// <param name="local">The local name symbol.</param>
    /// <param name="context">The row of the context node; its descendants follow it.</param>
    /// <param name="end">The first row after the subtree of the context node.</param>
    /// <param name="rows">The array that holds the rows.</param>
    /// <param name="first">The first slot of <paramref name="rows"/> in the range.</param>
    /// <param name="last">The slot after the last one in the range; equal to <paramref name="first"/> for none.</param>
    /// <remarks>The range holds every node of the name and some nodes of other names that share the bucket, so the caller tests each row.</remarks>
    internal void Find(XmlSymbol local, int context, int end, out int[] rows, out int first, out int last)
    {
        rows = _rows;
        var bucket = BucketOf(local);
        first = LowerBound(_starts[bucket], _starts[bucket + 1], context + 1);
        last = LowerBound(first, _starts[bucket + 1], end);
    }

    // The symbols of the names of a document are handed out one after the other, so the low bits spread them over the buckets.
    private static int BucketOf(XmlSymbol local) => local.Value & (Buckets - 1);

    // The first slot in [low, high) whose row is at least value.
    private int LowerBound(int low, int high, int value)
    {
        while (low < high)
        {
            var middle = low + ((high - low) >> 1);
            if (_rows[middle] < value)
            {
                low = middle + 1;
            }
            else
            {
                high = middle;
            }
        }

        return low;
    }
}
