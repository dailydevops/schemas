namespace NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Engine;

using NetEvolve.Schemas.Resources;
using NetEvolve.Schemas.XPath.Abstractions;

/// <summary>
/// Serves the source documents of a case environment to <c>fn:doc</c> and <c>fn:doc-available</c> by the <c>uri</c> of the source;
/// every other URI is not found, so the call raises <c>FODC0002</c> as it does without a resolver.
/// </summary>
internal sealed class Qt3SourceResolver : IXPathResourceResolver
{
    private readonly Dictionary<string, string> _files = new(StringComparer.Ordinal);

    /// <summary>Initializes a new instance of the <see cref="Qt3SourceResolver"/> class.</summary>
    /// <param name="sources">The sources of the environment; those without a <c>uri</c> are not served.</param>
    public Qt3SourceResolver(IEnumerable<Qt3Source> sources)
    {
        foreach (var source in sources)
        {
            if (source.Uri is not null)
            {
                _files[source.Uri] = source.File;
            }
        }
    }

    /// <inheritdoc />
    public ResourceResult Resolve(in ResourceRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var key = request.ResolvedUri ?? request.Reference;
        return _files.TryGetValue(key, out var file)
            ? ResourceResult.Found(File.ReadAllBytes(file), key)
            : ResourceResult.NotFound("not a source of the environment");
    }
}
