namespace NetEvolve.Schemas.Conformance.Tests.Unit.Fetch;

using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using NetEvolve.Schemas.Conformance.Fetch;
using NetEvolve.Schemas.Conformance.Manifest;

internal sealed class FakeTransport(byte[] payload) : ISuiteTransport
{
    public int Calls { get; private set; }

    public Task<Stream> OpenAsync(Uri source, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Calls++;
        return Task.FromResult<Stream>(new MemoryStream(payload, writable: false));
    }
}
