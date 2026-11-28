namespace NetEvolve.Schemas.XPath.Functions.QNames;

using System;
using NetEvolve.Schemas.Resources.Default;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// <c>fn:resolve-uri($relative as xs:string?[, $base as xs:string]) as xs:anyURI?</c> (Functions and Operators 1.0, 8.1): the reference
/// against the base URI, by the resolution of RFC 3986, 5.2, the one-argument form against the static base URI that the function holds.
/// The empty sequence gives the empty sequence. A base that is no valid URI (a fragment or a character that no URI may contain) is
/// refused first, also for an absolute reference; an absolute reference is then returned as it is, and a relative one needs an absolute
/// base.
/// </summary>
/// <remarks>
/// <para>
/// Errors: FORG0002 for a reference or a base that is no URI reference, and for a relative reference against a base that is not an
/// absolute URI; FONS0005 for the one-argument form of a relative reference when the function holds no static base URI.
/// </para>
/// <para>
/// Limitations (#270, decisions): a character that no URI may contain (control characters, space, <c>&lt;&gt;"{}|\^`</c>) is FORG0002,
/// because the XML escaping of <c>UriReference</c> would turn it into <c>%20</c> and the like and change the string the caller wrote; a
/// character beyond ASCII is escaped as UTF-8 octets, as <c>fn:iri-to-uri</c> does, because RFC 3986 knows no such character. The static
/// base URI is held by the function like the one of <see cref="StaticBaseUriFunction"/>; the one in the default catalog holds none, and the
/// function registry binds a copy that holds the static base URI of the static context (#806).
/// </para>
/// </remarks>
internal sealed class ResolveUriFunction : QNameFunction
{
    private readonly string? _staticBaseUri;

    /// <summary>Initializes a new instance of the <see cref="ResolveUriFunction"/> class without a static base URI.</summary>
    public ResolveUriFunction()
        : this(null) { }

    /// <summary>Initializes a new instance of the <see cref="ResolveUriFunction"/> class.</summary>
    /// <param name="staticBaseUri">The static base URI, <see langword="null"/> for none; a relative URI counts as none.</param>
    public ResolveUriFunction(Uri? staticBaseUri)
        : base("resolve-uri", 1, 2, [OptionalString, OneString]) =>
        _staticBaseUri = staticBaseUri is { IsAbsoluteUri: true } ? staticBaseUri.OriginalString : null;

    /// <inheritdoc />
    /// <exception cref="XPathException">A reference or a base is no URI (FORG0002), or the static base URI is missing (FONS0005).</exception>
    public override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation)
    {
        var arguments = invocation.Arguments;
        if (arguments[0].IsEmpty)
        {
            return XPathEvaluationStatus.Ok;
        }

        var explicitBase = arguments.Length > 1;
        var baseUri = explicitBase ? Text(arguments[1]) : _staticBaseUri;
        var relative = Text(arguments[0]);
        if (HasForbiddenCharacter(relative) || (baseUri is not null && IsInvalidBase(baseUri)))
        {
            throw XPathErrors.Exception(XPathErrorCodes.FORG0002);
        }

        var resolution = UriReference.Resolve(relative, baseUri);
        if (resolution.Uri is null)
        {
            throw XPathErrors.Exception(
                baseUri is null && resolution.Code == CoreDiagnosticCodes.ResourceBaseUriMissing
                    ? XPathErrorCodes.FONS0005
                    : XPathErrorCodes.FORG0002
            );
        }

        invocation.Result = XdmSequence.Create(XdmItem.FromAnyUri(resolution.Uri));
        return XPathEvaluationStatus.Ok;
    }

    // A base with a fragment is no base URI (RFC 3986, 5.1); the other base errors are the ones of the resolution.
    private static bool IsInvalidBase(string baseUri) =>
        baseUri.AsSpan().IndexOf('#') >= 0 || HasForbiddenCharacter(baseUri);

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
