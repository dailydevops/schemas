#pragma warning disable CA1054 // Approved shape (#529): URIs are passed as strings, like ResourceRequest.

namespace NetEvolve.Schemas.Formats;

using System;
using System.Threading;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Resources;
using NetEvolve.Schemas.Resources.Default;

/// <summary>
/// Loads an external resource for a format: resolves the reference against its base, asks the <see cref="IResourceResolver"/>, caps the
/// size at <see cref="ValidationLimits.MaxResourceSize"/>, and maps the outcome to NES0006, NES0007, and NES0100 to NES0103.
/// </summary>
/// <remarks>
/// One instance per build or run, not thread-safe: it counts. Every <see cref="Load"/> counts against
/// <see cref="ValidationLimits.MaxExternalResources"/> before anything else happens, so a refused or missing resource counts, and nothing
/// is cached or de-duplicated. The resolver contract and the <see cref="ResourceRequest.ResolvedUri"/> are unchanged. Limitation: the
/// resolver is synchronous, so there is no asynchronous load.
/// </remarks>
public sealed class ResourceLoader
{
    private readonly IResourceResolver _resolver;
    private readonly ValidationLimits _limits;

    /// <summary>Initializes a new instance of the <see cref="ResourceLoader"/> class.</summary>
    /// <param name="resolver">The resolver that finds the resources.</param>
    /// <param name="limits">The limits that cap the number and the size of the resources.</param>
    /// <exception cref="ArgumentNullException"><paramref name="resolver"/> or <paramref name="limits"/> is <see langword="null"/>.</exception>
    public ResourceLoader(IResourceResolver resolver, ValidationLimits limits)
    {
        ArgumentNullException.ThrowIfNull(resolver);
        ArgumentNullException.ThrowIfNull(limits);

        _resolver = resolver;
        _limits = limits;
    }

    /// <summary>
    /// Gets a value indicating whether a relative reference without an absolute base is passed to the resolver with
    /// <see cref="ResourceRequest.ResolvedUri"/> set to <see langword="null"/> instead of failing with NES0101. Xsd sets it; the default keeps Schematron.
    /// </summary>
    internal bool PassMissingBaseToResolver { get; set; }

    /// <summary>Gets the number of requests counted so far, refused and missing ones included.</summary>
    public int RequestCount { get; private set; }

    /// <summary>Resolves a reference against the base and reads the resource under the size cap.</summary>
    /// <param name="kind">The kind of resource.</param>
    /// <param name="reference">The reference as written.</param>
    /// <param name="baseUri">The base URI of the referencing document, or <see langword="null"/>.</param>
    /// <param name="cancellationToken">The token passed to the resolver.</param>
    /// <param name="publicId">The public identifier, or <see langword="null"/>.</param>
    /// <param name="namespaceUri">The namespace of an import, or <see langword="null"/>.</param>
    /// <returns>The result: the content, a recoverable failure, or a breached limit.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="reference"/> is <see langword="null"/>.</exception>
    /// <exception cref="OperationCanceledException">The token is canceled.</exception>
    public ResourceLoadResult Load(
        ResourceKind kind,
        string reference,
        string? baseUri,
        CancellationToken cancellationToken,
        string? publicId = null,
        string? namespaceUri = null
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(reference);

        if (RequestCount >= _limits.MaxExternalResources)
        {
            return Fail(
                ResourceLoadStatus.LimitExceeded,
                CoreDiagnosticCodes.ExternalResourceLimitExceeded,
                "More than " + _limits.MaxExternalResources + " external resources are referenced."
            );
        }

        RequestCount++;
        var resolution = UriReference.Resolve(reference, baseUri);
        if (
            resolution.Uri is null
            && !(PassMissingBaseToResolver && resolution.Code == CoreDiagnosticCodes.ResourceBaseUriMissing)
        )
        {
            return Fail(
                ResourceLoadStatus.Failed,
                resolution.Code,
                resolution.Code == CoreDiagnosticCodes.ResourceBaseUriMissing
                    ? "The reference '" + reference + "' is relative and the base URI is not absolute."
                    : "The reference '" + reference + "' is not a valid URI reference."
            );
        }

        var request = new ResourceRequest(kind, reference, baseUri, resolution.Uri, publicId, namespaceUri);
        using var result = _resolver.Resolve(in request, cancellationToken);
        if (result.Status != ResourceStatus.Found)
        {
            var notFound = result.Status == ResourceStatus.NotFound;
            return Fail(
                ResourceLoadStatus.Failed,
                notFound ? CoreDiagnosticCodes.ResourceNotFound : CoreDiagnosticCodes.ResourceDenied,
                result.Message
                    ?? "The resource '"
                        + (resolution.Uri ?? reference)
                        + (notFound ? "' was not found." : "' was denied.")
            );
        }

        var systemId = result.SystemId ?? resolution.Uri;
        return ResourceContent.TryRead(in result, _limits.MaxResourceSize, out var content)
            ? new ResourceLoadResult(ResourceLoadStatus.Loaded, content, systemId, default, null)
            : Fail(
                ResourceLoadStatus.LimitExceeded,
                CoreDiagnosticCodes.ResourceSizeLimitExceeded,
                "The resource '" + (systemId ?? reference) + "' is larger than " + _limits.MaxResourceSize + " bytes.",
                systemId
            );
    }

    private static ResourceLoadResult Fail(
        ResourceLoadStatus status,
        DiagnosticCode code,
        string message,
        string? systemId = null
    ) => new(status, default, systemId, code, message);
}
