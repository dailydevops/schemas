namespace NetEvolve.Schemas.Schematron.Loading.XInclude;

using System;
using System.Threading;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Resources;
using NetEvolve.Schemas.Resources.Default;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Xml.Contracts;

/// <summary>
/// Resolves the <c>xi:include</c> elements of a schema document into one tree, reading every resource through the core resolver
/// (XML Inclusions (XInclude) Version 1.0, Second Edition; design #137 AP-22, decision OQ-5).
/// </summary>
/// <remarks>
/// <para>
/// One instance per build, not thread-safe: it counts. Every fetch counts against <see cref="ValidationLimits.MaxExternalResources"/>
/// before anything else happens, so a refused or missing resource counts, and nothing is cached or de-duplicated: as in decision
/// OQ-5 of design #137 the number of loads bounds the nesting, and the depth of the merged tree is bounded by
/// <see cref="ValidationLimits.MaxDepth"/>. The count is separate from the one of the Schematron include loader. The default
/// <see cref="DenyAllResourceResolver"/> refuses every request with <see cref="CoreDiagnosticCodes.ResourceDenied"/>.
/// </para>
/// <para>
/// Codes: a resource that is missing, refused, not well formed, not decodable, or whose reference is invalid is a resource
/// error, and an <c>xi:fallback</c> child recovers from it; without one the code of the cause is reported at the
/// <c>xi:include</c> element. A breached limit, an inclusion cycle (<see cref="SchematronDiagnosticCodes.IncludeCycle"/>), a
/// misplaced or unknown element of the XInclude namespace, and a wrong <c>parse</c> or missing <c>href</c> are fatal: the first
/// one ends the run. <c>xpointer</c> and a fragment identifier in <c>href</c> are <see cref="SchematronDiagnosticCodes.Unsupported"/>.
/// </para>
/// <para>
/// Limitations: <c>xpointer</c> is not supported (DEF-58, #70); <c>accept</c> and <c>accept-language</c> have no effect, because the
/// resolver does no content negotiation; <c>xml:base</c> and <c>xml:lang</c> of the including document are not applied (decision
/// Q10 of design #106); the elements of included content report the position of the <c>xi:include</c> element in the document
/// the schema was loaded from, because a tree has one source; namespace declarations on <c>xi:include</c> and
/// <c>xi:fallback</c> elements are not carried to fallback content; the included document is not validated and its document
/// type declaration is dropped. The tree is merged after the XInclude infoset rules except that <c>xml:base</c> is the absolute
/// system identifier of the included document. The nesting of includes is recursive, bounded by <see cref="ValidationLimits.MaxDepth"/>.
/// </para>
/// </remarks>
internal sealed class XIncludeResolver
{
    private readonly IncludeFetcher _fetcher;

    internal XIncludeResolver(IResourceResolver resolver, ValidationLimits limits)
    {
        ArgumentNullException.ThrowIfNull(resolver);
        ArgumentNullException.ThrowIfNull(limits);

        _fetcher = new IncludeFetcher(resolver, limits);
    }

    internal ValidationLimits Limits => _fetcher.Limits;

    /// <summary>Resolves the includes of a document.</summary>
    /// <param name="document">The document node.</param>
    /// <param name="systemId">The system identifier of the document, the base of its references, or <see langword="null"/>.</param>
    /// <param name="problems">Receives the first fatal error, or the resource error of an include without fallback.</param>
    /// <param name="cancellationToken">The token passed to the resolver.</param>
    /// <param name="result">The document node of the merged tree, empty when a problem was reported.</param>
    /// <returns><see langword="true"/> when no problem was reported.</returns>
    internal bool TryResolve(
        XmlNode document,
        string? systemId,
        IProblemSink problems,
        CancellationToken cancellationToken,
        out XmlNode result
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(document.Tree);
        ArgumentNullException.ThrowIfNull(problems);

        Uri? documentUri = null;
        if (systemId is not null)
        {
            _ = Uri.TryCreate(systemId, UriKind.Absolute, out documentUri);
        }

        using var run = new XIncludeRun(this, problems, systemId, documentUri, cancellationToken);
        return run.Run(document, out result);
    }

    /// <summary>Resolves a reference against the base and reads the resource under the size cap.</summary>
    internal FetchStatus TryFetch(
        string href,
        string? baseUri,
        DocumentLocation at,
        CancellationToken cancellationToken,
        out ReadOnlyMemory<byte> content,
        out string systemId,
        out SchematronProblem problem
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(href);

        return _fetcher.TryFetch(href, baseUri, at, cancellationToken, out content, out systemId, out problem);
    }
}
