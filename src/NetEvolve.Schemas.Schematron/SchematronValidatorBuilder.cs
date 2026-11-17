#pragma warning disable NE0002 // Approved namespace NetEvolve.Schemas for format builders (design #103 AP-30, #137 AP-7)

namespace NetEvolve.Schemas;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Resources;
using NetEvolve.Schemas.Schematron;
using NetEvolve.Schemas.Schematron.Seams;

/// <summary>Fluent builder for a Schematron validator: phase, parameters, limits, and resolver. Builders are not thread-safe.</summary>
/// <remarks>
/// Limitation: until work package SCH-55 composes the pipeline, <c>Build</c> and <c>BuildAsync</c> fail with a
/// <see cref="SchemaCompilationException"/> carrying <see cref="SchematronDiagnosticCodes.NotImplemented"/>. A schema
/// source is read at <c>Build</c>, never at <c>ForSchematron</c>.
/// </remarks>
public sealed class SchematronValidatorBuilder
    : SchemaValidatorBuilder<SchematronValidatorBuilder, ISchematronValidator>
{
    private const string DefaultPhaseName = "#DEFAULT";

    private readonly SchemaSource _source;
    private readonly Dictionary<string, string> _parameters = new(StringComparer.Ordinal);
    private string _phase = DefaultPhaseName;
    private ISchematronPipeline _pipeline = NotImplementedPipeline.Instance;

    internal SchematronValidatorBuilder(SchemaSource source) => _source = source;

    /// <summary>Gets the phase: an id, <c>#ALL</c>, or <c>#DEFAULT</c> (the default, meaning <c>defaultPhase</c> or <c>#ALL</c>).</summary>
    public string Phase => _phase;

    /// <summary>Gets a copy of the external parameters. Ordinal keys; the enumeration order is unspecified.</summary>
    public IReadOnlyDictionary<string, string> Parameters => Snapshot(_parameters);

    /// <summary>Sets the phase. The id is checked at <c>Build</c>.</summary>
    /// <param name="phase">A phase id, <c>#ALL</c>, or <c>#DEFAULT</c>.</param>
    /// <returns>This builder.</returns>
    public SchematronValidatorBuilder WithPhase(string phase)
    {
        Argument.ThrowIfNullOrEmpty(phase);

        _phase = phase;
        return this;
    }

    /// <summary>Sets an external parameter; the last call per name wins. The value is a literal string, never an expression.</summary>
    /// <param name="name">The name of a parameter the schema declares; checked at <c>Build</c>.</param>
    /// <param name="value">The value.</param>
    /// <returns>This builder.</returns>
    public SchematronValidatorBuilder WithParameter(string name, string value)
    {
        Argument.ThrowIfNullOrEmpty(name);
        Argument.ThrowIfNull(value);

        _parameters[name] = value;
        return this;
    }

    internal SchematronValidatorBuilder UsePipeline(ISchematronPipeline pipeline)
    {
        Argument.ThrowIfNull(pipeline);

        _pipeline = pipeline;
        return this;
    }

    /// <inheritdoc />
    protected override ISchematronValidator CreateValidator(ValidationLimits limits, IResourceResolver resolver)
    {
        var parameters = Snapshot(_parameters);
        var settings = new BuildSettings(_phase, parameters, limits, resolver);

        return new SchematronValidator(
            _pipeline.Load(_source, settings, CancellationToken.None),
            _phase,
            parameters,
            limits,
            resolver
        );
    }

    private static ReadOnlyDictionary<string, string> Snapshot(Dictionary<string, string> parameters) =>
        new(new Dictionary<string, string>(parameters, StringComparer.Ordinal));
}
