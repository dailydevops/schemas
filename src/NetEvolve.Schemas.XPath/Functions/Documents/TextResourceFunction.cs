namespace NetEvolve.Schemas.XPath.Functions.Documents;

using System;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Functions.Binding;
using NetEvolve.Schemas.XPath.Syntax.Tree;

/// <summary>
/// The base of <c>fn:unparsed-text</c>, <c>fn:unparsed-text-available</c>, and <c>fn:unparsed-text-lines</c> (Functions and Operators 3.1,
/// 14.9; XSLT 2.0, 16.2): the arguments <c>$href as xs:string?</c> and <c>$encoding as xs:string</c> and the one way to get the decoded
/// text of a call through the resolver of the evaluation context and the pool.
/// </summary>
/// <remarks>
/// Limitation: the functions hold the static base URI like <c>fn:doc</c> does (<see cref="DocumentFunction"/>). The text of a resource
/// is pooled with the context by URI and encoding, so the three functions agree on one resource within an evaluation.
/// </remarks>
internal abstract class TextResourceFunction : DocumentFunction
{
    /// <summary>Initializes a new instance of the <see cref="TextResourceFunction"/> class.</summary>
    /// <param name="localName">The local name.</param>
    /// <param name="staticBaseUri">The static base URI, <see langword="null"/> for none; a relative URI counts as none.</param>
    /// <exception cref="ArgumentNullException"><paramref name="localName"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="localName"/> is empty.</exception>
    protected TextResourceFunction(string localName, Uri? staticBaseUri)
        : base(
            localName,
            1,
            2,
            [OptionalString, new XPathParameterType(XPathItemType.String, Occurrence.ExactlyOne)],
            staticBaseUri,
            null
        ) { }

    /// <summary>Gets the text of a call whose first argument is not the empty sequence.</summary>
    /// <param name="invocation">The call.</param>
    /// <param name="text">The text when the result is <see langword="true"/>.</param>
    /// <param name="error">The error code of the text functions when the result is <see langword="false"/>.</param>
    /// <returns><see langword="true"/> when the resource is retrieved and decodes into XML characters.</returns>
    protected bool TryLoadText(ref XPathFunctionCall invocation, out string text, out XPathErrorCode error)
    {
        var arguments = invocation.Arguments;
        var reference = Text(arguments[0]);
        var encoding = arguments.Length > 1 ? Text(arguments[1]) : null;
        text = string.Empty;
        if (
            ResourceUris.Resolve(reference, StaticBaseUri, out var absolute) != ResourceUriStatus.Resolved
            || invocation.Context.ResourceResolver is not { } resolver
        )
        {
            error = DocumentErrors.FOUT1170;
            return false;
        }

        return DocumentPool
            .For(invocation.Context)
            .TryGetText(resolver, reference, StaticBaseUri, absolute!, encoding, out text, out error);
    }
}
