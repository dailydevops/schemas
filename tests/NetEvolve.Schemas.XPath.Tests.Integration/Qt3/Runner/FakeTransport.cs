namespace NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Runner;

using NetEvolve.Schemas.Conformance.Fetch;

internal sealed class FakeTransport(byte[] payload) : ISuiteTransport
{
    public Task<Stream> OpenAsync(Uri source, CancellationToken cancellationToken) =>
        Task.FromResult<Stream>(new MemoryStream(payload, writable: false));
}
