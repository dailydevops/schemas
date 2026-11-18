namespace NetEvolve.Schemas.Schematron.Evaluation.Patterns;

using NetEvolve.Arguments;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Schematron.Diagnostics;
using NetEvolve.Schemas.Schematron.Results.Mapping;
using NetEvolve.Schemas.Schematron.Seams;

/// <summary>Turns the problems of the evaluate stage into error issues and error events.</summary>
internal sealed class EvaluationProblems : IProblemSink
{
    internal EvaluationProblems(ValidationIssueCollector collector, IssueMapper events, string? instanceSystemId) =>
        throw NotImplementedFailure.For(GetType().Name);

    /// <summary>Gets a value indicating whether the collector dropped an error issue of this sink.</summary>
    internal bool HasDroppedError => throw NotImplementedFailure.For(GetType().Name);

    /// <inheritdoc />
    public void Add(in SchematronProblem problem) => throw NotImplementedFailure.For(GetType().Name);
}
