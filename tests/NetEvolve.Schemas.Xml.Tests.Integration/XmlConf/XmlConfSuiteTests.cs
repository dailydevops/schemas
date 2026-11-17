namespace NetEvolve.Schemas.Xml.Tests.Integration.XmlConf;

using NetEvolve.Schemas.Conformance.Baseline;
using NetEvolve.Schemas.Conformance.Results;

public class XmlConfSuiteTests
{
    /// <summary>The number of cases the pinned suite selects; a new release or a filter change moves it on purpose.</summary>
    private const int SelectedCaseCount = 2001;

    /// <summary>The number of cases the filter drops from the pinned suite.</summary>
    private const int ExcludedCaseCount = 584;

    [Test]
    public async Task Suite_SelectedCaseCount_EqualsCommittedConstant(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var selection = await XmlConfSuite.LoadSelectionAsync(cancellationToken);

        await Assert.That(selection.Selected.Count).IsEqualTo(SelectedCaseCount);
        await Assert.That(selection.Excluded.Count).IsEqualTo(ExcludedCaseCount);
    }

    [Test]
    public async Task Suite_SelectedCases_CarryEveryEntitiesTag(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var selection = await XmlConfSuite.LoadSelectionAsync(cancellationToken);

        await Assert
            .That(selection.Selected.Select(c => c.Entities).Distinct())
            .IsEquivalentTo([
                XmlConfEntities.None,
                XmlConfEntities.General,
                XmlConfEntities.Parameter,
                XmlConfEntities.Both,
            ]);
    }

    [Test]
    public async Task Suite_ExcludedCases_AreOnlyXml11AndOldEditionCases(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var selection = await XmlConfSuite.LoadSelectionAsync(cancellationToken);

        await Assert
            .That(selection.Selected.Any(c => c.Recommendation.Contains("1.1", StringComparison.Ordinal)))
            .IsFalse();
        await Assert.That(selection.Excluded.All(e => !string.IsNullOrWhiteSpace(e.Reason))).IsTrue();
    }

    [Test]
    public async Task Baseline_ListsEverySelectedCase_InOneFilePerTestSet(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var selection = await XmlConfSuite.LoadSelectionAsync(cancellationToken);

        var expected = selection
            .Selected.GroupBy(c => c.TestSet, StringComparer.Ordinal)
            .ToDictionary(
                g => g.Key,
                g => g.Select(c => c.Id).Order(StringComparer.Ordinal).ToArray(),
                StringComparer.Ordinal
            );
        var files = Directory
            .GetFiles(XmlConfSuite.BaselineDirectory, "*.txt")
            .Select(f => Path.GetFileNameWithoutExtension(f))
            .Order(StringComparer.Ordinal);

        await Assert.That(files).IsEquivalentTo(expected.Keys.Order(StringComparer.Ordinal));
        foreach (var (testSet, ids) in expected)
        {
            await Assert.That(BaselineFiles.Load(XmlConfSuite.BaselineDirectory, testSet).Ids).IsEquivalentTo(ids);
        }
    }

    [Test]
    public async Task Baseline_MatchesUnboundReaderRun(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var selection = await XmlConfSuite.LoadSelectionAsync(cancellationToken);
        var results = await XmlConfRunner.RunAsync(selection, UnboundXmlConfReader.Instance, cancellationToken);

        var report = ConformanceReport.Compare(results, XmlConfSuite.LoadBaselines(selection));

        await Assert.That(report.IsSuccess).IsTrue();
        await Assert.That(report.Counts.Passed).IsEqualTo(0);
        await Assert.That(report.Counts.Failed).IsEqualTo(SelectedCaseCount);
        await Assert.That(report.Counts.Skipped).IsEqualTo(ExcludedCaseCount);
    }

    /// <summary>Regenerates the baseline files from the current selection; run it with the explicit filter after a deliberate change.</summary>
    [Test]
    [Explicit]
    public async Task Baseline_Regenerate(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var selection = await XmlConfSuite.LoadSelectionAsync(cancellationToken);

        foreach (var group in selection.Selected.GroupBy(c => c.TestSet, StringComparer.Ordinal))
        {
            BaselineFiles.Save(XmlConfSuite.BaselineDirectory, group.Key, group.Select(c => c.Id));
        }

        await Assert.That(Directory.GetFiles(XmlConfSuite.BaselineDirectory, "*.txt").Length).IsGreaterThan(0);
    }
}
