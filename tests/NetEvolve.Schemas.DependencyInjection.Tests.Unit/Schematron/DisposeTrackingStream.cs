namespace NetEvolve.Schemas.DependencyInjection.Tests.Unit.Schematron;

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
