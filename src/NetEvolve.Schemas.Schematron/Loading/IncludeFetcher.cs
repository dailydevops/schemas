namespace NetEvolve.Schemas.Schematron.Loading;

using System;
using System.Threading;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Resources;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Model;
using NetEvolve.Schemas.Xml.Reader.Pipeline;

/// <summary>
/// The fetch and parse steps shared by the Schematron include loader and the XInclude resolver (design #137 AP-22): resolves a
/// reference through the core <see cref="ResourceLoader"/> and reads the bytes into a tree.
/// </summary>
/// <remarks>
/// One instance per loader, not thread-safe: it counts. Every <see cref="TryFetch"/> counts against
/// <see cref="ValidationLimits.MaxExternalResources"/> before anything else happens, so a refused or missing resource counts, and
/// nothing is cached or de-duplicated (decision OQ-5 of design #137). The raw bytes are exposed, because a text include needs them.
/// </remarks>
internal sealed class IncludeFetcher
{
    private readonly ResourceLoader _loader;

    internal IncludeFetcher(IResourceResolver resolver, ValidationLimits limits)
    {
        ArgumentNullException.ThrowIfNull(resolver);
        ArgumentNullException.ThrowIfNull(limits);

        _loader = new ResourceLoader(resolver, limits);
        Limits = limits;
    }

    internal ValidationLimits Limits { get; }

    /// <summary>Resolves a reference against the base and reads the resource under the size cap.</summary>
    /// <param name="href">The reference.</param>
    /// <param name="baseUri">The base URI of the referencing document, or <see langword="null"/>.</param>
    /// <param name="at">The location the problems are reported at.</param>
    /// <param name="cancellationToken">The token passed to the resolver.</param>
    /// <param name="content">The bytes of the resource when the result is <see cref="FetchStatus.Found"/>.</param>
    /// <param name="systemId">The system identifier of the resource, once it was found.</param>
    /// <param name="problem">The problem when the result is not <see cref="FetchStatus.Found"/>.</param>
    /// <returns>
    /// <see cref="FetchStatus.Found"/>, <see cref="FetchStatus.Recoverable"/> for a bad reference or a missing or refused
    /// resource, or <see cref="FetchStatus.Fatal"/> when a limit was breached.
    /// </returns>
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

        var result = _loader.Load(ResourceKind.SchemaDocument, href, baseUri, cancellationToken);
        content = result.Content;
        systemId = result.SystemId ?? string.Empty;
        if (result.Status == ResourceLoadStatus.Loaded)
        {
            problem = default;
            return FetchStatus.Found;
        }

        problem = new SchematronProblem(result.Code, result.Message, at);
        return result.Status == ResourceLoadStatus.Failed ? FetchStatus.Recoverable : FetchStatus.Fatal;
    }

    /// <summary>Reads the bytes of a fetched resource into a tree.</summary>
    /// <param name="content">The bytes.</param>
    /// <param name="kind">What the resource is called in the message, for example <c>module</c>.</param>
    /// <param name="systemId">The system identifier of the resource.</param>
    /// <param name="at">The location a problem is reported at.</param>
    /// <param name="document">The document node, empty when the content is not well formed.</param>
    /// <param name="problem">The <see cref="SchematronDiagnosticCodes.ModuleInvalid"/> problem when the result is <see langword="false"/>.</param>
    /// <returns><see langword="true"/> when the content is well formed.</returns>
    internal static bool TryParse(
        ReadOnlySpan<byte> content,
        string kind,
        string systemId,
        DocumentLocation at,
        out XmlNode document,
        out SchematronProblem problem
    )
    {
        ArgumentNullException.ThrowIfNull(kind);
        ArgumentNullException.ThrowIfNull(systemId);

        using var reader = XmlPipelineReader.Create(content);
        using var builder = new XmlTreeBuilder();
        var sink = builder;
        if (reader.Pump(ref sink) == XmlReadStatus.Completed)
        {
            document = builder.Complete().Document;
            problem = default;
            return true;
        }

        var error = reader.Diagnostic;
        document = default;
        problem = new SchematronProblem(
            SchematronDiagnosticCodes.ModuleInvalid,
            "The "
                + kind
                + " '"
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
            at
        );
        return false;
    }
}
