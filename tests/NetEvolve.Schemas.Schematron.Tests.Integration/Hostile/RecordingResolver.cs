namespace NetEvolve.Schemas.Schematron.Tests.Integration.Hostile;

using System.Collections.Concurrent;
using System.Text;
using NetEvolve.Schemas.Resources;

/// <summary>
/// A resolver over an in-memory map that records every request and every successful load. It never touches the file system or the network,
/// so a request for an address outside the map ends as <see cref="ResourceStatus.Denied"/>.
/// </summary>
internal sealed class RecordingResolver : IResourceResolver
{
    private readonly IReadOnlyDictionary<string, string> _documents;
    private readonly ConcurrentQueue<string> _requests = new();
    private int _loads;

    public RecordingResolver(IReadOnlyDictionary<string, string> documents)
    {
        ArgumentNullException.ThrowIfNull(documents);
        _documents = documents;
    }

    /// <summary>Gets the address of every request, in arrival order.</summary>
    public IReadOnlyCollection<string> Requests => _requests;

    /// <summary>Gets the number of requests that ended with a document.</summary>
    public int SuccessfulLoads => Volatile.Read(ref _loads);

    public ResourceResult Resolve(in ResourceRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var address = request.ResolvedUri ?? request.Reference;
        _requests.Enqueue(address);
        if (request.ResolvedUri is not null && _documents.TryGetValue(request.ResolvedUri, out var text))
        {
            _ = Interlocked.Increment(ref _loads);
            return ResourceResult.Found(new MemoryStream(Encoding.UTF8.GetBytes(text)), request.ResolvedUri);
        }

        return ResourceResult.Denied($"Hostile corpus: '{address}' is not part of the map.");
    }
}
