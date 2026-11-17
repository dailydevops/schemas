namespace NetEvolve.Schemas.Formats;

using System;
using System.Buffers;
using System.IO;

/// <summary>
/// A pooled, growing buffer for the stream adapter. It never holds more than <c>MaxInputSize + 1</c> bytes, so one extra
/// byte proves a breach without reading further. The array is cleared when it goes back to the pool.
/// </summary>
internal sealed class PooledStreamBuffer : IDisposable
{
    private const int UnknownLengthCapacity = 4_096;
    private readonly int _ceiling;
    private byte[] _array;
    private int _limit;
    private int _used;

    internal PooledStreamBuffer(Stream stream, long maxInputSize)
    {
        _ceiling = (int)(maxInputSize + 1);
        var wanted = stream.CanSeek ? Math.Max(stream.Length - stream.Position, 0) + 1 : UnknownLengthCapacity;
        _array = ArrayPool<byte>.Shared.Rent((int)Math.Min(wanted, _ceiling));
        _limit = Math.Min(_array.Length, _ceiling);
    }

    internal byte[] Array => _array;

    /// <summary>Gets the offset where the next read writes.</summary>
    internal int Offset => _used;

    /// <summary>Gets the number of bytes the next read may fill.</summary>
    internal int Count => _limit - _used;

    internal Memory<byte> Window => new(_array, _used, _limit - _used);

    internal ReadOnlySpan<byte> Content => new(_array, 0, _used);

    /// <summary>Records a read; returns <see langword="false"/> when the content exceeds <c>MaxInputSize</c>.</summary>
    internal bool Advance(int read)
    {
        _used += read;
        if (_used == _ceiling)
        {
            return false;
        }

        if (_used == _limit)
        {
            Grow();
        }

        return true;
    }

    public void Dispose() => ArrayPool<byte>.Shared.Return(_array, clearArray: true);

    private void Grow()
    {
        var next = ArrayPool<byte>.Shared.Rent((int)Math.Min((long)_limit * 2, _ceiling));
        Buffer.BlockCopy(_array, 0, next, 0, _used);
        ArrayPool<byte>.Shared.Return(_array, clearArray: true);
        _array = next;
        _limit = Math.Min(next.Length, _ceiling);
    }
}
