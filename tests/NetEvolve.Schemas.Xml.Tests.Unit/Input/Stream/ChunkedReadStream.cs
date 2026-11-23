namespace NetEvolve.Schemas.Xml.Tests.Unit.Input.Stream;

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Mono.Cecil;

/// <summary>A read-only stream that hands out the bytes in chunks of a chosen size and can fail or cancel on a chosen read.</summary>
internal sealed class ChunkedReadStream : System.IO.Stream
{
    private readonly byte[] _data;
    private readonly Func<int, int> _chunkSize;
    private int _position;

    internal ChunkedReadStream(byte[] data, Func<int, int> chunkSize)
    {
        _data = data;
        _chunkSize = chunkSize;
    }

    /// <summary>Gets or sets the zero-based read call that throws <see cref="IOException"/>, -1 for never.</summary>
    internal int FaultOnRead { get; set; } = -1;

    /// <summary>Gets or sets the zero-based read call during which the source is canceled, -1 for never.</summary>
    internal int CancelOnRead { get; set; } = -1;

    /// <summary>Gets or sets the source canceled by <see cref="CancelOnRead"/>.</summary>
    internal CancellationTokenSource? Source { get; set; }

    /// <summary>Gets or sets a value indicating whether the stream observes its token or returns data as if it ignored it.</summary>
    internal bool ObserveToken { get; set; } = true;

    internal int Reads { get; private set; }

    internal bool IsDisposed { get; private set; }

    public override bool CanRead => true;

    public override bool CanSeek => false;

    public override bool CanWrite => false;

    public override long Length => throw new NotSupportedException();

    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
        ReadCoreAsync(buffer.AsMemory(offset, count), Observed(cancellationToken)).AsTask();

    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
        ReadCoreAsync(buffer, Observed(cancellationToken));

    public override int Read(byte[] buffer, int offset, int count) => Take(buffer.AsSpan(offset, count));

    public override void Flush() { }

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    public override void SetLength(long value) => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    protected override void Dispose(bool disposing)
    {
        IsDisposed = true;
        base.Dispose(disposing);
    }

    private CancellationToken Observed(CancellationToken cancellationToken) =>
        ObserveToken ? cancellationToken : CancellationToken.None;

    private async ValueTask<int> ReadCoreAsync(Memory<byte> buffer, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var call = Reads++;
        if (call == FaultOnRead)
        {
            throw new IOException("fault");
        }

        if (call == CancelOnRead)
        {
#if NET10_0_OR_GREATER
            await Source!.CancelAsync();
#else
            Source!.Cancel();
#endif
            cancellationToken.ThrowIfCancellationRequested();
        }

        return Take(buffer.Span);
    }

    private int Take(Span<byte> buffer)
    {
        var take = Math.Min(Math.Min(buffer.Length, _chunkSize(Reads)), _data.Length - _position);
        _data.AsSpan(_position, take).CopyTo(buffer);
        _position += take;
        return take;
    }
}
