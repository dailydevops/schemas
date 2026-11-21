namespace NetEvolve.Schemas.Xml.Tests.Integration.XmlConf.Output;

using NetEvolve.Schemas.Conformance.Baseline;
using NetEvolve.Schemas.Conformance.Results;

/// <summary>Runs the canonical output comparison over the pinned W3C XML Conformance Test Suite.</summary>
public class XmlConfOutputSuiteTests
{
    /// <summary>The number of selected cases that have an output file.</summary>
    private const int OutputCaseCount = 387;

    /// <summary>
    /// The number of output cases without a document type declaration: the comparison runs for them. It is zero for the
    /// pinned suite: an output file belongs to a valid case, and a valid document needs a document type declaration.
    /// </summary>
    private const int ActivatedCaseCount = 0;

    /// <summary>The number of output cases left in the baseline: every case with a document type declaration and every case that fails the comparison.</summary>
    private const int BaselinedCaseCount = 387;

    private static string BaselineDirectory { get; } =
        Path.Combine(
            XmlConfSuite.RepositoryRoot,
            "tests",
            "NetEvolve.Schemas.Xml.Tests.Integration",
            "XmlConf",
            "Output",
            "Baselines"
        );

    private static async Task<IReadOnlyList<CaseResult>> RunAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var selection = await XmlConfSuite.LoadSelectionAsync(cancellationToken);
        var outputs = XmlConfOutputReference.Read(XmlConfSuite.SuiteRoot);

        return await XmlConfOutputRunner.RunAsync(selection, outputs, XmlConfSuite.SuiteRoot, cancellationToken);
    }

    private static Dictionary<string, KnownFailureBaseline> LoadBaselines(IEnumerable<CaseResult> results)
    {
        var baselines = new Dictionary<string, KnownFailureBaseline>(StringComparer.Ordinal);
        foreach (var testSet in results.Select(r => r.TestSet).Distinct(StringComparer.Ordinal))
        {
            baselines[testSet] = BaselineFiles.Load(BaselineDirectory, testSet);
        }

        return baselines;
    }

    [Test]
    public async Task Suite_OutputCases_EqualTheCommittedConstants(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var results = await RunAsync(cancellationToken);
        var baselines = LoadBaselines(results);

        await Assert.That(results.Count).IsEqualTo(OutputCaseCount);
        await Assert
            .That(
                results.Count(r =>
                    !string.Equals(r.Reason, XmlConfOutputRunner.NeedsDtdProcessing, StringComparison.Ordinal)
                )
            )
            .IsEqualTo(ActivatedCaseCount);
        await Assert.That(baselines.Values.Sum(b => b.Count)).IsEqualTo(BaselinedCaseCount);
    }

    [Test]
    public async Task Suite_OutputCases_MatchTheBaselineWithoutRegressionsOrUnexpectedPasses(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var results = await RunAsync(cancellationToken);

        var report = ConformanceReport.Compare(results, LoadBaselines(results));

        await Assert.That(report.TestSets.SelectMany(s => s.Regressions)).IsEmpty();
        await Assert.That(report.TestSets.SelectMany(s => s.UnexpectedPasses)).IsEmpty();
        await Assert.That(report.IsSuccess).IsTrue();
        await Assert.That(report.Counts.Failed).IsEqualTo(BaselinedCaseCount);
    }

    [Test]
    public async Task Suite_BaselinedCases_AreLeftOutOnlyBecauseTheyNeedDtdProcessing(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var results = await RunAsync(cancellationToken);
        var baselines = LoadBaselines(results);

        await Assert
            .That(
                results.All(r =>
                    r.Outcome == CaseOutcome.Fail
                    && string.Equals(r.Reason, XmlConfOutputRunner.NeedsDtdProcessing, StringComparison.Ordinal)
                )
            )
            .IsTrue();
        await Assert.That(results.All(r => baselines[r.TestSet].Contains(r.CaseId))).IsTrue();
    }

    /// <summary>Regenerates the output baseline files from the run; run it with the explicit filter after a deliberate change.</summary>
    [Test]
    [Explicit]
    public async Task Baseline_Regenerate(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var results = await RunAsync(cancellationToken);

        foreach (var testSet in results.Select(r => r.TestSet).Distinct(StringComparer.Ordinal))
        {
            var failures = results
                .Where(r =>
                    string.Equals(r.TestSet, testSet, StringComparison.Ordinal) && r.Outcome == CaseOutcome.Fail
                )
                .Select(r => r.CaseId)
                .ToList();
            if (failures.Count == 0)
            {
                File.Delete(BaselineFiles.PathFor(BaselineDirectory, testSet));
            }
            else
            {
                BaselineFiles.Save(BaselineDirectory, testSet, failures);
            }
        }

        await Assert.That(results).IsNotEmpty();
    }
}
