#pragma warning disable NE0002 // Approved namespace NetEvolve.Schemas for format builders (design #103 AP-30, #137 AP-7)

namespace NetEvolve.Schemas;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Resources;
using NetEvolve.Schemas.Schematron;
using NetEvolve.Schemas.Schematron.Evaluation.Assertions;
using NetEvolve.Schemas.Schematron.Pipeline;
using NetEvolve.Schemas.Schematron.Seams;

/// <summary>Fluent builder for a Schematron validator: phase, parameters, limits, and resolver. Builders are not thread-safe.</summary>
/// <remarks>
/// <para>
/// A schema source is read at <c>Build</c>, never at <c>ForSchematron</c>: a stream from its current position to its end, never
/// disposed, bounded by <see cref="ValidationLimits.MaxResourceSize"/>, and by <c>BuildAsync</c> asynchronously. The bytes of a
/// stream are kept, so a second <c>Build</c> of one builder does not read the stream again. Every problem of the schema is one
/// <see cref="SchemaCompilationException"/> (SCH-55, #311).
/// </para>
/// </remarks>
public sealed class SchematronValidatorBuilder
    : SchemaValidatorBuilder<SchematronValidatorBuilder, ISchematronValidator>
{
    private const string DefaultPhaseName = "#DEFAULT";

    private readonly SchemaSourceHolder _source;
    private readonly Dictionary<string, string> _parameters = new(StringComparer.Ordinal);
    private string _phase = DefaultPhaseName;
    private bool _globalPatternLets;
    private bool _dtdProcessing;
    private SeverityFilter? _severityFilter;
    private ISchematronPipeline _pipeline = SchematronPipeline.Instance;

    internal SchematronValidatorBuilder(SchemaSourceHolder source) => _source = source;

    /// <summary>Gets the phase: an id, <c>#ALL</c>, or <c>#DEFAULT</c> (the default, meaning <c>defaultPhase</c> or <c>#ALL</c>).</summary>
    public string Phase => _phase;

    /// <summary>Gets a copy of the external parameters. Ordinal keys; the enumeration order is unspecified.</summary>
    public IReadOnlyDictionary<string, string> Parameters => Snapshot(_parameters);

    /// <summary>Sets the phase. The id is checked at <c>Build</c>.</summary>
    /// <param name="phase">A phase id, <c>#ALL</c>, or <c>#DEFAULT</c>.</param>
    /// <returns>This builder.</returns>
    public SchematronValidatorBuilder WithPhase(string phase)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(phase);

        _phase = phase;
        return this;
    }

    /// <summary>Sets an external parameter; the last call per name wins. The value is a literal string, never an expression.</summary>
    /// <param name="name">The name of a parameter the schema declares; checked at <c>Build</c>.</param>
    /// <param name="value">The value.</param>
    /// <returns>This builder.</returns>
    public SchematronValidatorBuilder WithParameter(string name, string value)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(value);

        _parameters[name] = value;
        return this;
    }

    /// <summary>
    /// Selects whether a <see langword="let"/> of a pattern or group is a schema-level variable. Off by default: a pattern <see langword="let"/>
    /// is then lexical, visible only inside its pattern, and it shadows a schema <see langword="let"/> of the same name. When on, the
    /// <see langword="let"/> declarations of the patterns of the selected phase are visible from every pattern, from global
    /// <c>xsl:variable</c> values, and from function bodies, and a pattern <see langword="let"/> whose name is already declared at the
    /// schema level or by another pattern is a build error (<c>SCH-LET-DUPLICATE</c>, ISO/IEC 19757-3:2016 clause 5.4.5).
    /// </summary>
    /// <remarks>
    /// A phase <see langword="let"/> and a rule <see langword="let"/> still shadow a hoisted one. Only the patterns that the selected phase activates
    /// are hoisted. A hoisted value can read the declarations that come before it, a forward reference is <c>SCH-VAR-UNDEFINED</c>.
    /// </remarks>
    /// <param name="enabled"><see langword="true"/> to hoist the pattern-level variables.</param>
    /// <returns>This builder.</returns>
    public SchematronValidatorBuilder WithGlobalPatternLets(bool enabled)
    {
        _globalPatternLets = enabled;
        return this;
    }

    /// <summary>
    /// Selects whether the schema documents may start with a document type declaration whose internal subset declares entities, as
    /// the OASIS XLIFF rule sets do. Off by default: a document type declaration then fails the build with <c>NES1201</c>.
    /// </summary>
    /// <remarks>
    /// Security: only the internal subset is read, an external subset or an external entity is never fetched, and the expansion is
    /// bounded by <see cref="ValidationLimits.MaxEntityExpansion"/> and <see cref="ValidationLimits.MaxEntityDepth"/>, so an
    /// entity bomb fails the build. The switch applies to the schema and to the modules it includes, not to the instance.
    /// </remarks>
    /// <param name="enabled"><see langword="true"/> to expand the internal entities of a schema document.</param>
    /// <returns>This builder.</returns>
    public SchematronValidatorBuilder WithDtdProcessing(bool enabled)
    {
        _dtdProcessing = enabled;
        return this;
    }

    /// <summary>
    /// Keeps only the findings whose <c>severity</c> is in an allow-list (issue #384). A failed <c>assert</c> or a successful
    /// <c>report</c> whose severity is not listed is dropped from the <see cref="ValidationResult"/> and from the SVRL output alike. A
    /// finding without a <c>severity</c> is always kept. There is no filter by default, and the last call wins.
    /// </summary>
    /// <remarks>
    /// The <c>severity</c> is an opaque token without a vocabulary or an order (ISO/IEC 19757-3), so the filter compares text only. The list
    /// is copied; a later change of the caller list has no effect.
    /// </remarks>
    /// <param name="severities">The severities to keep; neither <see langword="null"/>, empty, nor containing a <see langword="null"/> or empty entry.</param>
    /// <param name="comparison">How a finding severity is compared with the list; the default ignores case.</param>
    /// <returns>This builder.</returns>
    public SchematronValidatorBuilder WithSeverityFilter(
        IReadOnlyList<string> severities,
        StringComparison comparison = StringComparison.OrdinalIgnoreCase
    )
    {
        _severityFilter = new SeverityFilter(severities, comparison);
        return this;
    }

    internal SchematronValidatorBuilder UsePipeline(ISchematronPipeline pipeline)
    {
        ArgumentNullException.ThrowIfNull(pipeline);

        _pipeline = pipeline;
        return this;
    }

    /// <inheritdoc />
    protected override ISchematronValidator CreateValidator(ValidationLimits limits, IResourceResolver resolver) =>
        Create(limits, resolver, CancellationToken.None);

    /// <inheritdoc />
    protected override async ValueTask<ISchematronValidator> CreateValidatorAsync(
        ValidationLimits limits,
        IResourceResolver resolver,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        // The stream is read asynchronously and kept first; the stages after it are synchronous and find the bytes.
        await _source.PrefetchAsync(limits, cancellationToken).ConfigureAwait(false);
        return Create(limits, resolver, cancellationToken);
    }

    private SchematronValidator Create(
        ValidationLimits limits,
        IResourceResolver resolver,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var parameters = Snapshot(_parameters);
        var settings = new BuildSettings(_phase, parameters, limits, resolver, _globalPatternLets, _dtdProcessing);

        return new SchematronValidator(
            _pipeline.Load(_source, settings, cancellationToken),
            _phase,
            parameters,
            limits,
            resolver,
            _severityFilter
        );
    }

    private static ReadOnlyDictionary<string, string> Snapshot(Dictionary<string, string> parameters) =>
        new(new Dictionary<string, string>(parameters, StringComparer.Ordinal));
}
