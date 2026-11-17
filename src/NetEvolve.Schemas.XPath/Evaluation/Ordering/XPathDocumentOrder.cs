namespace NetEvolve.Schemas.XPath.Evaluation.Ordering;

using System;
using System.Buffers;
using NetEvolve.Schemas.XPath.Xdm.Nodes;

/// <summary>
/// Document-order sort with duplicate removal by identity for node sequences (XPath 2.0, 3.2; XDM, Document Order). Buffers come from
/// an <see cref="ArrayPool{T}"/> and are returned on every path, also when the source or a comparison throws.
/// </summary>
/// <remarks>Limitation: the merge sort needs a scratch array as large as the input; nodes of different trees order by tree.</remarks>
internal static class XPathDocumentOrder
{
    private const int InitialCapacity = 256;
    private const int RunLength = 16;

    /// <summary>Drains a source into a pooled buffer and brings it into document order without duplicates, doing only the work the requirement names.</summary>
    /// <typeparam name="TSource">The source type; a struct avoids boxing.</typeparam>
    /// <param name="source">The source.</param>
    /// <param name="requirement">What the sequence needs, from <see cref="XPathOrderAnalysis.GetRequirement"/>.</param>
    /// <param name="pool">The pool, <see langword="null"/> for the shared one.</param>
    /// <returns>The buffer; the caller disposes it.</returns>
    public static XPathNodeBuffer Normalize<TSource>(
        ref TSource source,
        XPathOrderRequirement requirement,
        ArrayPool<XPathNode>? pool
    )
        where TSource : IXPathNodeSource
    {
        var rented = pool ?? ArrayPool<XPathNode>.Shared;
        var buffer = Collect(ref source, rented);
        var done = false;
        try
        {
            var nodes = buffer.Written;
            switch (requirement)
            {
                case XPathOrderRequirement.Reverse:
                    nodes.Reverse();
                    break;
                case XPathOrderRequirement.Deduplicate:
                    buffer.Length = RemoveAdjacentDuplicates(nodes);
                    break;
                case XPathOrderRequirement.Sort:
                    Sort(nodes, rented);
                    buffer.Length = RemoveAdjacentDuplicates(nodes);
                    break;
                default:
                    break;
            }

            done = true;
            return buffer;
        }
        finally
        {
            if (!done)
            {
                buffer.Dispose();
            }
        }
    }

    /// <summary>Drains a source into a pooled buffer sorted by document order with duplicates removed.</summary>
    /// <typeparam name="TSource">The source type; a struct avoids boxing.</typeparam>
    /// <param name="source">The source.</param>
    /// <param name="pool">The pool, <see langword="null"/> for the shared one.</param>
    /// <returns>The buffer; the caller disposes it.</returns>
    public static XPathNodeBuffer SortDistinct<TSource>(ref TSource source, ArrayPool<XPathNode>? pool)
        where TSource : IXPathNodeSource => Normalize(ref source, XPathOrderRequirement.Sort, pool);

    private static XPathNodeBuffer Collect<TSource>(ref TSource source, ArrayPool<XPathNode> pool)
        where TSource : IXPathNodeSource
    {
        var array = pool.Rent(InitialCapacity);
        var count = 0;
        try
        {
            while (source.TryGetNext(out var node))
            {
                if (count == array.Length)
                {
                    var grown = pool.Rent(count * 2);
                    Array.Copy(array, grown, count);
                    pool.Return(array, clearArray: true);
                    array = grown;
                }

                array[count++] = node;
            }
        }
        catch
        {
            pool.Return(array, clearArray: true);
            throw;
        }

        return new XPathNodeBuffer(pool, array, count);
    }

    private static int RemoveAdjacentDuplicates(Span<XPathNode> nodes)
    {
        if (nodes.Length < 2)
        {
            return nodes.Length;
        }

        var kept = 1;
        for (var i = 1; i < nodes.Length; i++)
        {
            if (!nodes[i].IsSameNode(nodes[kept - 1]))
            {
                nodes[kept++] = nodes[i];
            }
        }

        return kept;
    }

    // Insertion sort on short runs, then bottom-up merges between the input and a pooled scratch array.
    private static void Sort(Span<XPathNode> nodes, ArrayPool<XPathNode> pool)
    {
        var length = nodes.Length;
        for (var start = 0; start < length; start += RunLength)
        {
            InsertionSort(nodes.Slice(start, Math.Min(RunLength, length - start)));
        }

        if (length <= RunLength)
        {
            return;
        }

        var scratch = pool.Rent(length);
        try
        {
            var source = nodes;
            var target = new Span<XPathNode>(scratch, 0, length);
            var inScratch = false;
            for (var width = RunLength; width < length; width *= 2)
            {
                for (var low = 0; low < length; low += 2 * width)
                {
                    var middle = Math.Min(low + width, length);
                    var high = Math.Min(low + (2 * width), length);
                    Merge(source, target, low, middle, high);
                }

                var swap = source;
                source = target;
                target = swap;
                inScratch = !inScratch;
            }

            if (inScratch)
            {
                source.CopyTo(nodes);
            }
        }
        finally
        {
            pool.Return(scratch, clearArray: true);
        }
    }

    private static void InsertionSort(Span<XPathNode> run)
    {
        for (var i = 1; i < run.Length; i++)
        {
            var value = run[i];
            var j = i - 1;
            while (j >= 0 && run[j].CompareDocumentOrder(value) > 0)
            {
                run[j + 1] = run[j];
                j--;
            }

            run[j + 1] = value;
        }
    }

    private static void Merge(ReadOnlySpan<XPathNode> source, Span<XPathNode> target, int low, int middle, int high)
    {
        var left = low;
        var right = middle;
        var output = low;
        while (left < middle && right < high)
        {
            target[output++] = source[left].CompareDocumentOrder(source[right]) <= 0 ? source[left++] : source[right++];
        }

        while (left < middle)
        {
            target[output++] = source[left++];
        }

        while (right < high)
        {
            target[output++] = source[right++];
        }
    }
}
