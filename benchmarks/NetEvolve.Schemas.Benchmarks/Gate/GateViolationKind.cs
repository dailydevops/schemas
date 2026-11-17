namespace NetEvolve.Schemas.Benchmarks.Gate;

internal enum GateViolationKind
{
    MissingTarget,
    MissingBaseline,
    AllocationGrowth,
    WorseBaselineRatio,
    Regression,
}
