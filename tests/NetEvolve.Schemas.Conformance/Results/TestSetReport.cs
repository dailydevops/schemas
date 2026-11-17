namespace NetEvolve.Schemas.Conformance.Results;

/// <summary>The comparison of one test set against its known-failure baseline.</summary>
/// <param name="TestSet">The name of the test set.</param>
/// <param name="Counts">The case counts of the test set.</param>
/// <param name="Regressions">The sorted ids of cases that fail but are not in the baseline.</param>
/// <param name="UnexpectedPasses">The sorted ids of cases that pass but are in the baseline.</param>
public sealed record TestSetReport(
    string TestSet,
    TestSetCounts Counts,
    IReadOnlyList<string> Regressions,
    IReadOnlyList<string> UnexpectedPasses
);
