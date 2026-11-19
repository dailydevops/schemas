namespace NetEvolve.Schemas.Xml.Tests.Unit.Dtd.External.Loader;

using System;
using System.IO;

/// <summary>A read-only stream over bytes that records its disposal and can return few bytes per read or fail on read.</summary>
internal sealed class TrackingStream : MemoryStream
{
    private readonly int _maxRead;

    internal TrackingStream(byte[] content, int maxRead = int.MaxValue)
        : base(content, writable: false) => _maxRead = maxRead;

    /// <summary>Gets a value indicating whether the stream was disposed.</summary>
    internal bool IsDisposed { get; private set; }

    /// <summary>Gets or sets a value indicating whether every read throws.</summary>
    internal bool FailOnRead { get; set; }

    public override int Read(byte[] buffer, int offset, int count)
    {
        if (FailOnRead)
        {
            throw new IOException("scripted read failure");
        }

        return base.Read(buffer, offset, Math.Min(count, _maxRead));
    }

    protected override void Dispose(bool disposing)
    {
        IsDisposed = true;
        base.Dispose(disposing);
    }
}
