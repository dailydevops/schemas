#pragma warning disable CA1054 // The approved contract passes URIs as strings.

namespace NetEvolve.Schemas.Testing.Fakes;

using System;
using System.Collections.Generic;
using System.Threading;
using NetEvolve.Schemas.Resources;

/// <summary>Resolver that serves registered content by resolved URI, or by reference when no URI was resolved.</summary>
/// <remarks>Not thread-safe while resources are being added; resolving is safe once registration is complete.</remarks>
public sealed class InMemoryResourceResolver : IResourceResolver
{
    private readonly Dictionary<string, byte[]> _resources = new(StringComparer.Ordinal);

    /// <summary>Registers content under a key.</summary>
    /// <param name="uri">The resolved URI, or the reference when no URI is resolved.</param>
    /// <param name="content">The content to serve.</param>
    /// <returns>This resolver.</returns>
    public InMemoryResourceResolver Add(string uri, byte[] content)
    {
        _resources[uri] = content;
        return this;
    }

    /// <inheritdoc />
    public ResourceResult Resolve(in ResourceRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var key = request.ResolvedUri ?? request.Reference;
        return _resources.TryGetValue(key, out var content)
            ? ResourceResult.Found(content, key)
            : ResourceResult.NotFound($"No in-memory resource registered for '{key}'.");
    }
}
