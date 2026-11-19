namespace NetEvolve.Schemas.Xml.Tests.Integration.XmlConf;

using System.Text;
using NetEvolve.Schemas.Conformance.Baseline;
using NetEvolve.Schemas.Conformance.Reporting;
using NetEvolve.Schemas.Conformance.Results;

[ConformanceLeg]
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

    /// <summary>Not-well-formed cases whose document is absent from the pinned archive; they cannot be read and belong to XML-11 (#166).</summary>
    private static readonly string[] MissingNotWellFormedDocuments =
    [
        "eduni-namespaces-misc/hst-bh-001",
        "eduni-namespaces-misc/hst-bh-002",
        "eduni-namespaces-misc/hst-bh-003",
        "eduni-namespaces-misc/hst-bh-004",
        "eduni-namespaces-misc/hst-lhs-007",
        "eduni-namespaces-misc/hst-lhs-008",
        "eduni-namespaces-misc/hst-lhs-009",
    ];

    [Test]
    public async Task Pipeline_NotWellFormedCasesWithoutDoctype_AreAllRejectedAndLeaveTheBaseline(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var selection = await XmlConfSuite.LoadSelectionAsync(cancellationToken);
        var baselines = XmlConfSuite.LoadBaselines(selection);
        var notWellFormed = selection.Selected.Where(c => c.Type == XmlConfType.NotWellFormed).ToList();
        var cases = await WithoutDoctypeAsync(notWellFormed, cancellationToken);

        var results = await XmlConfRunner.RunAsync(
            new XmlConfSelection(cases, []),
            new PipelineXmlConfReader(XmlConfSuite.SuiteRoot),
            cancellationToken
        );

        var failed = results.Where(r => r.Outcome == CaseOutcome.Fail).Select(r => r.CaseId).ToList();
        var missing = notWellFormed
            .Where(c => !File.Exists(Path.Combine(XmlConfSuite.SuiteRoot, c.Uri)))
            .Select(c => c.Id)
            .ToList();

        await Assert.That(cases).IsNotEmpty();
        await Assert.That(failed).IsEmpty();
        await Assert.That(cases.Any(c => baselines[c.TestSet].Ids.Contains(c.Id, StringComparer.Ordinal))).IsFalse();
        await Assert.That(missing).IsEquivalentTo(MissingNotWellFormedDocuments);
    }

    /// <summary>
    /// Not-well-formed xmltest cases that break a constraint of entity references or of entity replacement text
    /// (Entity Declared, No Recursion, Parsed Entity, No External Entity References, content of replacement text).
    /// Cases about attribute-list defaults (078, 079, 080, 082, 084, 180) are XML-16 and not listed.
    /// </summary>
    private static readonly string[] NotWellFormedEntityCases =
    [
        "xmltest/not-wf-sa-071",
        "xmltest/not-wf-sa-072",
        "xmltest/not-wf-sa-073",
        "xmltest/not-wf-sa-074",
        "xmltest/not-wf-sa-075",
        "xmltest/not-wf-sa-076",
        "xmltest/not-wf-sa-077",
        "xmltest/not-wf-sa-081",
        "xmltest/not-wf-sa-083",
        "xmltest/not-wf-sa-088",
        "xmltest/not-wf-sa-090",
        "xmltest/not-wf-sa-092",
        "xmltest/not-wf-sa-104",
        "xmltest/not-wf-sa-110",
        "xmltest/not-wf-sa-111",
        "xmltest/not-wf-sa-115",
        "xmltest/not-wf-sa-116",
        "xmltest/not-wf-sa-117",
        "xmltest/not-wf-sa-118",
        "xmltest/not-wf-sa-119",
        "xmltest/not-wf-sa-120",
        "xmltest/not-wf-sa-181",
        "xmltest/not-wf-sa-182",
        "xmltest/not-wf-sa-185",
    ];

    [Test]
    public async Task Pipeline_NotWellFormedEntityCases_AreRejectedAndLeaveTheBaseline(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var selection = await XmlConfSuite.LoadSelectionAsync(cancellationToken);
        var baselines = XmlConfSuite.LoadBaselines(selection);
        var cases = selection.Selected.Where(c => NotWellFormedEntityCases.Contains(c.Id)).ToList();

        var results = await XmlConfRunner.RunAsync(
            new XmlConfSelection(cases, []),
            new PipelineXmlConfReader(XmlConfSuite.SuiteRoot),
            cancellationToken
        );

        await Assert.That(cases.Select(c => c.Id)).IsEquivalentTo(NotWellFormedEntityCases);
        await Assert.That(results.Where(r => r.Outcome == CaseOutcome.Fail).Select(r => r.CaseId)).IsEmpty();
        await Assert.That(cases.Any(c => baselines[c.TestSet].Ids.Contains(c.Id, StringComparer.Ordinal))).IsFalse();
    }

    /// <summary>Not-well-formed cases whose fault is in the external subset (conditional sections, references) or in an external general entity (a self-reference, a text declaration with <c>standalone</c>, a second text declaration).</summary>
    private static readonly string[] NotWellFormedExternalSubsetCases =
    [
        "xmltest/not-wf-not-sa-001",
        "xmltest/not-wf-not-sa-002",
        "xmltest/not-wf-not-sa-003",
        "xmltest/not-wf-not-sa-004",
        "xmltest/not-wf-not-sa-006",
        "xmltest/not-wf-not-sa-007",
        "xmltest/not-wf-not-sa-008",
        "xmltest/not-wf-not-sa-009",
        "xmltest/not-wf-ext-sa-001",
        "xmltest/not-wf-ext-sa-002",
        "xmltest/not-wf-ext-sa-003",
    ];

    [Test]
    public async Task Pipeline_NotWellFormedExternalSubsetCases_AreRejectedAndLeaveTheBaseline(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var selection = await XmlConfSuite.LoadSelectionAsync(cancellationToken);
        var baselines = XmlConfSuite.LoadBaselines(selection);
        var cases = selection.Selected.Where(c => NotWellFormedExternalSubsetCases.Contains(c.Id)).ToList();

        var results = await XmlConfRunner.RunAsync(
            new XmlConfSelection(cases, []),
            new PipelineXmlConfReader(XmlConfSuite.SuiteRoot),
            cancellationToken
        );

        await Assert.That(cases.Select(c => c.Id)).IsEquivalentTo(NotWellFormedExternalSubsetCases);
        await Assert.That(results.Where(r => r.Outcome == CaseOutcome.Fail).Select(r => r.CaseId)).IsEmpty();
        await Assert.That(cases.Any(c => baselines[c.TestSet].Ids.Contains(c.Id, StringComparer.Ordinal))).IsFalse();
    }

    /// <summary>
    /// Valid cases without a document type declaration that need namespace processing switched off; the pipeline reader
    /// has no such mode (limitation), so they stay in the baseline.
    /// </summary>
    private static readonly string[] NamespaceProcessingOffCases = ["oasis/o-p04pass1", "oasis/o-p05pass1"];

    [Test]
    public async Task Pipeline_ValidAndInvalidCasesWithoutDoctype_AreAcceptedAndLeaveTheBaseline(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var selection = await XmlConfSuite.LoadSelectionAsync(cancellationToken);
        var baselines = XmlConfSuite.LoadBaselines(selection);
        var candidates = await WithoutDoctypeAsync(
            selection.Selected.Where(c => c.Type is XmlConfType.Valid or XmlConfType.Invalid),
            cancellationToken
        );
        var cases = candidates.Where(c => c.Namespaces).ToList();

        var results = await XmlConfRunner.RunAsync(
            new XmlConfSelection(cases, []),
            new PipelineXmlConfReader(XmlConfSuite.SuiteRoot),
            cancellationToken
        );

        await Assert.That(results.Where(r => r.Outcome == CaseOutcome.Fail).Select(r => r.CaseId)).IsEmpty();
        await Assert.That(cases.Any(c => baselines[c.TestSet].Ids.Contains(c.Id, StringComparer.Ordinal))).IsFalse();
        await Assert
            .That(candidates.Where(c => !c.Namespaces).Select(c => c.Id))
            .IsEquivalentTo(NamespaceProcessingOffCases);
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
