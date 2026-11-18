namespace NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Contracts.Fakes;

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using NetEvolve.Schemas.Resources;

/// <summary>A fake core resolver that records every request and returns one scripted document or a miss.</summary>
internal sealed class FakeDtdResolver : IResourceResolver
{
    private readonly byte[]? _content;

    internal FakeDtdResolver(string? content = "<!ELEMENT a EMPTY>") =>
        _content = content is null ? null : Encoding.UTF8.GetBytes(content);

    /// <summary>Gets the requests received, in order.</summary>
    internal List<ResourceKind> Requests { get; } = [];

    public ResourceResult Resolve(in ResourceRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Requests.Add(request.Kind);

        return _content is null
            ? ResourceResult.NotFound("no such resource")
            : ResourceResult.Found(new ReadOnlyMemory<byte>(_content), request.ResolvedUri ?? request.Reference);
    }
}
