namespace NetEvolve.Schemas.Xsd.Tests.Integration.Conformance;

using NetEvolve.Schemas.Conformance.Baseline;
using NetEvolve.Schemas.Conformance.Reporting;
using NetEvolve.Schemas.Conformance.Results;
using NetEvolve.Schemas.Xsd.Tests.Integration.Ubl;

/// <summary>Validates the official sample instances of the pinned OASIS UBL packages; positive cases only.</summary>
[Explicit]
[Category(ConformanceCategory.Name)]
public class UblConformanceTests
{
    [Test]
    [Arguments("2.1")]
    [Arguments("2.2")]
    [Arguments("2.3")]
    [Arguments("2.4")]
    public async Task Samples_AgainstBaseline_HaveNoRegressionAndNoUnexpectedPass(
        string version,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var root = await UblSuite.FetchAsync(version, cancellationToken);
        var results = await UblSuite.RunAsync(version, root, cancellationToken);

        var report = ConformanceReport.Compare(
            results,
            new Dictionary<string, KnownFailureBaseline>(StringComparer.Ordinal)
            {
                [UblSuite.KeyOf(version)] = BaselineFiles.Load(UblSuite.BaselineDirectory, UblSuite.KeyOf(version)),
            }
        );

        await Assert.That(results.Count(r => r.Outcome == CaseOutcome.Pass)).IsGreaterThan(0);
        // Exactly one sample per package has no maindoc schema; a layout change must not shrink the coverage silently.
        await Assert.That(results.Count(r => r.Outcome == CaseOutcome.Skipped)).IsEqualTo(1);
        await Assert.That(report.IsSuccess).IsTrue();
    }
}
