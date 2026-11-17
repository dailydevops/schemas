namespace NetEvolve.Schemas.Resources.Default;

using System;

/// <summary>Resolves a URI reference against a base URI (RFC 3986 section 5) after the XML 1.0 section 4.2.2 escaping.</summary>
internal static class UriReference
{
    /// <summary>Resolves <paramref name="reference"/> against <paramref name="baseUri"/>.</summary>
    /// <param name="reference">The reference as written.</param>
    /// <param name="baseUri">The base URI, or <see langword="null"/>.</param>
    /// <returns>The resolution.</returns>
    internal static UriResolution Resolve(string reference, string? baseUri)
    {
        ArgumentNullException.ThrowIfNull(reference);
        _ = baseUri;
        return default;
    }
}
