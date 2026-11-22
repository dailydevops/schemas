namespace NetEvolve.Schemas.Benchmarks.Xml.Dtd;

using NetEvolve.Schemas.Resources;

/// <summary>Serves the external subset from memory to this library; no network and no filesystem is touched.</summary>
internal sealed class InMemoryResolver : IResourceResolver
{
    private readonly ReadOnlyMemory<byte> _subset;

    public InMemoryResolver(byte[] subset) => _subset = subset;

    /// <summary>Gets the number of requests since the last <see cref="Reset"/>.</summary>
    public int Requests { get; private set; }

    public void Reset() => Requests = 0;

    public ResourceResult Resolve(in ResourceRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        Requests++;

        return string.Equals(request.ResolvedUri, DtdBenchmarkInput.SubsetUri, StringComparison.Ordinal)
            ? ResourceResult.Found(_subset, request.ResolvedUri)
            : ResourceResult.NotFound("Only the subset of the benchmark is served.");
    }
}
