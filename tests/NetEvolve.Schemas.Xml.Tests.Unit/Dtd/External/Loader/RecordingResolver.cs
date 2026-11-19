namespace NetEvolve.Schemas.Xml.Tests.Unit.Dtd.External.Loader;

using System;
using System.Collections.Generic;
using System.Threading;
using NetEvolve.Schemas.Resources;

/// <summary>A core resolver that records every full request and serves scripted bytes by resolved URI (or by reference when there is none).</summary>
internal sealed class RecordingResolver : IResourceResolver
{
    private readonly Dictionary<string, Entry> _entries = new(StringComparer.Ordinal);

    private sealed record Entry(byte[] Content, string? FinalUri, int MaxRead, bool AsMemory, bool FailOnRead);

    /// <summary>Gets the requests received, in order.</summary>
    internal List<ResourceRequest> Requests { get; } = [];

    /// <summary>Gets the streams handed out, in order.</summary>
    internal List<TrackingStream> Streams { get; } = [];

    /// <summary>Scripts a resource.</summary>
    internal RecordingResolver Add(
        string uri,
        byte[] content,
        string? finalUri = null,
        int maxRead = int.MaxValue,
        bool asMemory = false,
        bool failOnRead = false
    )
    {
        _entries[uri] = new Entry(content, finalUri, maxRead, asMemory, failOnRead);

        return this;
    }

    public ResourceResult Resolve(in ResourceRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Requests.Add(request);

        if (!_entries.TryGetValue(request.ResolvedUri ?? request.Reference, out var entry))
        {
            return ResourceResult.NotFound("no such resource");
        }

        if (entry.AsMemory)
        {
            return ResourceResult.Found(new ReadOnlyMemory<byte>(entry.Content), entry.FinalUri);
        }

        var stream = new TrackingStream(entry.Content, entry.MaxRead) { FailOnRead = entry.FailOnRead };
        Streams.Add(stream);

        return ResourceResult.Found(stream, entry.FinalUri);
    }
}
