namespace NetEvolve.Schemas.Schematron.Tests.Unit.Evaluation.Patterns;

/// <summary>The two members of the run summary, which is a ref struct and cannot leave the harness.</summary>
internal readonly record struct PatternRunResult(bool IsTruncated, DiagnosticCode StopCode);
