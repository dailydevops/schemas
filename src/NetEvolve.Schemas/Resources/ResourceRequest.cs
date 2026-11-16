#pragma warning disable CA1054, CA1056, CA1815 // Approved shape (#103): URIs are passed as strings and the struct defines no value equality.

namespace NetEvolve.Schemas.Resources;

/// <summary>A request for an external resource.</summary>
/// <remarks>Limitation: argument validation and the empty-string default of <see cref="Reference"/> arrive with FND-10.</remarks>
public readonly struct ResourceRequest
{
    /// <summary>Initializes a new instance of the <see cref="ResourceRequest"/> struct.</summary>
    /// <param name="kind">The kind of resource.</param>
    /// <param name="reference">The reference as written.</param>
    /// <param name="baseUri">The base URI, or <see langword="null"/>.</param>
    /// <param name="resolvedUri">The resolved absolute URI, or <see langword="null"/> without an absolute base.</param>
    /// <param name="publicId">The public identifier, or <see langword="null"/>.</param>
    /// <param name="namespaceUri">The namespace of an import, or <see langword="null"/>.</param>
    public ResourceRequest(
        ResourceKind kind,
        string reference,
        string? baseUri,
        string? resolvedUri,
        string? publicId = null,
        string? namespaceUri = null
    )
    {
        Kind = kind;
        Reference = reference;
        BaseUri = baseUri;
        ResolvedUri = resolvedUri;
        PublicId = publicId;
        NamespaceUri = namespaceUri;
    }

    /// <summary>Gets the kind of resource.</summary>
    public ResourceKind Kind { get; }

    /// <summary>Gets the reference as written.</summary>
    public string Reference { get; }

    /// <summary>Gets the base URI.</summary>
    public string? BaseUri { get; }

    /// <summary>Gets the resolved absolute URI.</summary>
    public string? ResolvedUri { get; }

    /// <summary>Gets the public identifier.</summary>
    public string? PublicId { get; }

    /// <summary>Gets the namespace of an import.</summary>
    public string? NamespaceUri { get; }
}
