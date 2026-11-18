namespace NetEvolve.Schemas.Schematron.Evaluation.Assertions;

using System;
using NetEvolve.Schemas.Schematron.Diagnostics;
using NetEvolve.Schemas.Schematron.Results.Mapping;
using NetEvolve.Schemas.Schematron.Seams;

/// <summary>Evaluates compiled assertions for the context nodes of one run and delivers their findings. Not thread-safe: one per run.</summary>
internal sealed class AssertionEvaluator : IDisposable
{
    internal AssertionEvaluator(
        IQueryRun run,
        ISlotBinder binder,
        IssueMapper events,
        IProblemSink problems,
        string? instanceSystemId,
        int maxTextLength
    ) => throw NotImplementedFailure.For(GetType().Name);

    /// <summary>Evaluates one assertion for one context node; the finding is delivered when the assertion fires.</summary>
    internal AssertionOutcome Evaluate(CompiledAssertion assertion, in QueryFocus focus) =>
        throw NotImplementedFailure.For(GetType().Name);

    /// <inheritdoc />
    public void Dispose() => throw NotImplementedFailure.For(GetType().Name);
}
