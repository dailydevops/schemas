#pragma warning disable CA1065, MA0025 // Stub per #103: throwing bodies stay until FND-09, FND-10, FND-55 and FND-56 replace them.

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
