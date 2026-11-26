namespace NetEvolve.Schemas.Tests.Unit.Formats;

using System;
using System.Collections.Generic;
using System.Threading;
using NetEvolve.Schemas.Resources;

/// <summary>A resolver that records its requests and answers with a fixed function.</summary>
internal sealed class StubResolver(Func<ResourceRequest, ResourceResult> answer) : IResourceResolver
{
    public List<ResourceRequest> Requests { get; } = [];

    public CancellationToken LastToken { get; private set; }

    public ResourceResult Resolve(in ResourceRequest request, CancellationToken cancellationToken)
    {
        Requests.Add(request);
        LastToken = cancellationToken;
        return answer(request);
    }
}
