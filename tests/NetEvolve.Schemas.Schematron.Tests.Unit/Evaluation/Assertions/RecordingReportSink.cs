namespace NetEvolve.Schemas.Schematron.Tests.Unit.Evaluation.Assertions;

using System.Collections.Generic;

/// <summary>A caller sink that copies the span members of every finding, as a sink must.</summary>
internal sealed class RecordingReportSink : ISchematronReportSink
{
    internal List<RecordedFinding> Findings { get; } = [];

    public void Begin(in SchematronRunInfo info) { }

    public void ActivePattern(in SchematronPatternEvent pattern) { }

    public void FiredRule(in SchematronRuleEvent rule) { }

    public void ErrorOccurred(in SchematronErrorEvent errorEvent) { }

    public void Finish(in SchematronRunSummary summary) { }

    public void Finding(in SchematronFinding finding)
    {
        var diagnostics = new List<(string Id, string Text)>();
        for (var i = 0; i < finding.Diagnostics.Count; i++)
        {
            diagnostics.Add((finding.Diagnostics.IdAt(i), finding.Diagnostics.TextAt(i)));
        }

        var properties = new List<(string Id, string Text)>();
        for (var i = 0; i < finding.Properties.Count; i++)
        {
            properties.Add((finding.Properties.IdAt(i), finding.Properties.TextAt(i)));
        }

        Findings.Add(
            new RecordedFinding(
                finding.IsReport,
                finding.Id.ToString(),
                finding.RuleId.ToString(),
                finding.Constraint.ToString(),
                finding.Test.ToString(),
                finding.Location.ToString(),
                finding.Role.ToString(),
                finding.Flags.ToString(),
                finding.Severity.ToString(),
                finding.Message.ToString(),
                diagnostics,
                properties,
                finding.Issue
            )
        );
    }
}
