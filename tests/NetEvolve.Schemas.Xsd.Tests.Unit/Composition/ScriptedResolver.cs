namespace NetEvolve.Schemas.Xsd.Tests.Unit.Composition;

using System;
using System.Threading;
using NetEvolve.Schemas.Resources;

/// <summary>A resolver whose answer is a function of the request.</summary>
internal sealed class ScriptedResolver(Func<ResourceRequest, ResourceResult> answer) : IResourceResolver
{
    public int Calls { get; private set; }

    public ResourceResult Resolve(in ResourceRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        Calls++;
        return answer(request);
    }
}
