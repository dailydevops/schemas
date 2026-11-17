namespace NetEvolve.Schemas.Schematron.Tests.Unit.Fakes;

using System.Collections.Generic;
using NetEvolve.Schemas.Schematron.Seams;

/// <summary>Records every callback in order and copies the span members, as a sink must.</summary>
internal sealed class FakeReportSink : ISchematronReportSink
{
    internal List<string> Events { get; } = [];

    internal string? FindingMessage { get; private set; }

    internal string? FindingConstraint { get; private set; }

    internal ValidationIssue? FindingIssue { get; private set; }

    internal int DiagnosticCount { get; private set; }

    public void Begin(in SchematronRunInfo info) =>
        Events.Add("begin:" + info.Phase.ToString() + ":" + info.InstanceSystemId.ToString());

    public void ActivePattern(in SchematronPatternEvent pattern) =>
        Events.Add("pattern:" + pattern.Id.ToString() + (pattern.IsGroup ? ":group" : string.Empty));

    public void FiredRule(in SchematronRuleEvent rule) => Events.Add("rule:" + rule.Id.ToString());

    public void Finding(in SchematronFinding finding)
    {
        Events.Add((finding.IsReport ? "report:" : "assert:") + finding.Id.ToString());
        FindingMessage = finding.Message.ToString();
        FindingConstraint = finding.Constraint.ToString();
        FindingIssue = finding.Issue;
        DiagnosticCount = finding.Diagnostics.Count;
    }

    public void ErrorOccurred(in SchematronErrorEvent errorEvent) =>
        Events.Add("error:" + errorEvent.Code.ToString() + ":" + errorEvent.Location.ToString());

    public void Finish(in SchematronRunSummary summary) =>
        Events.Add("end:" + summary.IsTruncated + ":" + summary.StopCode);
}
