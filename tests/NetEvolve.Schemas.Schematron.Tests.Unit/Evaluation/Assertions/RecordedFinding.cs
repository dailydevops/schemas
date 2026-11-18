namespace NetEvolve.Schemas.Schematron.Tests.Unit.Evaluation.Assertions;

using System.Collections.Generic;

/// <summary>The copied members of one finding.</summary>
internal sealed record RecordedFinding(
    bool IsReport,
    string Id,
    string RuleId,
    string Constraint,
    string Test,
    string Location,
    string Role,
    string Flags,
    string Severity,
    string Message,
    List<(string Id, string Text)> Diagnostics,
    List<(string Id, string Text)> Properties,
    ValidationIssue? Issue
);
