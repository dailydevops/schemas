namespace NetEvolve.Schemas.Xsd.Tests.Unit.DependencyInjection;

internal sealed class DisposeTrackingStream : MemoryStream
{
    internal DisposeTrackingStream(byte[] bytes)
        : base(bytes) { }

    internal bool IsDisposed { get; private set; }

    protected override void Dispose(bool disposing)
    {
        IsDisposed = true;
        base.Dispose(disposing);
    }
}
