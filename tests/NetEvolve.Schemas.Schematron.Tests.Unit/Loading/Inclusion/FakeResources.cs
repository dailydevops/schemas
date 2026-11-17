namespace NetEvolve.Schemas.Schematron.Tests.Unit.Loading.Inclusion;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using NetEvolve.Schemas.Resources;

/// <summary>A fake core resolver over documents held in memory; it records every request.</summary>
internal sealed class FakeResources : IResourceResolver
{
    private readonly Dictionary<string, byte[]> _files = new(StringComparer.Ordinal);

    internal List<string> Requests { get; } = [];

    /// <summary>Gets or sets a value indicating whether found documents are returned as streams instead of memory.</summary>
    internal bool AsStream { get; set; }

    /// <summary>Gets or sets a value indicating whether streams cannot seek, so the size is only known by reading.</summary>
    internal bool ForwardOnly { get; set; }

    internal FakeResources Add(string uri, string xml)
    {
        _files[uri] = Encoding.UTF8.GetBytes(xml);
        return this;
    }

    public ResourceResult Resolve(in ResourceRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var uri = request.ResolvedUri ?? request.Reference;
        Requests.Add(uri);
        if (!_files.TryGetValue(uri, out var bytes))
        {
            return ResourceResult.NotFound("no document " + uri);
        }

        return AsStream ? Open(bytes, uri) : ResourceResult.Found(new ReadOnlyMemory<byte>(bytes), uri);
    }

    private ResourceResult Open(byte[] bytes, string uri)
    {
        Stream? stream = ForwardOnly ? new OneWayStream(bytes) : new MemoryStream(bytes);
        try
        {
            var result = ResourceResult.Found(stream, uri);
            stream = null;
            return result;
        }
        finally
        {
            stream?.Dispose();
        }
    }
}
