namespace NetEvolve.Schemas.XPath.Evaluation.Ordering;

using System;
using System.Buffers;
using System.Runtime.InteropServices;
using NetEvolve.Schemas.XPath.Xdm.Nodes;

/// <summary>A node sequence in a pooled array; dispose it to return the array. A struct, so that holding one allocates nothing.</summary>
[StructLayout(LayoutKind.Auto)]
internal struct XPathNodeBuffer : IDisposable
{
    private readonly ArrayPool<XPathNode> _pool;
    private XPathNode[]? _array;

    internal XPathNodeBuffer(ArrayPool<XPathNode> pool, XPathNode[] array, int length)
    {
        _pool = pool;
        _array = array;
        Length = length;
    }

    /// <summary>Gets the number of nodes.</summary>
    public int Length { get; internal set; }

    /// <summary>Gets the nodes; valid until the buffer is disposed.</summary>
    public readonly ReadOnlySpan<XPathNode> Nodes => new(_array, 0, Length);

    internal readonly Span<XPathNode> Written => new(_array, 0, Length);

    /// <summary>Returns the array to the pool. Subsequent calls are no-ops.</summary>
    public void Dispose()
    {
        if (_array is { } array)
        {
            _array = null;
            Length = 0;
            // Cleared: the array holds tree references that a pooled array must not keep alive.
            _pool.Return(array, clearArray: true);
        }
    }
}
