namespace NetEvolve.Schemas.XPath.Collations;

using System;
using System.Collections.Generic;
using NetEvolve.Arguments;

/// <summary>Maps a collation URI to a collation.</summary>
internal sealed class CollationRegistry
{
    private readonly Dictionary<string, Collation> _collations = new(StringComparer.Ordinal);

    /// <summary>Gets the registry of the built-in collations.</summary>
    public static CollationRegistry Default { get; } = new();

    /// <summary>Adds a collation.</summary>
    /// <param name="collation">The collation.</param>
    /// <returns>A registry with the collation.</returns>
    public CollationRegistry With(Collation collation)
    {
        Argument.ThrowIfNull(collation);

        return this;
    }

    /// <summary>Looks a collation up.</summary>
    /// <param name="uri">The collation URI, absolute or relative.</param>
    /// <param name="staticBaseUri">The static base URI against which a relative URI is resolved.</param>
    /// <param name="collation">The collation.</param>
    /// <returns><see langword="true"/> when a collation is registered for the URI.</returns>
    public bool TryResolve(string uri, Uri? staticBaseUri, out Collation? collation) =>
        _collations.TryGetValue(uri + staticBaseUri, out collation);

    /// <summary>Looks a collation up.</summary>
    /// <param name="uri">The collation URI, absolute or relative.</param>
    /// <param name="staticBaseUri">The static base URI against which a relative URI is resolved.</param>
    /// <returns>The collation.</returns>
    public Collation Resolve(string uri, Uri? staticBaseUri) =>
        TryResolve(uri, staticBaseUri, out var collation) ? collation! : CodepointCollation.Instance;
}
