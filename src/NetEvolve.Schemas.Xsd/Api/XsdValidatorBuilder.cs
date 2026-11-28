#pragma warning disable NE0002 // Approved namespace NetEvolve.Schemas for format entry points (design #103 AP-2, #146 DP-1)

namespace NetEvolve.Schemas;

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Resources;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xsd.Compilation;
using NetEvolve.Schemas.Xsd.Validation.Assembly;

/// <summary>Fluent builder for an XSD validator: version, limits, and resolver. Builders are not thread-safe.</summary>
/// <remarks>
/// <c>Build</c> compiles every schema document that was added and throws a <see cref="SchemaCompilationException"/> with all issues
/// when the schema has an error; without any schema document it succeeds, and every element of an instance is then undeclared.
/// <c>BuildAsync</c> reads the stream sources with <c>ReadAsync</c> and runs the same compilation. <c>AddSchemaLocation</c> names a schema
/// document that the build loads through the resolver of the builder.
/// </remarks>
public sealed class XsdValidatorBuilder : SchemaValidatorBuilder<XsdValidatorBuilder, IXsdValidator>
{
    private readonly List<SchemaSourceHolder> _sources = [];
    private readonly List<(string Reference, string? NamespaceName)> _locations = [];
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
        ArgumentOutOfRangeException.ThrowIfLessThan((int)version, (int)XsdVersion.V10);
        ArgumentOutOfRangeException.ThrowIfGreaterThan((int)version, (int)XsdVersion.V11);

        _version = version;
        return this;
    }

    /// <summary>Adds a schema document given as characters; the characters are copied, so the caller may reuse its buffer.</summary>
    /// <param name="content">The document.</param>
    /// <param name="systemId">The system identifier and base URI of the document, or <see langword="null"/>.</param>
    /// <returns>This builder.</returns>
    public XsdValidatorBuilder AddSchema(ReadOnlyMemory<char> content, string? systemId = null)
    {
        _sources.Add(SchemaSourceHolder.FromChars(content.Span, systemId));
        return this;
    }

    /// <summary>Adds a schema document given as bytes; the bytes are copied, so the caller may reuse its buffer.</summary>
    /// <param name="content">The document.</param>
    /// <param name="systemId">The system identifier and base URI of the document, or <see langword="null"/>.</param>
    /// <returns>This builder.</returns>
    public XsdValidatorBuilder AddSchema(ReadOnlyMemory<byte> content, string? systemId = null)
    {
        _sources.Add(SchemaSourceHolder.FromBytes(content.Span, systemId));
        return this;
    }

    /// <summary>Adds a schema document read from a stream at <c>Build</c> or <c>BuildAsync</c>; the stream is never disposed.</summary>
    /// <param name="stream">The stream, read from its current position to its end.</param>
    /// <param name="systemId">The system identifier and base URI of the document, or <see langword="null"/>.</param>
    /// <returns>This builder.</returns>
    public XsdValidatorBuilder AddSchema(Stream stream, string? systemId = null)
    {
        ArgumentNullException.ThrowIfNull(stream);

        _sources.Add(SchemaSourceHolder.FromStream(stream, systemId));
        return this;
    }

    /// <summary>
    /// Adds a schema document that the build loads through the resolver of the builder instead of receiving its content: the request
    /// has the kind <see cref="ResourceKind.SchemaDocument"/> and the namespace in <see cref="ResourceRequest.NamespaceUri"/>.
    /// </summary>
    /// <remarks>
    /// The resolver is the one of the builder at <c>Build</c>, which refuses everything unless another one is set. A document that is
    /// refused, missing, or larger than <see cref="ValidationLimits.MaxResourceSize"/> is a schema error of the build, and the request
    /// counts against <see cref="ValidationLimits.MaxExternalResources"/>. The location is not resolved against a base URI.
    /// </remarks>
    /// <param name="reference">The location of the document, as the resolver receives it.</param>
    /// <param name="namespaceName">The target namespace that the document must have, or <see langword="null"/> for no expectation.</param>
    /// <returns>This builder.</returns>
    public XsdValidatorBuilder AddSchemaLocation(string reference, string? namespaceName = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(reference);

        _locations.Add((reference, namespaceName));
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
    /// Limitations: a hinted schema document may refer to components of the configured schema but cannot redeclare them.
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
            ReadInputs(limits, CancellationToken.None),
            CancellationToken.None,
            _schemaLocationHints
        );

    /// <inheritdoc />
    protected override async ValueTask<IXsdValidator> CreateValidatorAsync(
        ValidationLimits limits,
        IResourceResolver resolver,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        foreach (var source in _sources)
        {
            await source.PrefetchAsync(limits, cancellationToken).ConfigureAwait(false);
        }

        return ValidatorAssembly.Create(
            _version,
            _dtdProcessing,
            limits,
            resolver,
            ReadInputs(limits, cancellationToken),
            cancellationToken,
            _schemaLocationHints
        );
    }

    // A schema is a resource: every document is bounded by MaxResourceSize (NES0007), and a stream is read once and replayed.
    private List<SchemaInput> ReadInputs(ValidationLimits limits, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var inputs = new List<SchemaInput>(_sources.Count + _locations.Count);
        foreach (var source in _sources)
        {
            inputs.Add(
                source.Kind == DocumentSourceKind.Chars
                    ? SchemaInput.FromChars(source.GetChars(limits), source.SystemId)
                    : SchemaInput.FromBytes(source.GetBytes(limits, cancellationToken), source.SystemId)
            );
        }

        foreach (var (reference, namespaceName) in _locations)
        {
            inputs.Add(SchemaInput.FromLocation(reference, namespaceName));
        }

        return inputs;
    }
}
