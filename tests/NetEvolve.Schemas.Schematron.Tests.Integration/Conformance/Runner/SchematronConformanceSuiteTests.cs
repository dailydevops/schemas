namespace NetEvolve.Schemas.Schematron.Tests.Integration.Conformance.Runner;

using NetEvolve.Schemas.Conformance.Reporting;

[Explicit]
[Category(ConformanceCategory.Name)]
[ConformanceLeg]
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
}
