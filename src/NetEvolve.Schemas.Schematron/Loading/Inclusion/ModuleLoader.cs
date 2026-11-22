namespace NetEvolve.Schemas.Schematron.Loading.Inclusion;

using System.Threading;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Resources;
using NetEvolve.Schemas.Resources.Default;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.XPath.Xdm.Nodes;

/// <summary>
/// Loads the modules reached by <c>include</c> and <c>extends</c> through the core resolver (design #137 AP-22): resolves the
/// reference against the base URI of the referencing module, asks the resolver, caps the size, and reads the document.
/// </summary>
/// <remarks>
/// <para>
/// One instance per build, not thread-safe: it counts. Every call counts against <see cref="ValidationLimits.MaxExternalResources"/>
/// before anything else happens, so a refused or missing resource counts, and nothing is cached or de-duplicated: the number of
/// loads bounds the nesting and the size of the merged model (decision OQ-5 of design #137). The default
/// <see cref="DenyAllResourceResolver"/> refuses every request with <see cref="CoreDiagnosticCodes.ResourceDenied"/> and opens
/// nothing. A refusal, a miss, a bad reference, and a limit are reported with the core codes at the reference. The size of a
/// document is capped by <see cref="ValidationLimits.MaxResourceSize"/>.
/// </para>
/// <para>
/// Limitations: <c>xml:base</c> and <c>xml:lang</c> are not applied (decision Q10 of design #106, section 11 of design #137):
/// the base URI of a loaded module is its system identifier. A document that is not well formed is reported with
/// <see cref="SchematronDiagnosticCodes.ModuleInvalid"/>; the reader limits are the reader defaults.
/// </para>
/// </remarks>
internal sealed class ModuleLoader : IModuleLoader
{
    private readonly IncludeFetcher _fetcher;

    internal ModuleLoader(IResourceResolver resolver, ValidationLimits limits)
    {
        ArgumentNullException.ThrowIfNull(resolver);
        ArgumentNullException.ThrowIfNull(limits);

        _fetcher = new IncludeFetcher(resolver, limits);
    }

    public bool TryLoad(
        in ModuleReference reference,
        CancellationToken cancellationToken,
        out LoadedModule module,
        out SchematronProblem problem
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        module = default;
        if (
            _fetcher.TryFetch(
                reference.Href,
                reference.BaseUri,
                reference.Location,
                cancellationToken,
                out var content,
                out var systemId,
                out problem
            ) != FetchStatus.Found
            || !IncludeFetcher.TryParse(
                content.Span,
                "module",
                systemId,
                reference.Location,
                out var document,
                out problem
            )
        )
        {
            return false;
        }

        module = new LoadedModule(new XPathNode(document), systemId);
        return true;
    }
}
