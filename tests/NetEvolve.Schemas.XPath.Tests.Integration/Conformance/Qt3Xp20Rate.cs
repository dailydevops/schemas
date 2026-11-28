namespace NetEvolve.Schemas.XPath.Tests.Integration.Conformance;

using NetEvolve.Schemas.Conformance.Results;

/// <summary>The check of the total XPath 2.0 counts: applicable is passed + failed; skipped cases are not applicable.</summary>
internal static class Qt3Xp20Rate
{
    /// <summary>
    /// Returns whether the cases that ran (applicable plus skipped) equal the cases of the catalog minus the excluded cases,
    /// so that no selected case drops out of the rate unnoticed.
    /// </summary>
    public static bool IsConsistent(TestSetCounts counts, int excluded, int totalCases) =>
        counts.Total == totalCases - excluded;
}
