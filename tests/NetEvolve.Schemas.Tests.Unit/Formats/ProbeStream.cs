namespace NetEvolve.Schemas.Tests.Unit.Formats;

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

/// <summary>A memory stream that counts synchronous and asynchronous reads, hands out small chunks, and can hide seeking.</summary>
internal sealed class ProbeStream(byte[] bytes, bool canSeek = true, int chunk = 4) : MemoryStream(bytes)
{
    public int SyncReads { get; private set; }

    public int AsyncReads { get; private set; }

    public bool ThrowOnSyncRead { get; init; }

    public long? ReportedLength { get; init; }

    public Action? OnRead { get; set; }

    public override bool CanSeek => canSeek;

    public override long Length => ReportedLength ?? base.Length;

    public override int Read(byte[] buffer, int offset, int count)
    {
        SyncReads++;
        if (ThrowOnSyncRead)
        {
            throw new InvalidOperationException("A synchronous read is not allowed.");
        }

        OnRead?.Invoke();
        return base.Read(buffer, offset, Math.Min(count, chunk));
    }

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        AsyncReads++;
        OnRead?.Invoke();
        return Task.FromResult(base.Read(buffer, offset, Math.Min(count, chunk)));
    }

    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        AsyncReads++;
        OnRead?.Invoke();
        var scratch = new byte[Math.Min(buffer.Length, chunk)];
        var read = base.Read(scratch, 0, scratch.Length);
        scratch.AsMemory(0, read).CopyTo(buffer);
        return ValueTask.FromResult(read);
    }
}
