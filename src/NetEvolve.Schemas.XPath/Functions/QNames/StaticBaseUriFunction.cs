namespace NetEvolve.Schemas.XPath.Functions.QNames;

using System;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// <c>fn:static-base-uri() as xs:anyURI?</c> (Functions and Operators 1.0, 8.2.2): the static base URI that the function holds, the
/// empty sequence when there is none.
/// </summary>
/// <remarks>
/// Limitation: a call reaches the evaluation context only, which carries no static context, so the function holds the base URI itself, as
/// <c>fn:compare</c> does. The one in the default catalog holds none and gives the empty sequence; a host that wants a static base URI
/// registers a function built over it (the layering of the function registry lets it override this one). Wiring the static context to a
/// call is #752 and, for the base URI, #806. A relative URI is no static base URI.
/// </remarks>
internal sealed class StaticBaseUriFunction : QNameFunction
{
    private readonly string? _baseUri;

    /// <summary>Initializes a new instance of the <see cref="StaticBaseUriFunction"/> class without a static base URI.</summary>
    public StaticBaseUriFunction()
        : this(null) { }

    /// <summary>Initializes a new instance of the <see cref="StaticBaseUriFunction"/> class.</summary>
    /// <param name="staticBaseUri">The static base URI, <see langword="null"/> for none; a relative URI counts as none.</param>
    public StaticBaseUriFunction(Uri? staticBaseUri)
        : base("static-base-uri", 0, 0, []) =>
        _baseUri = staticBaseUri is { IsAbsoluteUri: true } ? staticBaseUri.OriginalString : null;

    /// <inheritdoc />
    public override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation)
    {
        if (_baseUri is not null)
        {
            invocation.Result = XdmSequence.Create(XdmItem.FromAnyUri(_baseUri));
        }

        return XPathEvaluationStatus.Ok;
    }
}
