namespace NetEvolve.Schemas.Tests.Unit.Inputs;

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

/// <summary>A memory stream that counts the bytes read, can hide seeking and lie about its length.</summary>
public sealed class TrackingStream : MemoryStream
{
    private const int MaxChunk = 7_000;
    private readonly bool _canSeek;

    public TrackingStream(byte[] bytes, bool canSeek = true)
        : base(bytes) => _canSeek = canSeek;

    public bool IsDisposed { get; private set; }

    public long BytesRead { get; private set; }

    public long? ReportedLength { get; set; }

    public Action? OnRead { get; set; }

    public override bool CanSeek => _canSeek;

    public override long Length => ReportedLength ?? base.Length;

    public override int Read(byte[] buffer, int offset, int count)
    {
        OnRead?.Invoke();
        var read = base.Read(buffer, offset, Math.Min(count, MaxChunk));
        BytesRead += read;
        return read;
    }

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(Read(buffer, offset, count));
    }

    protected override void Dispose(bool disposing)
    {
        IsDisposed = true;
        base.Dispose(disposing);
    }
}
