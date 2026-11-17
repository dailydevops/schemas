namespace NetEvolve.Schemas.Schematron.Loading.Inclusion;

using System;
using System.Threading;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Resources;
using NetEvolve.Schemas.Resources.Default;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Model;
using NetEvolve.Schemas.Xml.Reader.Pipeline;
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
    private readonly IResourceResolver _resolver;
    private readonly ValidationLimits _limits;
    private int _requests;

    internal ModuleLoader(IResourceResolver resolver, ValidationLimits limits)
    {
        Argument.ThrowIfNull(resolver);
        Argument.ThrowIfNull(limits);

        _resolver = resolver;
        _limits = limits;
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
        if (_requests >= _limits.MaxExternalResources)
        {
            problem = Problem(
                CoreDiagnosticCodes.ExternalResourceLimitExceeded,
                "More than " + _limits.MaxExternalResources + " external resources are referenced.",
                in reference
            );
            return false;
        }

        _requests++;
        var resolution = UriReference.Resolve(reference.Href, reference.BaseUri);
        if (resolution.Uri is null)
        {
            problem = Problem(
                resolution.Code,
                resolution.Code == CoreDiagnosticCodes.ResourceBaseUriMissing
                    ? "The reference '" + reference.Href + "' is relative and the base URI is not absolute."
                    : "The reference '" + reference.Href + "' is not a valid URI reference.",
                in reference
            );
            return false;
        }

        var request = new ResourceRequest(
            ResourceKind.SchemaDocument,
            reference.Href,
            reference.BaseUri,
            resolution.Uri
        );
        using var result = _resolver.Resolve(in request, cancellationToken);
        if (result.Status != ResourceStatus.Found)
        {
            var notFound = result.Status == ResourceStatus.NotFound;
            problem = Problem(
                notFound ? CoreDiagnosticCodes.ResourceNotFound : CoreDiagnosticCodes.ResourceDenied,
                result.Message ?? "The resource '" + resolution.Uri + (notFound ? "' was not found." : "' was denied."),
                in reference
            );
            return false;
        }

        var systemId = result.SystemId ?? resolution.Uri;
        if (!ResourceContent.TryRead(in result, _limits.MaxResourceSize, out var content))
        {
            problem = Problem(
                CoreDiagnosticCodes.ResourceSizeLimitExceeded,
                "The resource '" + systemId + "' is larger than " + _limits.MaxResourceSize + " bytes.",
                in reference
            );
            return false;
        }

        return TryParse(content.Span, systemId, in reference, out module, out problem);
    }

    private static bool TryParse(
        ReadOnlySpan<byte> content,
        string systemId,
        in ModuleReference reference,
        out LoadedModule module,
        out SchematronProblem problem
    )
    {
        using var reader = XmlPipelineReader.Create(content);
        using var builder = new XmlTreeBuilder();
        var sink = builder;
        if (reader.Pump(ref sink) == XmlReadStatus.Completed)
        {
            module = new LoadedModule(new XPathNode(builder.Complete().Document), systemId);
            problem = default;
            return true;
        }

        var error = reader.Diagnostic;
        module = default;
        problem = Problem(
            SchematronDiagnosticCodes.ModuleInvalid,
            "The module '"
                + systemId
                + "' is not well formed"
                + (
                    error is null
                        ? "."
                        : ": "
                            + error.Message
                            + " (line "
                            + error.Position.Line
                            + ", column "
                            + error.Position.Column
                            + ")."
                ),
            in reference
        );
        return false;
    }

    private static SchematronProblem Problem(DiagnosticCode code, string message, in ModuleReference reference) =>
        new(code, message, reference.Location);
}
