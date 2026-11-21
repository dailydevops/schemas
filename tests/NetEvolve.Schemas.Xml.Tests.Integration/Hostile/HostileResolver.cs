namespace NetEvolve.Schemas.Xml.Tests.Integration.Hostile;

using System.Text;
using NetEvolve.Schemas.Resources;

/// <summary>A resolver that records every request and serves the scripted resources by resolved URI, so a test sees whether the loader asked at all.</summary>
internal sealed class HostileResolver : IResourceResolver
{
    private readonly IReadOnlyDictionary<string, Func<string>> _resources;

    /// <summary>Initializes a new instance of the <see cref="HostileResolver"/> class.</summary>
    /// <param name="resources">The resources by absolute URI, <see langword="null"/> for none.</param>
    public HostileResolver(IReadOnlyDictionary<string, Func<string>>? resources) =>
        _resources = resources ?? new Dictionary<string, Func<string>>(StringComparer.Ordinal);

    /// <summary>Gets the references of the requests received, in order.</summary>
    public List<string> Requests { get; } = [];

    /// <inheritdoc />
    public ResourceResult Resolve(in ResourceRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var key = request.ResolvedUri ?? request.Reference;
        Requests.Add(key);

        return _resources.TryGetValue(key, out var build)
            ? ResourceResult.Found(new UTF8Encoding(false).GetBytes(build()), request.ResolvedUri)
            : ResourceResult.NotFound("The corpus has no such resource.");
    }
}
