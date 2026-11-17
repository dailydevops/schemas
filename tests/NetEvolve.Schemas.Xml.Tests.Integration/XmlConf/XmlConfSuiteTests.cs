namespace NetEvolve.Schemas.Xml.Tests.Integration.XmlConf;

using System.Text;
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

    private static async Task<List<XmlConfCase>> WithoutDoctypeAsync(
        IEnumerable<XmlConfCase> candidates,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var cases = new List<XmlConfCase>();
        foreach (var c in candidates)
        {
            var path = Path.Combine(XmlConfSuite.SuiteRoot, c.Uri);
            if (!File.Exists(path))
            {
                continue;
            }

            var bytes = await File.ReadAllBytesAsync(path, cancellationToken);
            var text =
                bytes is [0xFF, 0xFE, ..] ? Encoding.Unicode.GetString(bytes)
                : bytes is [0xFE, 0xFF, ..] ? Encoding.BigEndianUnicode.GetString(bytes)
                : Encoding.Latin1.GetString(bytes);
            if (!text.Contains("<!DOCTYPE", StringComparison.Ordinal))
            {
                cases.Add(c);
            }
        }

        return cases;
    }

    [Test]
    public async Task Baseline_ListsOnlySelectedCases_InOneFilePerTestSet(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var selection = await XmlConfSuite.LoadSelectionAsync(cancellationToken);
        var selected = selection.Selected.ToDictionary(c => c.Id, c => c.TestSet, StringComparer.Ordinal);

        foreach (var file in Directory.GetFiles(XmlConfSuite.BaselineDirectory, "*.txt"))
        {
            var testSet = Path.GetFileNameWithoutExtension(file);
            foreach (var id in BaselineFiles.Load(XmlConfSuite.BaselineDirectory, testSet).Ids)
            {
                await Assert.That(selected.TryGetValue(id, out var owner)).IsTrue();
                await Assert.That(owner).IsEqualTo(testSet);
            }
        }
    }

    [Test]
    public async Task Baseline_MatchesPipelineReaderRun(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var selection = await XmlConfSuite.LoadSelectionAsync(cancellationToken);
        var results = await XmlConfRunner.RunAsync(
            selection,
            new PipelineXmlConfReader(XmlConfSuite.SuiteRoot),
            cancellationToken
        );

        var report = ConformanceReport.Compare(results, XmlConfSuite.LoadBaselines(selection));

        await Assert.That(report.IsSuccess).IsTrue();
        await Assert.That(report.Counts.Passed).IsGreaterThan(0);
        await Assert.That(report.Counts.Skipped).IsEqualTo(ExcludedCaseCount);
    }

    [Test]
    public async Task Pipeline_NamespaceCasesWithoutDoctype_GiveTheExpectedResultAndLeaveTheBaseline(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var selection = await XmlConfSuite.LoadSelectionAsync(cancellationToken);
        var baseline = XmlConfSuite.LoadBaselines(selection)["eduni-namespaces-1.0"];
        var cases = await WithoutDoctypeAsync(
            selection.Selected.Where(c => string.Equals(c.TestSet, "eduni-namespaces-1.0", StringComparison.Ordinal)),
            cancellationToken
        );

        var results = await XmlConfRunner.RunAsync(
            new XmlConfSelection(cases, []),
            new PipelineXmlConfReader(XmlConfSuite.SuiteRoot),
            cancellationToken
        );

        var failed = results.Where(r => r.Outcome == CaseOutcome.Fail).Select(r => r.CaseId).ToList();

        await Assert.That(failed).IsEmpty();
        await Assert.That(results.Count(r => r.Outcome == CaseOutcome.Pass)).IsEqualTo(30);
        await Assert
            .That(
                cases
                    .Where(c => !failed.Contains(c.Id, StringComparer.Ordinal))
                    .Any(c => baseline.Ids.Contains(c.Id, StringComparer.Ordinal))
            )
            .IsFalse();
    }

    /// <summary>Regenerates the baseline files from the failures of the pipeline reader; run it with the explicit filter after a deliberate change.</summary>
    [Test]
    [Explicit]
    public async Task Baseline_Regenerate(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var selection = await XmlConfSuite.LoadSelectionAsync(cancellationToken);
        var results = await XmlConfRunner.RunAsync(
            selection,
            new PipelineXmlConfReader(XmlConfSuite.SuiteRoot),
            cancellationToken
        );

        foreach (var testSet in selection.Selected.Select(c => c.TestSet).Distinct(StringComparer.Ordinal))
        {
            var failures = results
                .Where(r =>
                    string.Equals(r.TestSet, testSet, StringComparison.Ordinal) && r.Outcome == CaseOutcome.Fail
                )
                .Select(r => r.CaseId)
                .ToList();
            if (failures.Count == 0)
            {
                File.Delete(BaselineFiles.PathFor(XmlConfSuite.BaselineDirectory, testSet));
            }
            else
            {
                BaselineFiles.Save(XmlConfSuite.BaselineDirectory, testSet, failures);
            }
        }

        await Assert.That(Directory.GetFiles(XmlConfSuite.BaselineDirectory, "*.txt").Length).IsGreaterThan(0);
    }
}
