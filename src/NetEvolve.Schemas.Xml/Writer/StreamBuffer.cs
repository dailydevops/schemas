namespace NetEvolve.Schemas.Xml.Writer;

using System;
using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

/// <summary>Collects bytes in memory and hands them to a stream with one <see cref="Stream.WriteAsync(byte[], int, int, CancellationToken)"/> per flush; it never writes synchronously.</summary>
internal sealed class StreamBuffer : IBufferWriter<byte>
{
    private readonly Stream _stream;
    private byte[] _array = new byte[1024];
    private int _count;

    internal StreamBuffer(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        _stream = stream;
    }

    public void Advance(int count) => _count += count;

    public Memory<byte> GetMemory(int sizeHint = 0)
    {
        Ensure(sizeHint);
        return _array.AsMemory(_count);
    }

    public Span<byte> GetSpan(int sizeHint = 0)
    {
        Ensure(sizeHint);
        return _array.AsSpan(_count);
    }

    internal async Task FlushAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (_count == 0)
        {
            return;
        }

#if NETSTANDARD2_0
        await _stream.WriteAsync(_array, 0, _count, cancellationToken).ConfigureAwait(false);
#else
        await _stream.WriteAsync(_array.AsMemory(0, _count), cancellationToken).ConfigureAwait(false);
#endif
        _count = 0;
    }

    private void Ensure(int sizeHint)
    {
        var needed = Math.Max(sizeHint, 1);
        if (_array.Length - _count < needed)
        {
            Array.Resize(ref _array, Math.Max(_array.Length * 2, _count + needed));
        }
    }
}
