namespace NetEvolve.Schemas.Schematron.Results.Mapping;

using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Schematron.Diagnostics;
using NetEvolve.Schemas.Schematron.Seams;

/// <summary>Maps fired assertions to issues and forwards every event to the optional caller sink.</summary>
internal sealed class IssueMapper : IEvaluationEvents
{
    private readonly ValidationIssueCollector _collector;
    private readonly ISchematronReportSink? _sink;

    internal IssueMapper(ValidationIssueCollector collector, ISchematronReportSink? sink)
    {
        _collector = collector;
        _sink = sink;
    }

    public bool WantsLocationPath => Fail<bool>();

    internal bool HasDroppedError => Fail<bool>();

    internal ValidationIssue? Map(bool isReport, string constraint, string message, DocumentLocation location) =>
        Fail<ValidationIssue?>(isReport, constraint, message, location);

    public void Begin(in SchematronRunInfo info) => Fail<bool>();

    public void ActivePattern(in SchematronPatternEvent pattern) => Fail<bool>();

    public void FiredRule(in SchematronRuleEvent rule) => Fail<bool>();

    public void Finding(in SchematronFinding finding) => Fail<bool>();

    public void ErrorOccurred(in SchematronErrorEvent errorEvent) => Fail<bool>();

    public void Finish(in SchematronRunSummary summary) => Fail<bool>();

    private T Fail<T>(params object?[] unused) =>
        throw NotImplementedFailure.For("issue mapper " + unused.Length + _collector.Count + (_sink is null));
}
