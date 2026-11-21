#pragma warning disable NE0002 // Approved namespace NetEvolve.Schemas for format entry points (design #103 AP-2, #146 DP-1)

namespace NetEvolve.Schemas;

using System;
using System.IO;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Resources;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;

/// <summary>Fluent builder for an XSD validator: version, limits, and resolver. Builders are not thread-safe.</summary>
/// <remarks>
/// Limitation: until work package XSD-57 lands, <c>Build</c> and <c>BuildAsync</c> fail with a
/// <see cref="NotSupportedException"/>. Schema sources, DTD processing, and schema location hints are added by
/// the work packages that implement them.
/// </remarks>
public sealed class XsdValidatorBuilder : SchemaValidatorBuilder<XsdValidatorBuilder, IXsdValidator>
{
    private XsdVersion _version = XsdVersion.V11;

    internal XsdValidatorBuilder() { }

    /// <summary>Gets the XSD version, <see cref="XsdVersion.V11"/> unless set.</summary>
    public XsdVersion Version => _version;

    /// <summary>Selects the XSD version. <see cref="XsdVersion.V10"/> is strict: every 1.1 construct is a schema error.</summary>
    /// <param name="version">The version.</param>
    /// <returns>This builder.</returns>
    public XsdValidatorBuilder WithVersion(XsdVersion version)
    {
        Argument.ThrowIfLessThan((int)version, (int)XsdVersion.V10);
        Argument.ThrowIfGreaterThan((int)version, (int)XsdVersion.V11);

        _version = version;
        return this;
    }

    /// <summary>Adds a schema document given as characters; the memory is borrowed until <c>Build</c> returns.</summary>
    /// <param name="content">The document.</param>
    /// <param name="systemId">The system identifier and base URI of the document, or <see langword="null"/>.</param>
    /// <returns>This builder.</returns>
    public XsdValidatorBuilder AddSchema(ReadOnlyMemory<char> content, string? systemId = null) =>
        throw new NotSupportedException("Schema sources are not available yet.");

    /// <summary>Adds a schema document given as bytes; the memory is borrowed until <c>Build</c> returns.</summary>
    /// <param name="content">The document.</param>
    /// <param name="systemId">The system identifier and base URI of the document, or <see langword="null"/>.</param>
    /// <returns>This builder.</returns>
    public XsdValidatorBuilder AddSchema(ReadOnlyMemory<byte> content, string? systemId = null) =>
        throw new NotSupportedException("Schema sources are not available yet.");

    /// <summary>Adds a schema document read from a stream at <c>Build</c> or <c>BuildAsync</c>; the stream is never disposed.</summary>
    /// <param name="stream">The stream.</param>
    /// <param name="systemId">The system identifier and base URI of the document, or <see langword="null"/>.</param>
    /// <returns>This builder.</returns>
    public XsdValidatorBuilder AddSchema(Stream stream, string? systemId = null) =>
        throw new NotSupportedException("Schema sources are not available yet.");

    /// <summary>Selects whether the validator processes the document type declaration of an instance; off by default.</summary>
    /// <param name="enabled"><see langword="true"/> to read the internal subset and validate ENTITY values against it.</param>
    /// <returns>This builder.</returns>
    public XsdValidatorBuilder WithDtdProcessing(bool enabled) =>
        throw new NotSupportedException("DTD processing is not available yet.");

    /// <inheritdoc />
    protected override IXsdValidator CreateValidator(ValidationLimits limits, IResourceResolver resolver) =>
        throw new NotSupportedException("The XSD validator is not available until work package XSD-57 lands.");
}
