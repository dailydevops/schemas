namespace NetEvolve.Schemas.Schematron;

using System.Collections.Generic;
using System.Threading;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Resources;
using NetEvolve.Schemas.Schematron.Diagnostics;
using NetEvolve.Schemas.Schematron.Evaluation.Assertions;
using NetEvolve.Schemas.Schematron.Model;
using NetEvolve.Schemas.Schematron.Pipeline;
using NetEvolve.Schemas.Schematron.Seams;

internal sealed class SchematronValidator : SchemaValidatorBase, ISchematronValidator
{
    private readonly CompiledSchema _compiled;
    private readonly string _phase;
    private readonly IReadOnlyDictionary<string, string> _parameters;
    private readonly ISchematronReportSink? _sink;
    private readonly SeverityFilter? _severityFilter;

    internal SchematronValidator(
        CompiledSchema compiled,
        string phase,
        IReadOnlyDictionary<string, string> parameters,
        ValidationLimits limits,
        IResourceResolver resolver,
        SeverityFilter? severityFilter = null
    )
        : base(limits, resolver)
    {
        ArgumentNullException.ThrowIfNull(compiled);
        ArgumentNullException.ThrowIfNull(phase);
        ArgumentNullException.ThrowIfNull(parameters);

        _compiled = compiled;
        _phase = phase;
        _parameters = parameters;
        _severityFilter = severityFilter;
    }

    // The view that WithReportSink returns: shares the compiled schema, carries the sink.
    private SchematronValidator(SchematronValidator parent, ISchematronReportSink sink)
        : base(parent.Limits, parent.Resolver)
    {
        _compiled = parent._compiled;
        _phase = parent._phase;
        _parameters = parent._parameters;
        _severityFilter = parent._severityFilter;
        _sink = sink;
    }

    public SchematronSchema Schema => _compiled.Schema;

    public SchematronEdition Edition => _compiled.Schema.Edition;

    public string Phase => _phase;

    public string EffectivePhase => _compiled.EffectivePhase;

    public IReadOnlyDictionary<string, string> Parameters => _parameters;

    ValidationLimits ISchematronValidator.Limits => Limits;

    IResourceResolver ISchematronValidator.Resolver => Resolver;

    internal CompiledSchema Compiled => _compiled;

    internal ISchematronReportSink? ReportSink => _sink;

    public ISchematronValidator WithReportSink(ISchematronReportSink sink)
    {
        ArgumentNullException.ThrowIfNull(sink);

        return new SchematronValidator(this, sink);
    }

    protected override ValidationResult ValidateCore(in DocumentSource source, CancellationToken cancellationToken) =>
        _compiled is CompiledProgram program
            ? ValidationRun.Execute(program, _sink, _parameters, in source, Limits, _severityFilter, cancellationToken)
            : throw NotImplementedFailure.For("evaluation");
}
