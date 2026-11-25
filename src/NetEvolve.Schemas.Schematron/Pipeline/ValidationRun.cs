namespace NetEvolve.Schemas.Schematron.Pipeline;

using System;
using System.Collections.Generic;
using System.Threading;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Schematron.Evaluation.Assertions;
using NetEvolve.Schemas.Schematron.Evaluation.Patterns;
using NetEvolve.Schemas.Schematron.Results.Mapping;
using NetEvolve.Schemas.Schematron.Scopes;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Xslt.Functions;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Text.NameTable;
using NetEvolve.Schemas.XPath.Xdm.Nodes;

/// <summary>
/// One validation call of a compiled schema (SCH-55, #311, design #150 EP-1): the instance is read into a tree, the run of the binding
/// is begun, every active pattern is evaluated in one pass over the tree, and the events go to the issue mapper first and to the caller
/// sink after it. The result is the issues the collector kept.
/// </summary>
/// <remarks>
/// <para>
/// The run owns everything that changes: the collector, the run of the binding with its variable values, the slot binder, and the
/// evaluators. The compiled schema is read and never written, so concurrent calls on one validator do not meet.
/// </para>
/// <para>
/// An instance that is not a tree gives the events <c>Begin</c>, <c>ErrorOccurred</c>, and <c>Finish</c> and one issue, and no rule runs
/// (design #137 AP-16). A schema without an active pattern gives <c>Begin</c>, an <c>ErrorOccurred</c> with
/// <see cref="SchematronDiagnosticCodes.NoActivePattern"/> and no issue, and <c>Finish</c>. The cancellation token ends the run with an
/// <see cref="OperationCanceledException"/> and no <c>Finish</c>.
/// </para>
/// </remarks>
internal static class ValidationRun
{
    // The longest message of a finding, in characters: the text node limit of the Xml package.
    private static readonly int MaxMessageLength = XmlReaderLimits.Default.MaxTextNodeLength;

    /// <summary>Validates an instance.</summary>
    /// <param name="program">The compiled schema.</param>
    /// <param name="sink">The caller sink, or <see langword="null"/> for none.</param>
    /// <param name="parameters">The external parameters of the builder.</param>
    /// <param name="source">The instance.</param>
    /// <param name="limits">The limits of the validator.</param>
    /// <param name="cancellationToken">The token of the call.</param>
    /// <returns>The result.</returns>
    /// <exception cref="OperationCanceledException">The token was canceled.</exception>
    internal static ValidationResult Execute(
        CompiledProgram program,
        ISchematronReportSink? sink,
        IReadOnlyDictionary<string, string> parameters,
        in DocumentSource source,
        ValidationLimits limits,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(program);
        ArgumentNullException.ThrowIfNull(parameters);
        ArgumentNullException.ThrowIfNull(limits);

        var collector = new ValidationIssueCollector(limits);
        var events = new IssueMapper(collector, sink);
        var systemId = source.SystemId;
        Begin(program, events, systemId);
        var context = program.Contexts.Rent();
        try
        {
            return Run(program, context, collector, events, parameters, in source, limits, cancellationToken);
        }
        finally
        {
            program.Contexts.Return(context);
        }
    }

    private static ValidationResult Run(
        CompiledProgram program,
        ValidationContext context,
        ValidationIssueCollector collector,
        IssueMapper events,
        IReadOnlyDictionary<string, string> parameters,
        in DocumentSource source,
        ValidationLimits limits,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var systemId = source.SystemId;
        if (!InstanceReader.TryRead(in source, limits, context.Names, out var tree, out var failure))
        {
            Fail(collector, events, failure, systemId);
            return collector.ToResult();
        }

        var document = new XPathNode(tree.Document);
        using var run = program.Binding.BeginRun(program.Resources, cancellationToken);
        var problems = new EvaluationProblems(collector, events, systemId);
        var binder = new SlotBinder(program.Plan, run, new QueryFocus(document, 1, 1), problems);
        if (program.XsltTables is { Functions.Count: > 0 } tables && run is IXsltFunctionRun functionRun)
        {
            // The slots of the functions lie above every slot of the plan, so the run grows its slot table on the first bind.
            var caller = new XsltFunctionCaller(
                functionRun,
                binder,
                new SpanInterningNameTable(XmlReaderLimits.Default.MaxDistinctNames),
                cancellationToken
            );
            functionRun.FunctionCalls = new XsltFunctionHost(tables.Functions, caller, functionRun);
        }

        foreach (var parameter in parameters)
        {
            _ = binder.SetParameter(parameter.Key, parameter.Value);
        }

        using var assertions = new AssertionEvaluator(run, binder, events, problems, systemId, MaxMessageLength);
        var evaluator = new PatternEvaluator(
            program.Matcher,
            run,
            binder,
            assertions,
            events,
            problems,
            cancellationToken
        );
        var summary =
            program.Sets.Count == 0
                ? NoActivePattern(events, systemId)
                : evaluator.Evaluate(program.Sets, document, program.Sets.Count > 1 ? program.Dispatch : null);
        events.Finish(in summary);
        return collector.ToResult();
    }

    private static void Begin(CompiledProgram program, IssueMapper events, string? systemId)
    {
        var schema = program.Schema;
        var info = new SchematronRunInfo(
            program.Title.AsSpan(),
            program.EffectivePhase.AsSpan(),
            (schema.SchemaVersion ?? string.Empty).AsSpan(),
            schema.Edition,
            schema.Namespaces,
            (systemId ?? string.Empty).AsSpan()
        );
        events.Begin(in info);
    }

    private static void Fail(
        ValidationIssueCollector collector,
        IssueMapper events,
        ValidationIssue failure,
        string? systemId
    )
    {
        collector.Add(failure);
        var code = failure.Code.ToString();
        var error = new SchematronErrorEvent(
            (systemId ?? string.Empty).AsSpan(),
            "/".AsSpan(),
            code.AsSpan(),
            failure.Message.AsSpan(),
            failure
        );
        events.ErrorOccurred(in error);
        events.Finish(new SchematronRunSummary(false, default));
    }

    private static SchematronRunSummary NoActivePattern(IssueMapper events, string? systemId)
    {
        var code = SchematronDiagnosticCodes.NoActivePattern.ToString();
        var error = new SchematronErrorEvent(
            (systemId ?? string.Empty).AsSpan(),
            "/".AsSpan(),
            code.AsSpan(),
            "The run evaluated no pattern: the phase activates none.".AsSpan(),
            null
        );
        events.ErrorOccurred(in error);
        return new SchematronRunSummary(false, default);
    }
}
