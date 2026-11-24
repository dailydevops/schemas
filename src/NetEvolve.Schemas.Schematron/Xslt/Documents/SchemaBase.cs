namespace NetEvolve.Schemas.Schematron.Xslt.Documents;

using System;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Resources.Default;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Functions.Binding;

/// <summary>
/// Resolves a reference against a base URI (RFC 3986, 5.2) and finds the functions of the XPath package that the document functions of
/// this folder delegate to. The catalog functions are reached through the registry, so every load goes through the pool and the resolver
/// of the evaluation context and the pool is the one <c>fn:doc</c> uses.
/// </summary>
/// <remarks>
/// Limitation: a reference that holds a fragment identifier or a character that no URI may contain is passed on as written, so the
/// catalog function rejects it as it does for <c>fn:doc</c>; the escaping of the core would otherwise hide the character.
/// </remarks>
internal static class SchemaBase
{
    private static readonly XPathStaticContext Statics = XPathStaticContext
        .Default.ToBuilder()
        .WithVersion(XPathVersion.V20)
        .Build();

    /// <summary>Gets the string form of a base URI.</summary>
    /// <param name="baseUri">The base URI, <see langword="null"/> for none.</param>
    /// <returns>The text of an absolute URI, otherwise <see langword="null"/>.</returns>
    internal static string? Text(Uri? baseUri) => baseUri is { IsAbsoluteUri: true } ? baseUri.OriginalString : null;

    /// <summary>Resolves a reference.</summary>
    /// <param name="reference">The reference as written.</param>
    /// <param name="baseUri">The base URI, <see langword="null"/> for none.</param>
    /// <returns>The absolute URI; the reference unchanged when it cannot be resolved, so that the catalog function reports it.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="reference"/> is <see langword="null"/>.</exception>
    internal static string Resolve(string reference, string? baseUri)
    {
        ArgumentNullException.ThrowIfNull(reference);

        return HasForbiddenCharacter(reference) ? reference : UriReference.Resolve(reference, baseUri).Uri ?? reference;
    }

    /// <summary>Finds a function of the default catalog.</summary>
    /// <param name="localName">The local name in the function namespace.</param>
    /// <param name="arity">The number of arguments.</param>
    /// <returns>The function.</returns>
    /// <exception cref="InvalidOperationException">The catalog holds no such function.</exception>
    internal static XPathFunction Catalog(string localName, int arity) =>
        FunctionCatalog.Default.Registry.TryResolve(
            Statics,
            new XPathQName(Statics.DefaultFunctionNamespace, localName),
            arity,
            XPathHostRequirements.Xslt20Host,
            out var function
        )
            ? function!
            : throw new InvalidOperationException($"The catalog holds no function {localName}#{arity}.");

    private static bool HasForbiddenCharacter(string text)
    {
        foreach (var c in text)
        {
            if (c is '#' or '"' or '<' or '>' or '\\' or '^' or '`' or '{' or '|' or '}' or <= ' ' or '\u007F')
            {
                return true;
            }
        }

        return false;
    }
}
