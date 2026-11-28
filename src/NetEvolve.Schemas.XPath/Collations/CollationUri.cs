namespace NetEvolve.Schemas.XPath.Collations;

using System;

/// <summary>
/// Tells an absolute collation URI from a relative one and resolves a relative one against the static base URI. Absoluteness is the
/// scheme of RFC 3986 (letter, then letters, digits, plus, minus, or dot, then a colon), not <see cref="Uri.TryCreate(string, UriKind, out Uri)"/>:
/// on some platforms that method takes <c>/path</c> for an absolute file URI, and a collation lookup must not depend on the platform.
/// </summary>
internal static class CollationUri
{
    /// <summary>Tests whether a URI reference starts with a scheme.</summary>
    /// <param name="uri">The URI reference.</param>
    /// <returns><see langword="true"/> when the reference is absolute.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="uri"/> is <see langword="null"/>.</exception>
    public static bool IsAbsolute(string uri)
    {
        ArgumentNullException.ThrowIfNull(uri);

        if (uri.Length == 0 || !IsAsciiLetter(uri[0]))
        {
            return false;
        }

        for (var i = 1; i < uri.Length; i++)
        {
            var c = uri[i];
            if (c == ':')
            {
                return true;
            }

            if (!IsAsciiLetter(c) && !(c >= '0' && c <= '9') && c != '+' && c != '-' && c != '.')
            {
                return false;
            }
        }

        return false;
    }

    /// <summary>Resolves a relative URI reference against the static base URI.</summary>
    /// <param name="reference">The relative URI reference.</param>
    /// <param name="staticBaseUri">The static base URI.</param>
    /// <param name="absolute">The absolute URI.</param>
    /// <returns><see langword="false"/> when there is no absolute base URI or the reference cannot be resolved.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="reference"/> is <see langword="null"/>.</exception>
    public static bool TryResolve(string reference, Uri? staticBaseUri, out string absolute)
    {
        ArgumentNullException.ThrowIfNull(reference);

        absolute = string.Empty;
        if (
            staticBaseUri is null
            || !staticBaseUri.IsAbsoluteUri
            || !Uri.TryCreate(staticBaseUri, reference, out var resolved)
        )
        {
            return false;
        }

        absolute = resolved.AbsoluteUri;
        return true;
    }

    private static bool IsAsciiLetter(char c) => (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');
}
