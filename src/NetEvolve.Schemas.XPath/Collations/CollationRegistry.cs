namespace NetEvolve.Schemas.XPath.Collations;

using System;
using System.Collections.Generic;
using NetEvolve.Arguments;
using NetEvolve.Schemas.XPath.Errors;

/// <summary>
/// Maps a collation URI to a collation (Functions and Operators 1.0, 7.3): the codepoint collation is the only built-in collation, a
/// relative URI is resolved against the static base URI before the lookup, and every URI that is not registered is FOCH0002 instead of a
/// silent fallback. Immutable and thread-safe: a host adds a collation through <see cref="With"/>, which returns a new registry.
/// </summary>
/// <remarks>
/// Limitations: the lookup compares the absolute URI as a string, character by character, so the registered spelling is the one that
/// matches; a collation registered under the URI of another one replaces it (later wins, as in the layers of the function registry,
/// #168); only a caller inside the assembly, or one of the packages with <c>InternalsVisibleTo</c>, can add a collation, because the
/// public API is frozen (#168, #297).
/// </remarks>
internal sealed class CollationRegistry
{
    private readonly Dictionary<string, Collation> _collations;

    private CollationRegistry(Dictionary<string, Collation> collations) => _collations = collations;

    /// <summary>Gets the registry of the built-in collations: the codepoint collation.</summary>
    public static CollationRegistry Default { get; } =
        new(
            new Dictionary<string, Collation>(StringComparer.Ordinal)
            {
                [CodepointCollation.Instance.Uri] = CodepointCollation.Instance,
            }
        );

    /// <summary>Adds a collation under its URI.</summary>
    /// <param name="collation">The collation; one with the same URI is replaced.</param>
    /// <returns>A new registry with the collation; this registry is unchanged.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="collation"/> is <see langword="null"/>.</exception>
    public CollationRegistry With(Collation collation)
    {
        Argument.ThrowIfNull(collation);

        return new CollationRegistry(
            new Dictionary<string, Collation>(_collations, StringComparer.Ordinal) { [collation.Uri] = collation }
        );
    }

    /// <summary>Looks a collation up.</summary>
    /// <param name="uri">The collation URI, absolute or relative.</param>
    /// <param name="staticBaseUri">The static base URI against which a relative URI is resolved, <see langword="null"/> for none.</param>
    /// <param name="collation">The collation, <see langword="null"/> when none is registered.</param>
    /// <returns><see langword="true"/> when a collation is registered for the URI; a relative URI with no absolute static base URI finds none.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="uri"/> is <see langword="null"/>.</exception>
    public bool TryResolve(string uri, Uri? staticBaseUri, out Collation? collation)
    {
        Argument.ThrowIfNull(uri);

        var absolute = uri;
        if (!CollationUri.IsAbsolute(uri) && !CollationUri.TryResolve(uri, staticBaseUri, out absolute))
        {
            collation = null;
            return false;
        }

        return _collations.TryGetValue(absolute, out collation);
    }

    /// <summary>Looks a collation up and raises FOCH0002 when there is none.</summary>
    /// <param name="uri">The collation URI, absolute or relative.</param>
    /// <param name="staticBaseUri">The static base URI against which a relative URI is resolved, <see langword="null"/> for none.</param>
    /// <returns>The collation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="uri"/> is <see langword="null"/>.</exception>
    /// <exception cref="XPathException">No collation is registered for the URI (FOCH0002).</exception>
    public Collation Resolve(string uri, Uri? staticBaseUri) =>
        TryResolve(uri, staticBaseUri, out var collation)
            ? collation!
            : throw XPathErrors.Exception(XPathErrorCodes.FOCH0002);
}
