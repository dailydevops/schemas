namespace NetEvolve.Schemas.Xml.Dtd.External.Subset;

using System.Threading;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Resources;
using NetEvolve.Schemas.Xml.Buffers;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Contracts;
using NetEvolve.Schemas.Xml.Dtd.External.Loader;

/// <summary>
/// Reads the external subset and the external parameter entities of a document type declaration as text, through the
/// single access path of <see cref="ExternalEntityLoader"/> (design #122, sections 4 and 8): deny by default, the base URI
/// of the declaring entity, recursion and limits.
/// </summary>
/// <remarks>
/// The declaration parser works on one string per text, so an entity is read completely before it is parsed; the loader
/// frame still decodes it in chunks and charges every chunk to <c>MaxEntityExpansionCharacters</c>, so the text is bounded
/// by that budget and not by the size of the stream. The entity stays registered with the loader until its
/// <see cref="ExternalSubsetText"/> is disposed, so an entity that includes itself, or the same URI, is recursion
/// (<c>XML0020</c>). The reader belongs to one document and is not thread-safe. Limitation: the resolver is called
/// synchronously, also inside the asynchronous stream reader (design #122, section 12: an async path is later work).
/// </remarks>
internal sealed class ExternalSubsetReader
{
    private const int InitialCapacity = 4096;

    private readonly ExternalEntityLoader _loader;

    /// <summary>Initializes a new instance of the <see cref="ExternalSubsetReader"/> class.</summary>
    /// <param name="options">The options: the resolver, the flags, and the limits of DTD validation.</param>
    /// <param name="limits">The reader limits.</param>
    internal ExternalSubsetReader(DtdValidationOptions options, XmlReaderLimits limits)
    {
        Argument.ThrowIfNull(options);
        Argument.ThrowIfNull(limits);

        _loader = new ExternalEntityLoader(options, limits);
    }

    /// <summary>Requests an external subset or an external parameter entity and reads it completely.</summary>
    /// <param name="kind">The kind: an external subset or an external parameter entity.</param>
    /// <param name="name">The document type name or the entity name.</param>
    /// <param name="publicId">The public identifier, or <see langword="null"/>.</param>
    /// <param name="systemId">The system identifier as written.</param>
    /// <param name="declaringBaseUri">The base URI of the entity that contains the declaration, or <see langword="null"/>.</param>
    /// <param name="entity">The text when the method returns <see langword="true"/>; the caller disposes it when the text is parsed.</param>
    /// <param name="error">The error when the method returns <see langword="false"/>.</param>
    /// <returns><see langword="true"/> when the entity was read completely.</returns>
    internal bool TryOpen(
        ResourceKind kind,
        string name,
        string? publicId,
        string systemId,
        string? declaringBaseUri,
        out ExternalSubsetText? entity,
        out XmlErrorCode? error
    )
    {
        Argument.ThrowIfNull(name);
        Argument.ThrowIfNull(systemId);

        entity = null;
        error = null;
        ExternalEntityFrame? frame = null;
        try
        {
            if (
                !_loader.TryLoad(
                    kind,
                    name,
                    publicId,
                    systemId,
                    declaringBaseUri,
                    CancellationToken.None,
                    out frame,
                    out var failure
                )
            )
            {
                error = failure.Code;
                return false;
            }

            using var text = new PooledBuffer<char>(InitialCapacity);
            while (frame!.TryRead(out var chunk))
            {
                text.Append(chunk.Span);
            }

            if (frame.Error is not null)
            {
                error = frame.Error;
                return false;
            }

            entity = new ExternalSubsetText(frame, text.WrittenSpan.ToString());
            frame = null;
            return true;
        }
        finally
        {
            frame?.Dispose();
        }
    }

    /// <summary>Charges characters of an internal replacement text to the expansion budget shared with the external entities.</summary>
    /// <param name="count">The number of characters.</param>
    /// <returns><see langword="false"/> when the budget is exceeded; <c>XML1109</c> is then the error to report.</returns>
    internal bool TryChargeExpansion(int count)
    {
        Argument.ThrowIfLessThan(count, 0);

        return _loader.TryChargeExpansion(count);
    }
}
