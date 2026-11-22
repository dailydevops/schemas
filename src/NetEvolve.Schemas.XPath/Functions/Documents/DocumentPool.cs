namespace NetEvolve.Schemas.XPath.Functions.Documents;

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Resources;
using NetEvolve.Schemas.Resources.Default;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Model;
using NetEvolve.Schemas.Xml.Model.Loading;
using NetEvolve.Schemas.XPath.Abstractions;
using NetEvolve.Schemas.XPath.Context;

/// <summary>
/// The document pool of one evaluation context (XPath 2.0, 2.1.2, available documents; Functions and Operators 1.0, 15.5.4): the same
/// absolute URI yields the identical document node, so <c>fn:doc</c> is stable and the resolver is asked once per URI. A load that fails
/// is remembered too, so <c>fn:doc-available</c> and <c>fn:doc</c> agree. The pool also counts every resolver request of the context
/// (documents and texts) against <see cref="ValidationLimits.MaxExternalResources"/>.
/// </summary>
/// <remarks>
/// Limitations: the pool is kept beside the context, not in it, because the context is outside the work package; it lives exactly as
/// long as the context and survives <see cref="XPathEvaluationContext.Reset"/>. The size of a resource and the number of requests
/// follow <see cref="ValidationLimits.Default"/> because the evaluation limits carry neither. The documents are loaded with the reader
/// options of the function that asks first. Not thread-safe, like the context.
/// </remarks>
internal sealed class DocumentPool
{
    private static readonly ConditionalWeakTable<XPathEvaluationContext, DocumentPool> Pools = new();

    private readonly Dictionary<string, XmlDocumentTree?> _documents = new(StringComparer.Ordinal);
    private int _requests;

    private DocumentPool() { }

    /// <summary>Gets the pool of a context, creating it on first use.</summary>
    /// <param name="context">The evaluation context.</param>
    /// <returns>The pool.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> is <see langword="null"/>.</exception>
    internal static DocumentPool For(XPathEvaluationContext context)
    {
        Argument.ThrowIfNull(context);

        return Pools.GetValue(context, static _ => new DocumentPool());
    }

    /// <summary>Counts one resolver request against the limit of external resources.</summary>
    /// <returns><see langword="false"/> when the limit is reached; the request must then not be made.</returns>
    internal bool TryReserveRequest()
    {
        if (_requests >= ValidationLimits.Default.MaxExternalResources)
        {
            return false;
        }

        _requests++;
        return true;
    }

    /// <summary>Gets the document of an absolute URI, loading it through the resolver on first use.</summary>
    /// <param name="resolver">The resolver of the context.</param>
    /// <param name="reference">The reference as written.</param>
    /// <param name="baseUri">The base URI that the reference was resolved against, <see langword="null"/> for none.</param>
    /// <param name="absolute">The absolute URI.</param>
    /// <param name="options">The reader options.</param>
    /// <returns>The document, <see langword="null"/> when the resource is denied, missing, too large, or not well formed.</returns>
    /// <exception cref="ArgumentNullException">An argument is <see langword="null"/>.</exception>
    internal XmlDocumentTree? GetDocument(
        IXPathResourceResolver resolver,
        string reference,
        string? baseUri,
        string absolute,
        XmlReaderOptions options
    )
    {
        Argument.ThrowIfNull(resolver);
        Argument.ThrowIfNull(reference);
        Argument.ThrowIfNull(absolute);
        Argument.ThrowIfNull(options);

        if (_documents.TryGetValue(absolute, out var pooled))
        {
            return pooled;
        }

        if (!TryReserveRequest())
        {
            return null;
        }

        var loaded = Load(resolver, reference, baseUri, absolute, options);
        _documents[absolute] = loaded;
        return loaded;
    }

    private static XmlDocumentTree? Load(
        IXPathResourceResolver resolver,
        string reference,
        string? baseUri,
        string absolute,
        XmlReaderOptions options
    )
    {
        var request = new ResourceRequest(ResourceKind.XPathDocument, reference, baseUri, absolute);
        using var result = resolver.Resolve(in request, CancellationToken.None);
        if (
            result.Status != ResourceStatus.Found
            || !ResourceContent.TryRead(in result, ValidationLimits.Default.MaxResourceSize, out var content)
        )
        {
            return null;
        }

        var documentUri = Uri.TryCreate(result.SystemId ?? absolute, UriKind.Absolute, out var uri) ? uri : null;
        try
        {
            return XmlDocumentLoader.LoadFromMemory(content, options, documentUri);
        }
        catch (XmlParseException)
        {
            return null;
        }
    }
}
