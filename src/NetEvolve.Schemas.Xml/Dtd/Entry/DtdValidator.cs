namespace NetEvolve.Schemas.Xml.Dtd.Entry;

using System;
using System.Buffers;
using System.Threading;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Xml.Buffers;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Validation.Pipeline;

/// <summary>
/// The immutable, thread-safe DTD validator that <c>SchemaValidator.ForDtd()</c> builds (XML 1.0, section 5.1): every input
/// kind of the base class ends in the one core, which runs the validating reader mode (<see cref="DtdValidatingPipeline"/>)
/// and returns its issues.
/// </summary>
/// <remarks>
/// <para>
/// The core limits reach the reader (depth, attributes per element, entity expansion, and entity depth), and a reader limit
/// that a core limit names is reported with the core code (design #103, AP-18), as the other formats do. Every other fatal
/// status of the reader keeps its code of the Xml range. The limits of DTD validation and the flags of external access
/// come from the options.
/// </para>
/// <para>
/// Limitations: the pipeline reads complete in-memory input, so a <see cref="System.IO.Stream"/> is buffered completely
/// (see <see cref="SchemaValidatorBase"/>) and a sequence of several segments is copied once into a pooled buffer before the
/// run; the stream and sequence readers expose no declaration model (#926). The core limits
/// <see cref="ValidationLimits.MaxExternalResources"/> and <see cref="ValidationLimits.MaxResourceSize"/> are not applied to
/// external resources: <see cref="DtdValidationLimits.MaxExternalEntityRequests"/> bounds the resolver calls, and the bytes
/// of a resource are bounded by the entity expansion budget (#674). The run observes the cancellation token only before it
/// starts.
/// </para>
/// </remarks>
internal sealed class DtdValidator : SchemaValidatorBase, IDtdValidator
{
    private readonly DtdValidationOptions _options;
    private readonly XmlReaderOptions _readerOptions;

    /// <summary>Initializes a new instance of the <see cref="DtdValidator"/> class.</summary>
    /// <param name="options">The options: the resolver, the core limits, the limits of DTD validation, the flags of external access, and the treatment of non-deterministic content models.</param>
    internal DtdValidator(DtdValidationOptions options)
        : base(Required(options).Limits, options.Resolver)
    {
        _options = options;
        _readerOptions = XmlReaderOptions.Default.WithLimits(
            XmlReaderLimits
                .Default.WithMaxElementDepth(options.Limits.MaxDepth)
                .WithMaxAttributesPerElement(options.Limits.MaxAttributesPerElement)
                .WithMaxEntityExpansionCharacters(options.Limits.MaxEntityExpansion)
                .WithMaxEntityDepth(options.Limits.MaxEntityDepth)
        );
    }

    /// <inheritdoc />
    protected override ValidationResult ValidateCore(in DocumentSource source, CancellationToken cancellationToken) =>
        WithCoreCode(source.Kind == DocumentSourceKind.Bytes ? ValidateBytes(in source) : ValidateChars(in source));

    private static DtdValidationOptions Required(DtdValidationOptions options)
    {
        Argument.ThrowIfNull(options);

        return options;
    }

    // A reader limit that a core limit names ends the run with the core code (design #103, AP-18). The fatal status of the
    // reader is the last issue; it is not when a step stopped the run before, and then the issue of the step stays as it is.
    private static ValidationResult WithCoreCode(DtdPipelineResult run)
    {
        var issues = run.Issues.Issues;
        if (run.Diagnostic is not { } diagnostic || diagnostic.Code.CoreCode.Number == 0)
        {
            return run.Issues;
        }

        var last = issues[issues.Count - 1];
        if (last.Code != diagnostic.Code.Code)
        {
            return run.Issues;
        }

        var copy = new ValidationIssue[issues.Count];
        for (var i = 0; i < copy.Length - 1; i++)
        {
            copy[i] = issues[i];
        }

        copy[copy.Length - 1] = new ValidationIssue(
            diagnostic.Code.CoreCode,
            last.Severity,
            last.Message,
            last.Location,
            last.Constraint
        );

        return new ValidationResult(copy, run.Issues.IsTruncated, run.Issues.HasDroppedErrors);
    }

    private DtdPipelineResult ValidateBytes(in DocumentSource source)
    {
        if (source.IsSingleSegment)
        {
            return DtdValidatingPipeline.Validate(source.Bytes, _options, _readerOptions, source.SystemId);
        }

        using var flat = Flatten(source.ByteSequence);

        return DtdValidatingPipeline.Validate(flat.WrittenSpan, _options, _readerOptions, source.SystemId);
    }

    private DtdPipelineResult ValidateChars(in DocumentSource source)
    {
        if (source.IsSingleSegment)
        {
            return DtdValidatingPipeline.Validate(source.Chars, _options, _readerOptions, source.SystemId);
        }

        using var flat = Flatten(source.CharSequence);

        return DtdValidatingPipeline.Validate(flat.WrittenSpan, _options, _readerOptions, source.SystemId);
    }

    // The length is at most MaxInputSize, which the base class checked and which is below the largest array.
    private static PooledBuffer<T> Flatten<T>(in ReadOnlySequence<T> sequence)
    {
        var length = checked((int)sequence.Length);
        var buffer = new PooledBuffer<T>(length);
        sequence.CopyTo(buffer.GetFreeSpan());
        buffer.Advance(length);

        return buffer;
    }
}
