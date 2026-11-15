#pragma warning disable CA1065, MA0025 // Contract stub: the owning work package replaces the throwing bodies.

namespace NetEvolve.Schemas.Resources;

using System.Threading;

/// <summary>Resolver that denies every request and never opens anything.</summary>
/// <remarks>Limitation: members throw until FND-10 lands.</remarks>
public sealed class DenyAllResourceResolver : IResourceResolver
{
    private DenyAllResourceResolver() { }

    /// <summary>Gets the shared instance.</summary>
    public static DenyAllResourceResolver Instance => throw Stub.For("FND-10");

    /// <inheritdoc />
    public ResourceResult Resolve(in ResourceRequest request, CancellationToken cancellationToken) =>
        throw Stub.For("FND-10");
}
