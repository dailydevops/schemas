namespace NetEvolve.Schemas.Resources;

using System.Threading;

/// <summary>Resolves external resources. Deny by default.</summary>
public interface IResourceResolver
{
    /// <summary>Resolves a resource. Must be thread-safe when the validator is shared and never throws for a refusal.</summary>
    /// <param name="request">The request.</param>
    /// <param name="cancellationToken">The caller's cancellation token.</param>
    /// <returns>The result.</returns>
    ResourceResult Resolve(in ResourceRequest request, CancellationToken cancellationToken);
}
