namespace NetEvolve.Schemas.Xml.Dtd.External.General;

using System.Threading;
using NetEvolve.Schemas.Resources;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Dtd.External.Loader;
using NetEvolve.Schemas.Xml.Dtd.Model;
using NetEvolve.Schemas.Xml.Reader.Core;

/// <summary>
/// Opens the external general parsed entities that a document references in content, through the single access path of
/// <see cref="ExternalEntityLoader"/> (design #122, sections 4 and 8): deny by default, the base URI of the declaring
/// entity, recursion, and the limits that the external subset shares.
/// </summary>
/// <remarks>
/// The loader is the one of the document, so the requests, the depth, and the decoded characters of the subset and of the
/// general entities are charged to the same budgets. The resolver is called synchronously, also inside the asynchronous stream
/// reader (design #122, section 12: an async path is later work). Limitations: an <c>xml:base</c> attribute does not take part in
/// the resolution (#122, section 12), and the base URI of an entity is not reported by the events of its nodes: the sink contract
/// has no such member, so the tree reports the document URI for them (tracked in #730). Not thread-safe.
/// </remarks>
internal sealed class ExternalGeneralEntities
{
    private readonly ExternalEntityLoader _loader;

    /// <summary>Initializes a new instance of the <see cref="ExternalGeneralEntities"/> class.</summary>
    /// <param name="loader">The loader of the document.</param>
    internal ExternalGeneralEntities(ExternalEntityLoader loader)
    {
        ArgumentNullException.ThrowIfNull(loader);

        _loader = loader;
    }

    /// <summary>Charges characters of an internal replacement text to the expansion budget shared with the external entities.</summary>
    /// <param name="count">The number of characters.</param>
    /// <returns><see langword="false"/> when the budget is exceeded; <c>XML1109</c> is then the error to report.</returns>
    internal bool TryChargeExpansion(int count)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(count, 0);

        return _loader.TryChargeExpansion(count);
    }

    /// <summary>Requests an external general parsed entity; its head, the text declaration, is read.</summary>
    /// <param name="entity">The declaration of a general entity with a system identifier and no notation.</param>
    /// <param name="text">The text when the method returns <see langword="true"/>; the caller reads it and disposes it.</param>
    /// <param name="error">The error when the method returns <see langword="false"/>.</param>
    /// <returns><see langword="true"/> when the entity was found and its head was read.</returns>
    internal bool TryOpen(DtdEntityDeclaration entity, out IXmlEntityText? text, out XmlErrorCode? error)
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentNullException.ThrowIfNull(entity.SystemId);

        text = null;
        error = null;
        ExternalEntityFrame? frame = null;
        try
        {
            if (
                !_loader.TryLoad(
                    ResourceKind.ExternalEntity,
                    entity.Name,
                    entity.PublicId,
                    entity.SystemId,
                    entity.BaseUri,
                    CancellationToken.None,
                    out frame,
                    out var failure
                )
            )
            {
                error = failure.Code;
                return false;
            }

            text = new ExternalGeneralEntityText(frame!);
            frame = null;
            return true;
        }
        finally
        {
            frame?.Dispose();
        }
    }
}
