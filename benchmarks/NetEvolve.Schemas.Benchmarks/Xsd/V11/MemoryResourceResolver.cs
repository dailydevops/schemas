namespace NetEvolve.Schemas.Benchmarks.Xsd.V11;

using NetEvolve.Schemas.Resources;

/// <summary>Serves the schema documents of a corpus from memory, so no benchmark touches the network or the disk.</summary>
internal sealed class MemoryResourceResolver : IResourceResolver
{
    private readonly IReadOnlyDictionary<Uri, byte[]> _documents;

    public MemoryResourceResolver(IReadOnlyDictionary<Uri, byte[]> documents) => _documents = documents;

    public ResourceResult Resolve(in ResourceRequest request, CancellationToken cancellationToken) =>
        request.ResolvedUri is { } uri && _documents.TryGetValue(new Uri(uri), out var content)
            ? ResourceResult.Found(content, uri)
            : ResourceResult.NotFound(request.Reference);
}
