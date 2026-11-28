namespace NetEvolve.Schemas.XPath.Tests.Integration.Conformance;

using NetEvolve.Schemas.Conformance.Baseline;
using NetEvolve.Schemas.Conformance.Reporting;
using NetEvolve.Schemas.Conformance.Results;
using NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Engine;
using NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Runner;

public class Qt3Xp20RateTests
{
    [Test]
    [Arguments(10, 2, 1, 3, 16, true)]
    [Arguments(10, 2, 1, 3, 17, false)]
    [Arguments(10, 2, 1, 3, 15, false)]
    public async Task IsConsistent_ComparesApplicablePlusSkippedWithTotalMinusExcluded(
        int passed,
        int failed,
        int skipped,
        int excluded,
        int totalCases,
        bool expected
    ) =>
        await Assert
            .That(Qt3Xp20Rate.IsConsistent(new TestSetCounts(passed, failed, skipped), excluded, totalCases))
            .IsEqualTo(expected);
}
