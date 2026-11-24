namespace NetEvolve.Schemas.Xml.Tests.Integration.XmlConf.Validating;

using NetEvolve.Schemas.Conformance.Baseline;
using NetEvolve.Schemas.Conformance.Reporting;
using NetEvolve.Schemas.Conformance.Results;

/// <summary>
/// Runs the valid and invalid cases of the pinned W3C XML Conformance Test Suite through the validating reader mode, for
/// every value of the <c>ENTITIES</c> tag (XML-68).
/// </summary>
[Explicit]
[Category(ConformanceCategory.Name)]
[ConformanceLeg]
public class ValidatingXmlConfSuiteTests
{
    /// <summary>The number of valid and invalid cases of the selection, by type and <c>ENTITIES</c> value, with the cases that are not run and the cases that fail.</summary>
    private static readonly (
        XmlConfType Type,
        XmlConfEntities Entities,
        int Cases,
        int Excluded,
        int Failed
    )[] Counts =
    [
        (XmlConfType.Valid, XmlConfEntities.None, 601, 10, 0),
        (XmlConfType.Valid, XmlConfEntities.General, 3, 0, 0),
        (XmlConfType.Valid, XmlConfEntities.Parameter, 78, 0, 0),
        (XmlConfType.Valid, XmlConfEntities.Both, 46, 0, 0),
        (XmlConfType.Invalid, XmlConfEntities.None, 175, 4, 6),
        (XmlConfType.Invalid, XmlConfEntities.General, 6, 0, 0),
        (XmlConfType.Invalid, XmlConfEntities.Parameter, 44, 0, 6),
        (XmlConfType.Invalid, XmlConfEntities.Both, 4, 0, 4),
    ];

    private static async Task<(XmlConfSelection Selection, IReadOnlyList<CaseResult> Results)> RunAsync(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var selection = await XmlConfSuite.LoadSelectionAsync(cancellationToken);
        var results = await ValidatingXmlConfRunner.RunAsync(
            selection,
            XmlConfSuite.SuiteRoot,
            ValidatingXmlConfGaps.Excluded,
            cancellationToken
        );

        return (selection, results);
    }

    private static Dictionary<string, KnownFailureBaseline> Baselines(IEnumerable<CaseResult> results)
    {
        var baselines = new Dictionary<string, KnownFailureBaseline>(StringComparer.Ordinal);
        foreach (var testSet in results.Select(r => r.TestSet).Distinct(StringComparer.Ordinal))
        {
            var ids = ValidatingXmlConfGaps
                .Known.Keys.Where(id => id.StartsWith(testSet + "/", StringComparison.Ordinal))
                .OrderBy(id => id, StringComparer.Ordinal);
            baselines[testSet] = KnownFailureBaseline.Parse(string.Concat(ids.Select(id => id + "\n")));
        }

        return baselines;
    }

    [Test]
    public async Task Suite_ValidAndInvalidCases_PerTypeAndEntities_EqualTheCommittedCounts(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var (selection, results) = await RunAsync(cancellationToken);
        var outcomes = results.ToDictionary(r => r.CaseId, StringComparer.Ordinal);

        foreach (var (type, entities, cases, excluded, failed) in Counts)
        {
            var ids = selection
                .Selected.Where(c => c.Type == type && c.Entities == entities)
                .Select(c => c.Id)
                .ToList();

            await Assert.That(ids.Count).IsEqualTo(cases);
            await Assert.That(ids.Count(id => outcomes[id].Outcome == CaseOutcome.Skipped)).IsEqualTo(excluded);
            await Assert.That(ids.Count(id => outcomes[id].Outcome == CaseOutcome.Fail)).IsEqualTo(failed);
        }

        await Assert.That(results.Count).IsEqualTo(Counts.Sum(c => c.Cases));
    }

    [Test]
    public async Task Suite_ValidCases_ReportNoValidityIssue_ForEveryEntitiesValue(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var (selection, results) = await RunAsync(cancellationToken);
        var valid = selection
            .Selected.Where(c => c.Type == XmlConfType.Valid)
            .Select(c => c.Id)
            .ToHashSet(StringComparer.Ordinal);

        await Assert
            .That(results.Where(r => valid.Contains(r.CaseId) && r.Outcome == CaseOutcome.Fail).Select(r => r.CaseId))
            .IsEmpty();
    }

    [Test]
    public async Task Suite_InvalidCases_ReportAValidityIssueAndNoFatalStatus_ExceptTheKnownGaps(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var (selection, results) = await RunAsync(cancellationToken);
        var invalid = selection
            .Selected.Where(c => c.Type == XmlConfType.Invalid)
            .Select(c => c.Id)
            .ToHashSet(StringComparer.Ordinal);

        await Assert
            .That(results.Where(r => invalid.Contains(r.CaseId) && r.Outcome == CaseOutcome.Fail).Select(r => r.CaseId))
            .IsEquivalentTo(ValidatingXmlConfGaps.Known.Keys.Where(invalid.Contains));
    }

    [Test]
    public async Task Suite_MatchesTheKnownGaps_WithoutRegressionsOrUnexpectedPasses(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var (_, results) = await RunAsync(cancellationToken);

        var report = ConformanceReport.Compare(results, Baselines(results));
        ConformanceRateReporter.Collect(
            "NetEvolve.Schemas.Xml (DTD validation)",
            TargetFrameworkName.Of(typeof(ValidatingXmlConfSuiteTests).Assembly),
            report
        );

        await Assert.That(report.TestSets.SelectMany(s => s.Regressions)).IsEmpty();
        await Assert.That(report.TestSets.SelectMany(s => s.UnexpectedPasses)).IsEmpty();
        await Assert.That(report.IsSuccess).IsTrue();
        await Assert.That(report.Counts.Failed).IsEqualTo(ValidatingXmlConfGaps.Known.Count);
        await Assert.That(report.Counts.Skipped).IsEqualTo(ValidatingXmlConfGaps.Excluded.Count);
    }

    [Test]
    public async Task Gaps_EveryEntryIsASelectedCaseWithAReason_AndNoCaseIsInBothTables(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var selection = await XmlConfSuite.LoadSelectionAsync(cancellationToken);
        var runnable = selection
            .Selected.Where(c => c.Type is XmlConfType.Valid or XmlConfType.Invalid)
            .Select(c => c.Id)
            .ToHashSet(StringComparer.Ordinal);

        await Assert.That(ValidatingXmlConfGaps.Excluded.Keys.All(runnable.Contains)).IsTrue();
        await Assert.That(ValidatingXmlConfGaps.Known.Keys.All(runnable.Contains)).IsTrue();
        await Assert
            .That(ValidatingXmlConfGaps.Excluded.Values.Concat(ValidatingXmlConfGaps.Known.Values))
            .All(reason => !string.IsNullOrWhiteSpace(reason));
        await Assert
            .That(
                ValidatingXmlConfGaps.Excluded.Keys.Intersect(ValidatingXmlConfGaps.Known.Keys, StringComparer.Ordinal)
            )
            .IsEmpty();
    }

    [Test]
    public async Task Gaps_EveryCaseThatNeedsNamespaceProcessingOff_IsExcluded(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var selection = await XmlConfSuite.LoadSelectionAsync(cancellationToken);

        await Assert
            .That(
                selection
                    .Selected.Where(c => c.Type is XmlConfType.Valid or XmlConfType.Invalid && !c.Namespaces)
                    .Select(c => c.Id)
                    .Where(id => !ValidatingXmlConfGaps.Excluded.ContainsKey(id))
            )
            .IsEmpty();
    }
}
