#pragma warning disable CA1054, CA1056, CA1815 // Approved shape (#103): URIs are passed as strings and the struct defines no value equality.

namespace NetEvolve.Schemas.Resources;

using System;
using NetEvolve.Arguments;

/// <summary>A request for an external resource.</summary>
public readonly struct ResourceRequest
{
    private readonly string? _reference;

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
        Argument.ThrowIfNull(reference);
        Argument.ThrowIfGreaterThan((byte)kind, (byte)ResourceKind.XPathText);

        Kind = kind;
        _reference = reference;
        BaseUri = baseUri;
        ResolvedUri = resolvedUri;
        PublicId = publicId;
        NamespaceUri = namespaceUri;
    }

    /// <summary>Gets the kind of resource.</summary>
    public ResourceKind Kind { get; }

    /// <summary>Gets the reference as written, never <see langword="null"/>; <see langword="default"/> gives an empty string.</summary>
    public string Reference => _reference ?? string.Empty;

    /// <summary>Gets the base URI.</summary>
    public string? BaseUri { get; }

    /// <summary>Gets the resolved absolute URI.</summary>
    public string? ResolvedUri { get; }

    /// <summary>Gets the public identifier.</summary>
    public string? PublicId { get; }

    /// <summary>Gets the namespace of an import.</summary>
    public string? NamespaceUri { get; }
}
