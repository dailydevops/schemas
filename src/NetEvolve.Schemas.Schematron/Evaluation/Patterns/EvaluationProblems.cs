namespace NetEvolve.Schemas.Schematron.Evaluation.Patterns;

using System;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Schematron.Results.Mapping;
using NetEvolve.Schemas.Schematron.Seams;

/// <summary>
/// Turns the problems of the evaluate stage into error issues and pattern-scoped error events (design #137 AP-11 and #150 EP-2).
/// Hand it to every part of a run that reports problems, so the evaluator can see that the issue limit dropped one of them.
/// </summary>
/// <remarks>
/// The event location is <c>/</c>: a problem carries the location of the schema construct, not an instance node. The issue on
/// the event is <see langword="null"/> when the collector dropped it.
/// </remarks>
internal sealed class EvaluationProblems : IProblemSink
{
    private readonly ValidationIssueCollector _collector;
    private readonly IssueMapper _events;
    private readonly string _instance;

    /// <summary>Initializes a new instance of the <see cref="EvaluationProblems"/> class.</summary>
    /// <param name="collector">Receives the issues.</param>
    /// <param name="events">Forwards the error events to the caller sink.</param>
    /// <param name="instanceSystemId">The system identifier of the instance, or <see langword="null"/>.</param>
    internal EvaluationProblems(ValidationIssueCollector collector, IssueMapper events, string? instanceSystemId)
    {
        ArgumentNullException.ThrowIfNull(collector);
        ArgumentNullException.ThrowIfNull(events);

        _collector = collector;
        _events = events;
        _instance = instanceSystemId ?? string.Empty;
    }

    /// <summary>Gets a value indicating whether the collector dropped an issue of this sink; every problem is an error.</summary>
    internal bool HasDroppedError { get; private set; }

    /// <inheritdoc />
    public void Add(in SchematronProblem problem)
    {
        var issue = new ValidationIssue(
            problem.Code,
            IssueSeverity.Error,
            problem.Message,
            problem.Location,
            problem.Constraint
        );

        var before = _collector.Count;
        _collector.Add(issue);
        var kept = _collector.Count > before;
        HasDroppedError |= !kept;

        var code = problem.Code.ToString();
        var errorEvent = new SchematronErrorEvent(
            _instance.AsSpan(),
            "/".AsSpan(),
            code.AsSpan(),
            problem.Message.AsSpan(),
            kept ? issue : null
        );
        _events.ErrorOccurred(in errorEvent);
    }
}
