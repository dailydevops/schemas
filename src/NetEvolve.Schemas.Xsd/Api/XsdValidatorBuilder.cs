#pragma warning disable NE0002 // Approved namespace NetEvolve.Schemas for format entry points (design #103 AP-2, #146 DP-1)

namespace NetEvolve.Schemas;

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Resources;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xsd.Validation.Assembly;

/// <summary>Fluent builder for an XSD validator: version, limits, and resolver. Builders are not thread-safe.</summary>
/// <remarks>
/// <c>Build</c> compiles every schema document that was added and throws a <see cref="SchemaCompilationException"/> with all issues
/// when the schema has an error; without any schema document it succeeds, and every element of an instance is then undeclared.
/// <c>BuildAsync</c> reads the stream sources with <c>ReadAsync</c> and runs the same compilation. Limitation: <c>AddSchemaLocation</c>
/// is not available yet (#897).
/// </remarks>
public sealed class XsdValidatorBuilder : SchemaValidatorBuilder<XsdValidatorBuilder, IXsdValidator>
{
    private readonly List<SchemaSource> _sources = [];
    private XsdVersion _version = XsdVersion.V11;
    private bool _dtdProcessing;
    private bool _schemaLocationHints;

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
    public XsdValidatorBuilder AddSchema(ReadOnlyMemory<char> content, string? systemId = null)
    {
        _sources.Add(SchemaSource.FromChars(content, systemId));
        return this;
    }

    /// <summary>Adds a schema document given as bytes; the memory is borrowed until <c>Build</c> returns.</summary>
    /// <param name="content">The document.</param>
    /// <param name="systemId">The system identifier and base URI of the document, or <see langword="null"/>.</param>
    /// <returns>This builder.</returns>
    public XsdValidatorBuilder AddSchema(ReadOnlyMemory<byte> content, string? systemId = null)
    {
        _sources.Add(SchemaSource.FromBytes(content, systemId));
        return this;
    }

    /// <summary>Adds a schema document read from a stream at <c>Build</c> or <c>BuildAsync</c>; the stream is never disposed.</summary>
    /// <param name="stream">The stream, read from its current position to its end.</param>
    /// <param name="systemId">The system identifier and base URI of the document, or <see langword="null"/>.</param>
    /// <returns>This builder.</returns>
    public XsdValidatorBuilder AddSchema(Stream stream, string? systemId = null)
    {
        Argument.ThrowIfNull(stream);

        _sources.Add(SchemaSource.FromStream(stream, systemId));
        return this;
    }

    /// <summary>
    /// Selects whether the validator reads the document type declaration of an instance, so that ENTITY and ENTITIES values are
    /// checked against its unparsed entity declarations. Off by default: a document type declaration then ends the run with a reader
    /// error.
    /// </summary>
    /// <remarks>Limitation: only the internal subset is read; the entities of an external subset are not visible (design #146 DP-21).</remarks>
    /// <param name="enabled"><see langword="true"/> to read the internal subset.</param>
    /// <returns>This builder.</returns>
    public XsdValidatorBuilder WithDtdProcessing(bool enabled)
    {
        _dtdProcessing = enabled;
        return this;
    }

    /// <summary>
    /// Selects whether the validator loads schema documents that the instance names with <c>xsi:schemaLocation</c> and
    /// <c>xsi:noNamespaceSchemaLocation</c>. Off by default: the hints are then ignored and no location is accessed. When on, every
    /// location goes through the resolver of the builder, which refuses everything unless another one is set.
    /// </summary>
    /// <remarks>
    /// Limitations: a hinted schema document is compiled on its own, so it cannot refer to components of the configured schema; its identity
    /// constraints are not enforced; the validation of a tree does not read hints.
    /// </remarks>
    /// <param name="enabled"><see langword="true"/> to load the schema documents that the hints name.</param>
    /// <returns>This builder.</returns>
    public XsdValidatorBuilder WithSchemaLocationHints(bool enabled)
    {
        _schemaLocationHints = enabled;
        return this;
    }

    /// <inheritdoc />
    protected override IXsdValidator CreateValidator(ValidationLimits limits, IResourceResolver resolver) =>
        ValidatorAssembly.Create(
            _version,
            _dtdProcessing,
            limits,
            resolver,
            SchemaSources.Read(_sources, limits),
            CancellationToken.None,
            _schemaLocationHints
        );

    /// <inheritdoc />
    protected override async ValueTask<IXsdValidator> CreateValidatorAsync(
        ValidationLimits limits,
        IResourceResolver resolver,
        CancellationToken cancellationToken
    ) =>
        ValidatorAssembly.Create(
            _version,
            _dtdProcessing,
            limits,
            resolver,
            await SchemaSources.ReadAsync(_sources, limits, cancellationToken).ConfigureAwait(false),
            cancellationToken,
            _schemaLocationHints
        );
}
