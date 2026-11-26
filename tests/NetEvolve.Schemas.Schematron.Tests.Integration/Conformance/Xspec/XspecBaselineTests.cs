namespace NetEvolve.Schemas.Schematron.Tests.Integration.Conformance.Xspec;

using NetEvolve.Schemas.Conformance.Baseline;
using NetEvolve.Schemas.Conformance.Results;

/// <summary>Kept apart from the explicit conformance class so the category filter of the workflow never runs the regeneration.</summary>
public class XspecBaselineTests
{
    /// <summary>Regenerates the baseline files from the failures of the run; run it with the explicit filter after a deliberate change.</summary>
    [Test]
    [Explicit]
    public async Task Baseline_Regenerate(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var run = await XspecSuite.RunAsync(cancellationToken);
        _ = Directory.CreateDirectory(XspecSuite.BaselineDirectory);

        foreach (var testSet in XspecConformanceTests.TestSets(run))
        {
            var failures = run
                .Results.Where(r =>
                    string.Equals(r.TestSet, testSet, StringComparison.Ordinal) && r.Outcome == CaseOutcome.Fail
                )
                .Select(r => r.CaseId)
                .ToList();
            if (failures.Count == 0)
            {
                File.Delete(BaselineFiles.PathFor(XspecSuite.BaselineDirectory, testSet));
            }
            else
            {
                BaselineFiles.Save(XspecSuite.BaselineDirectory, testSet, failures);
            }
        }
    }
}
