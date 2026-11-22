namespace NetEvolve.Schemas.XPath.Functions.Documents;

using System;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Resources.Default;
using NetEvolve.Schemas.XPath.Functions.QNames;

/// <summary>
/// Turns the argument of <c>fn:doc</c>, <c>fn:doc-available</c>, and the <c>unparsed-text</c> functions into the absolute URI that the
/// resolver and the document pool see: the reference against the static base URI that the function holds, by RFC 3986, 5.2.
/// </summary>
/// <remarks>
/// Limitation: a character that no URI may contain (control characters, space, and the ASCII characters that RFC 3986 excludes) and a
/// fragment identifier make the reference invalid, as for <c>fn:resolve-uri</c> (#270): the escaping of the core would otherwise hide
/// them. A reference that is relative against no absolute base is a failure of its own, so that no resolver sees a URI that is not absolute.
/// </remarks>
internal static class ResourceUris
{
    /// <summary>Resolves a reference.</summary>
    /// <param name="reference">The reference as written.</param>
    /// <param name="baseUri">The static base URI, <see langword="null"/> for none.</param>
    /// <param name="absolute">The absolute URI when the result is <see cref="ResourceUriStatus.Resolved"/>.</param>
    /// <returns>The status.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="reference"/> is <see langword="null"/>.</exception>
    internal static ResourceUriStatus Resolve(string reference, string? baseUri, out string? absolute)
    {
        Argument.ThrowIfNull(reference);

        absolute = null;
        if (HasForbiddenCharacter(reference) || reference.AsSpan().IndexOf('#') >= 0)
        {
            return ResourceUriStatus.Invalid;
        }

        var resolution = UriReference.Resolve(reference, baseUri);
        if (resolution.Uri is null)
        {
            return resolution.Code == CoreDiagnosticCodes.ResourceBaseUriMissing
                ? ResourceUriStatus.NoBase
                : ResourceUriStatus.Invalid;
        }

        absolute = resolution.Uri;
        return ResourceUriStatus.Resolved;
    }

    /// <summary>Converts a static base URI to the form the functions hold: the original string of an absolute URI, otherwise none.</summary>
    /// <param name="staticBaseUri">The static base URI, <see langword="null"/> for none.</param>
    /// <returns>The base URI text, <see langword="null"/> for none; a relative URI counts as none.</returns>
    internal static string? BaseOf(Uri? staticBaseUri) =>
        staticBaseUri is { IsAbsoluteUri: true } ? staticBaseUri.OriginalString : null;

    // Only the ASCII characters, DEL included: the others are escaped by the resolution.
    private static bool HasForbiddenCharacter(string text)
    {
        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] < 0x80 && UriEscaping.IsForbiddenInUri(text[i]))
            {
                return true;
            }
        }

        return false;
    }
}
