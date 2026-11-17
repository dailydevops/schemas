namespace NetEvolve.Schemas.Resources;

using System.Threading;

/// <summary>Resolver that denies every request and never opens anything.</summary>
/// <remarks>
/// The denial message carries <see cref="CoreDiagnosticCodes.ResourceDenied"/>, the kind, the requested URI
/// (the resolved URI, or the reference as written without one) and the base URI.
/// </remarks>
public sealed class DenyAllResourceResolver : IResourceResolver
{
    private DenyAllResourceResolver() { }

    /// <summary>Gets the shared instance.</summary>
    public static DenyAllResourceResolver Instance { get; } = new();

    /// <inheritdoc />
    public ResourceResult Resolve(in ResourceRequest request, CancellationToken cancellationToken) =>
        ResourceResult.Denied(
            $"{CoreDiagnosticCodes.ResourceDenied}: {request.Kind} resource '{request.ResolvedUri ?? request.Reference}' was denied (base URI '{request.BaseUri ?? "(none)"}')."
        );
}
