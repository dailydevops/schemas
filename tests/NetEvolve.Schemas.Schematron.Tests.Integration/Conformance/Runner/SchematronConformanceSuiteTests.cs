namespace NetEvolve.Schemas.Schematron.Tests.Integration.Conformance.Runner;

using NetEvolve.Schemas.Conformance.Baseline;
using NetEvolve.Schemas.Conformance.Reporting;
using NetEvolve.Schemas.Conformance.Results;

public class SchematronConformanceSuiteTests
{
    /// <summary>The number of cases the pinned suite release selects; a new release changes it on purpose.</summary>
    private const int SelectedCaseCount = 84;

    [Test]
    public async Task Suite_SelectedCaseCount_EqualsCommittedConstant(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var selection = await SchematronSuite.LoadSelectionAsync(cancellationToken);

        await Assert.That(selection.Selected.Count).IsEqualTo(SelectedCaseCount);
        await Assert.That(selection.Excluded.Count).IsEqualTo(3);
    }

    [Test]
    public async Task Baseline_ListsEverySelectedCase_InOneFilePerTestSet(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var selection = await SchematronSuite.LoadSelectionAsync(cancellationToken);

        var expected = selection
            .Selected.GroupBy(c => c.TestSet, StringComparer.Ordinal)
            .ToDictionary(
                g => g.Key,
                g => g.Select(c => c.Id).Order(StringComparer.Ordinal).ToArray(),
                StringComparer.Ordinal
            );
        var files = Directory
            .GetFiles(SchematronSuite.BaselineDirectory, "*.txt")
            .Select(f => Path.GetFileNameWithoutExtension(f))
            .Order(StringComparer.Ordinal);

        await Assert.That(files).IsEquivalentTo(expected.Keys.Order(StringComparer.Ordinal));
        foreach (var (testSet, ids) in expected)
        {
            var baseline = BaselineFiles.Load(SchematronSuite.BaselineDirectory, testSet);
            await Assert.That(baseline.Ids).IsEquivalentTo(ids);
        }
    }

    [Test]
    [Arguments("iso-instance-document-selection")]
    [Arguments("iso-pattern-templating")]
    [Arguments("iso-reporting")]
    [Arguments("iso-rule-based-validation")]
    [Arguments("iso-schema-composition")]
    public async Task TestSet_AgainstBaseline_HasNoRegressionAndNoUnexpectedPass(
        string testSet,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var selection = await SchematronSuite.LoadSelectionAsync(cancellationToken);
        var results = await SchematronConformanceRunner.RunAsync(
            selection,
            UnboundSchematronValidator.Instance,
            cancellationToken
        );

        var report = ConformanceReport.Compare(
            results.Where(r => string.Equals(r.TestSet, testSet, StringComparison.Ordinal)),
            new Dictionary<string, KnownFailureBaseline>(StringComparer.Ordinal)
            {
                [testSet] = BaselineFiles.Load(SchematronSuite.BaselineDirectory, testSet),
            }
        );

        ConformanceRateReporter.Append(
            "NetEvolve.Schemas.Schematron",
            TargetFrameworkName.Of(typeof(SchematronConformanceSuiteTests).Assembly),
            report
        );

        await Assert.That(report.TestSets).HasSingleItem();
        await Assert.That(report.IsSuccess).IsTrue();
    }
}
