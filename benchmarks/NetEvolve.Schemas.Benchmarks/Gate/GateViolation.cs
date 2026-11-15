namespace NetEvolve.Schemas.Benchmarks.Gate;

internal sealed record GateViolation(string Name, GateViolationKind Kind, string Message);
