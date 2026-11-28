namespace NetEvolve.Schemas.Xml.Dtd.Validation.Pipeline;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Validation.Attributes;
using NetEvolve.Schemas.Xml.Dtd.Validation.ContentModels;
using NetEvolve.Schemas.Xml.Dtd.Validation.Document;
using NetEvolve.Schemas.Xml.Dtd.Validation.Identity;
using NetEvolve.Schemas.Xml.Dtd.Validation.Notations;
using NetEvolve.Schemas.Xml.Reader.Pipeline;

/// <summary>
/// The validating reader mode (XML 1.0, section 5.1; design #122, D1 to D4): one pass over the reader events checks a
/// document against its document type declaration. The reader runs with DTD processing on and the options of external
/// access, so the external subset and the external entities are requested through the resolver when the options allow it
/// (design #122, D6 and D7); a document type declaration is processed, not rejected. The steps run in the registration order of
/// design #122, D2: element content, attributes, identity, notations, and the document-level constraints.
/// </summary>
/// <remarks>
/// <para>
/// Validity errors are non-fatal issues in the result and never stop the parse; a fatal status of the reader (a
/// well-formedness error, a limit, a denied or failing external request) ends the run, skips the end of the steps, and is
/// the last issue of the result. A document without a document type declaration fires the start of the steps with an
/// empty model, so the document step reports <c>XML5910</c>.
/// </para>
/// <para>
/// Limitations: the input is complete bytes or characters (chunked, stream, and sequence input are later work packages;
/// the readers behind them do not expose the declaration model); the events of an entity reference are not reported to the
/// steps (see <see cref="DtdValidatingSink{TInner}"/>); the public entry point is XML-69 (<c>SchemaValidator.ForDtd</c>),
/// so the pipeline is internal. The members that take a sink pass its state back through the <see langword="ref"/>
/// argument, so a struct sink keeps what it collected. Not thread-safe per run; a run shares nothing with another.
/// </para>
/// </remarks>
internal static class DtdValidatingPipeline
{
    /// <summary>Validates a document of characters and drops the events of the reader.</summary>
    /// <param name="input">The document.</param>
    /// <param name="options">The options, <see langword="null"/> for <see cref="DtdValidationOptions.Default"/>.</param>
    /// <param name="readerOptions">The options of the reader, <see langword="null"/> for <see cref="XmlReaderOptions.Default"/>; DTD processing, external access, and the base URI are set by the pipeline.</param>
    /// <param name="documentUri">The absolute URI of the document, used as its system identifier in the issues and as the base of relative system identifiers; <see langword="null"/> when unknown.</param>
    /// <returns>The outcome.</returns>
    internal static DtdPipelineResult Validate(
        ReadOnlySpan<char> input,
        DtdValidationOptions? options = null,
        XmlReaderOptions? readerOptions = null,
        string? documentUri = null
    )
    {
        var sink = new DtdDiscardSink((readerOptions ?? XmlReaderOptions.Default).Limits);

        return Validate(input, ref sink, options, readerOptions, documentUri);
    }

    /// <summary>Validates a document of bytes and drops the events of the reader; the encoding follows XML 1.0 Appendix F and the declaration.</summary>
    /// <param name="input">The document.</param>
    /// <param name="options">The options, <see langword="null"/> for <see cref="DtdValidationOptions.Default"/>.</param>
    /// <param name="readerOptions">The options of the reader, <see langword="null"/> for <see cref="XmlReaderOptions.Default"/>.</param>
    /// <param name="documentUri">The absolute URI of the document, or <see langword="null"/> when unknown.</param>
    /// <returns>The outcome.</returns>
    internal static DtdPipelineResult Validate(
        ReadOnlySpan<byte> input,
        DtdValidationOptions? options = null,
        XmlReaderOptions? readerOptions = null,
        string? documentUri = null
    )
    {
        var sink = new DtdDiscardSink((readerOptions ?? XmlReaderOptions.Default).Limits);

        return Validate(input, ref sink, options, readerOptions, documentUri);
    }

    /// <summary>Validates a document of characters and forwards every event unchanged to a sink.</summary>
    /// <typeparam name="TSink">The type of the sink.</typeparam>
    /// <param name="input">The document.</param>
    /// <param name="inner">The sink; it holds its state again when the call returns.</param>
    /// <param name="options">The options, <see langword="null"/> for <see cref="DtdValidationOptions.Default"/>.</param>
    /// <param name="readerOptions">The options of the reader, <see langword="null"/> for <see cref="XmlReaderOptions.Default"/>.</param>
    /// <param name="documentUri">The absolute URI of the document, or <see langword="null"/> when unknown.</param>
    /// <param name="steps">The steps in registration order, <see langword="null"/> for <see cref="CreateSteps"/>.</param>
    /// <returns>The outcome.</returns>
    internal static DtdPipelineResult Validate<TSink>(
        ReadOnlySpan<char> input,
        ref TSink inner,
        DtdValidationOptions? options = null,
        XmlReaderOptions? readerOptions = null,
        string? documentUri = null,
        IReadOnlyList<IDtdValidationStep>? steps = null
    )
        where TSink : IXmlSink
    {
        options ??= DtdValidationOptions.Default;
        var effective = ReaderOptions(options, readerOptions, documentUri);
        using var reader = XmlPipelineReader.Create(input, effective);

        return Run(reader, ref inner, options, steps ?? CreateSteps(effective.Limits), documentUri);
    }

    /// <summary>Validates a document of bytes and forwards every event unchanged to a sink.</summary>
    /// <typeparam name="TSink">The type of the sink.</typeparam>
    /// <param name="input">The document.</param>
    /// <param name="inner">The sink; it holds its state again when the call returns.</param>
    /// <param name="options">The options, <see langword="null"/> for <see cref="DtdValidationOptions.Default"/>.</param>
    /// <param name="readerOptions">The options of the reader, <see langword="null"/> for <see cref="XmlReaderOptions.Default"/>.</param>
    /// <param name="documentUri">The absolute URI of the document, or <see langword="null"/> when unknown.</param>
    /// <param name="steps">The steps in registration order, <see langword="null"/> for <see cref="CreateSteps"/>.</param>
    /// <returns>The outcome.</returns>
    internal static DtdPipelineResult Validate<TSink>(
        ReadOnlySpan<byte> input,
        ref TSink inner,
        DtdValidationOptions? options = null,
        XmlReaderOptions? readerOptions = null,
        string? documentUri = null,
        IReadOnlyList<IDtdValidationStep>? steps = null
    )
        where TSink : IXmlSink
    {
        options ??= DtdValidationOptions.Default;
        var effective = ReaderOptions(options, readerOptions, documentUri);
        using var reader = XmlPipelineReader.Create(input, effective);

        return Run(reader, ref inner, options, steps ?? CreateSteps(effective.Limits), documentUri);
    }

    /// <summary>Creates the steps in the registration order of design #122, D2: element content, attributes, identity, notations, document.</summary>
    /// <param name="limits">The limits of the reader.</param>
    /// <returns>New steps; a step keeps the state of one run, so a run needs its own.</returns>
    internal static IReadOnlyList<IDtdValidationStep> CreateSteps(XmlReaderLimits limits)
    {
        ArgumentNullException.ThrowIfNull(limits);

        return
        [
            new DtdContentModelStep(),
            new DtdAttributeStep(limits),
            new DtdIdentityStep(limits),
            new DtdNotationStep(),
            new DtdDocumentStep(),
        ];
    }

    /// <summary>Derives the options of the reader: DTD processing on, the options of external access, and the base URI of the document.</summary>
    /// <param name="options">The options of validation.</param>
    /// <param name="readerOptions">The options of the reader, <see langword="null"/> for <see cref="XmlReaderOptions.Default"/>.</param>
    /// <param name="documentUri">The absolute URI of the document, or <see langword="null"/> when unknown.</param>
    /// <returns>The options of the reader.</returns>
    internal static XmlReaderOptions ReaderOptions(
        DtdValidationOptions options,
        XmlReaderOptions? readerOptions,
        string? documentUri
    )
    {
        ArgumentNullException.ThrowIfNull(options);

        return (readerOptions ?? XmlReaderOptions.Default)
            .WithDtdProcessing(true)
            .WithExternalAccess(options)
            .WithBaseUri(documentUri);
    }

    /// <summary>Pumps a reader into the validating sink and collects the issues.</summary>
    /// <typeparam name="TSink">The type of the inner sink.</typeparam>
    /// <param name="reader">The reader, created with the options of <see cref="ReaderOptions"/>.</param>
    /// <param name="inner">The inner sink; it holds its state again when the call returns.</param>
    /// <param name="options">The options.</param>
    /// <param name="steps">The steps in registration order.</param>
    /// <param name="documentUri">The URI of the document, or <see langword="null"/> when unknown.</param>
    /// <returns>The outcome.</returns>
    internal static DtdPipelineResult Run<TSink>(
        XmlPipelineReader reader,
        ref TSink inner,
        DtdValidationOptions options,
        IReadOnlyList<IDtdValidationStep> steps,
        string? documentUri
    )
        where TSink : IXmlSink
    {
        ArgumentNullException.ThrowIfNull(reader);
        ArgumentNullException.ThrowIfNull(options);

        var issues = new ValidationIssueCollector(options.Limits);
        var sink = new DtdValidatingSink<TSink>(reader, inner, steps, options, issues, documentUri);
        var status = reader.Pump(ref sink);
        inner = sink.Inner;
        if (status != XmlReadStatus.Completed)
        {
            // A fatal status is the last issue; the collector ignores it when a step stopped the run before.
            var diagnostic = reader.Diagnostic!;
            issues.Stop(
                new ValidationIssue(
                    diagnostic.Code.Code,
                    IssueSeverity.Error,
                    diagnostic.Message,
                    new DocumentLocation(documentUri, diagnostic.Position.Line, diagnostic.Position.Column),
                    diagnostic.Code.Constraint
                )
            );
        }

        return new DtdPipelineResult(status, reader.Diagnostic, issues.ToResult());
    }
}
