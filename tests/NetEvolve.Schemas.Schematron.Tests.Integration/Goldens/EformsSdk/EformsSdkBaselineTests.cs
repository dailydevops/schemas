namespace NetEvolve.Schemas.Schematron.Tests.Integration.Goldens.EformsSdk;

using NetEvolve.Schemas.Conformance.Baseline;
using NetEvolve.Schemas.Conformance.Results;

public class EformsSdkBaselineTests
{
    /// <summary>Regenerates the baseline files from the run; run it with the explicit filter after a deliberate change.</summary>
    [Test]
    [Explicit]
    public async Task Baseline_Regenerate(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var results = await EformsSdkRun.GetResultsAsync(cancellationToken);

        foreach (var testSet in EformsSdkGoldenTests.BaselineTestSets())
        {
            BaselineFiles.Save(
                EformsSdkRun.BaselineDirectory,
                testSet,
                results
                    .Where(r =>
                        string.Equals(r.TestSet, testSet, StringComparison.Ordinal) && r.Outcome != CaseOutcome.Pass
                    )
                    .Select(r => r.CaseId)
            );
        }

        await Assert.That(results).IsNotEmpty();
    }
}
