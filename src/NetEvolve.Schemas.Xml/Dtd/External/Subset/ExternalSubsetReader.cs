namespace NetEvolve.Schemas.Xml.Dtd.External.Subset;

using System;
using NetEvolve.Schemas.Resources;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Contracts;

/// <summary>Reads the external subset and the external parameter entities of a document type declaration as text.</summary>
internal sealed class ExternalSubsetReader
{
    /// <summary>Initializes a new instance of the <see cref="ExternalSubsetReader"/> class.</summary>
    /// <param name="options">The options: the resolver, the flags, and the limits of DTD validation.</param>
    /// <param name="limits">The reader limits.</param>
    internal ExternalSubsetReader(DtdValidationOptions options, XmlReaderLimits limits) =>
        throw new NotSupportedException();

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
    ) => throw new NotSupportedException();

    /// <summary>Charges characters of an internal replacement text to the expansion budget shared with the external entities.</summary>
    /// <param name="count">The number of characters.</param>
    /// <returns><see langword="false"/> when the budget is exceeded.</returns>
    internal bool TryChargeExpansion(int count) => throw new NotSupportedException();
}
