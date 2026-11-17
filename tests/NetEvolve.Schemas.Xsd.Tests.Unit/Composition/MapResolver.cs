namespace NetEvolve.Schemas.Xsd.Tests.Unit.Composition;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using NetEvolve.Schemas.Resources;

/// <summary>A resolver over an in-memory map that records every request.</summary>
internal sealed class MapResolver : IResourceResolver
{
    private readonly Dictionary<string, string> _documents = new(StringComparer.Ordinal);

    public List<string> Requests { get; } = [];

    public List<ResourceRequest> Calls { get; } = [];

    public MapResolver Add(string uri, string text)
    {
        _documents[uri] = text;
        return this;
    }

    public ResourceResult Resolve(in ResourceRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        Requests.Add(request.ResolvedUri ?? request.Reference);
        Calls.Add(request);

        return request.ResolvedUri is not null && _documents.TryGetValue(request.ResolvedUri, out var text)
            ? ResourceResult.Found(new MemoryStream(Encoding.UTF8.GetBytes(text)), request.ResolvedUri)
            : ResourceResult.NotFound("missing");
    }
}
