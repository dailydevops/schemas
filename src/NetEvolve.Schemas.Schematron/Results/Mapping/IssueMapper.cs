namespace NetEvolve.Schemas.Schematron.Results.Mapping;

using NetEvolve.Arguments;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Schematron.Diagnostics;
using NetEvolve.Schemas.Schematron.Seams;

/// <summary>Maps fired assertions to issues and forwards every event to the optional caller sink.</summary>
/// <remarks>
/// The mapper is the first consumer of the evaluation events (design #137 AP-11 and AP-27): the evaluator calls
/// <see cref="Map"/> for each fired assertion, builds the <see cref="SchematronFinding"/> with the returned issue, and
/// delivers it through <see cref="Finding"/>. The severity follows the kind only: a failed assert is an error and a
/// successful report a warning; the <c>severity</c>, <c>role</c>, and <c>flag</c> values travel on the finding.
/// </remarks>
internal sealed class IssueMapper : IEvaluationEvents
{
    private readonly ValidationIssueCollector _collector;
    private readonly ISchematronReportSink? _sink;

    internal IssueMapper(ValidationIssueCollector collector, ISchematronReportSink? sink)
    {
        ArgumentNullException.ThrowIfNull(collector);

        _collector = collector;
        _sink = sink;
    }

    /// <inheritdoc />
    public bool WantsLocationPath => _sink is not null;

    /// <summary>Gets a value indicating whether an error was dropped by the issue limit; evaluation stops there (OQ-1).</summary>
    internal bool HasDroppedError { get; private set; }

    /// <summary>Maps one fired assertion to an issue and adds it to the collector.</summary>
    /// <param name="isReport"><see langword="true"/> for a successful report, <see langword="false"/> for a failed assert.</param>
    /// <param name="constraint">The assertion identifier, see <see cref="AssertionIdentifier"/>.</param>
    /// <param name="message">The rendered message.</param>
    /// <param name="location">The instance location of the reported node.</param>
    /// <returns>The issue, or <see langword="null"/> when the collector dropped it.</returns>
    internal ValidationIssue? Map(bool isReport, string constraint, string message, DocumentLocation location)
    {
        ArgumentNullException.ThrowIfNull(constraint);
        ArgumentNullException.ThrowIfNull(message);

        var issue = new ValidationIssue(
            isReport ? SchematronDiagnosticCodes.ReportFired : SchematronDiagnosticCodes.AssertionFailed,
            isReport ? IssueSeverity.Warning : IssueSeverity.Error,
            message,
            location,
            constraint
        );

        var before = _collector.Count;
        _collector.Add(issue);
        if (_collector.Count > before)
        {
            return issue;
        }

        HasDroppedError |= !isReport;
        return null;
    }

    /// <inheritdoc />
    public void Begin(in SchematronRunInfo info) => _sink?.Begin(in info);

    /// <inheritdoc />
    public void ActivePattern(in SchematronPatternEvent pattern) => _sink?.ActivePattern(in pattern);

    /// <inheritdoc />
    public void FiredRule(in SchematronRuleEvent rule) => _sink?.FiredRule(in rule);

    /// <inheritdoc />
    public void Finding(in SchematronFinding finding) => _sink?.Finding(in finding);

    /// <inheritdoc />
    public void ErrorOccurred(in SchematronErrorEvent errorEvent) => _sink?.ErrorOccurred(in errorEvent);

    /// <inheritdoc />
    public void Finish(in SchematronRunSummary summary) => _sink?.Finish(in summary);
}
