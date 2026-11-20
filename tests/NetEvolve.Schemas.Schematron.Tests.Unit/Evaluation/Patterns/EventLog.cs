namespace NetEvolve.Schemas.Schematron.Tests.Unit.Evaluation.Patterns;

using System.Collections.Generic;
using NetEvolve.Schemas.Schematron.Tests.Unit.Evaluation.Assertions;

/// <summary>A caller sink that copies what the evaluator delivers into plain lists, as a sink must for span members.</summary>
internal sealed class EventLog : ISchematronReportSink
{
    private readonly RecordingReportSink _details = new();

    /// <summary>Gets every member of every finding in order, role, flags, and severity included.</summary>
    internal List<RecordedFinding> Details => _details.Findings;

    /// <summary>Gets the role of every active pattern or group in order.</summary>
    internal List<string> PatternRoles { get; } = [];

    /// <summary>Gets the delivered events in order: <c>pattern:id</c>, <c>group:id</c>, <c>fired:id</c>, <c>failed:text</c>, <c>report:text</c>, <c>error:code</c>, <c>begin</c>, <c>finish</c>.</summary>
    internal List<string> Events { get; } = [];

    internal List<(string Id, string Documents, string Name, bool IsGroup)> Patterns { get; } = [];

    internal List<(string Id, string Context, string Role, string Flags, string Document)> Rules { get; } = [];

    internal List<(string Code, string Message, string Location, bool HasIssue)> Errors { get; } = [];

    /// <summary>Gets the events joined with one space.</summary>
    internal string Sequence => string.Join(" ", Events);

    /// <summary>Gets the ids of the fired rules in order.</summary>
    internal string Fired => string.Join(",", Rules.ConvertAll(r => r.Id));

    internal List<string> Findings { get; } = [];

    /// <summary>Gets the messages of the failed asserts and successful reports in order.</summary>
    internal string Messages => string.Join(",", Findings);

    public void Begin(in SchematronRunInfo info) => Events.Add("begin");

    public void ActivePattern(in SchematronPatternEvent pattern)
    {
        Events.Add((pattern.IsGroup ? "group:" : "pattern:") + pattern.Id.ToString());
        Patterns.Add((pattern.Id.ToString(), pattern.Documents.ToString(), pattern.Name.ToString(), pattern.IsGroup));
        PatternRoles.Add(pattern.Role.ToString());
    }

    public void FiredRule(in SchematronRuleEvent rule)
    {
        Events.Add("fired:" + rule.Id.ToString());
        Rules.Add(
            (
                rule.Id.ToString(),
                rule.Context.ToString(),
                rule.Role.ToString(),
                rule.Flags.ToString(),
                rule.Document.ToString()
            )
        );
    }

    public void Finding(in SchematronFinding finding)
    {
        Events.Add((finding.IsReport ? "report:" : "failed:") + finding.Message.ToString());
        Findings.Add(finding.Message.ToString());
        _details.Finding(in finding);
    }

    public void ErrorOccurred(in SchematronErrorEvent errorEvent)
    {
        Events.Add("error:" + errorEvent.Code.ToString());
        Errors.Add(
            (
                errorEvent.Code.ToString(),
                errorEvent.Message.ToString(),
                errorEvent.Location.ToString(),
                errorEvent.Issue is not null
            )
        );
    }

    public void Finish(in SchematronRunSummary summary) => Events.Add("finish");
}
