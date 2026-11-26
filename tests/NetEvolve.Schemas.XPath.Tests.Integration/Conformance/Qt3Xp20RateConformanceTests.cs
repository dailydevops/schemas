namespace NetEvolve.Schemas.XPath.Tests.Integration.Conformance;

using NetEvolve.Schemas.Conformance.Baseline;
using NetEvolve.Schemas.Conformance.Reporting;
using NetEvolve.Schemas.Conformance.Results;
using NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Engine;
using NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Runner;

[Explicit]
[Category(ConformanceCategory.Name)]
public class Qt3Xp20RateConformanceTests
{
    /// <summary>Runs the whole selection once; the applicable and skipped cases are the selected cases, which are the catalog minus the excluded ones. The total is the catalog size pinned by Qt3Xp20SelectionTests, so catalog or exclusion drift makes this fail. The test only runs under the conformance filter.</summary>
    [Test]
    public async Task Selection_ApplicablePlusSkipped_EqualsSelectedMinusExcluded(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var selection = await Qt3Suite.GetSelectionAsync(cancellationToken);
        var engine = await Qt3Engine.GetSharedAsync(cancellationToken);
        var report = ConformanceReport.Compare(
            Qt3Runner.Run(selection.Selected, engine),
            new Dictionary<string, KnownFailureBaseline>(StringComparer.Ordinal)
        );

        var consistent = Qt3Xp20Rate.IsConsistent(
            report.Counts,
            selection.Excluded.Count,
            Qt3Xp20SelectionTests.TotalCases
        );

        await Assert.That(consistent).IsTrue();
    }
}
