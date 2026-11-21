namespace NetEvolve.Schemas.Xsd.Tests.Integration.Conformance;

using System.Collections.Concurrent;
using NetEvolve.Schemas.Conformance.Baseline;
using NetEvolve.Schemas.Conformance.Reporting;
using NetEvolve.Schemas.Conformance.Results;
using NetEvolve.Schemas.Xsd.Tests.Integration.SchemaTests;
using NetEvolve.Schemas.Xsd.Tests.Integration.Xsts;

[ConformanceLeg]
public class XstsConformanceTests
{
    private static readonly ConcurrentBag<CaseResult> ChunkResults = [];

    private static readonly ConcurrentBag<string> SchemaCaseKeys = [];

    /// <summary>The number of cases the pinned suite selects for XSD 1.0; a new pin changes it on purpose.</summary>
    private const int SelectedCaseCount10 = 39271;

    /// <summary>The number of cases the pinned suite selects for XSD 1.1; a new pin changes it on purpose.</summary>
    private const int SelectedCaseCount11 = 41389;

    /// <summary>The number of cases excluded because they need the precisionDecimal datatype (DEF-52, #64).</summary>
    private const int PrecisionDecimalExcludedCount = 122;

    /// <summary>The number of selected cases that one set test runs (decision P6 of the conformance spike).</summary>
    private const int SetSize = 100;

    /// <summary>
    /// The chunks of every test set. The sizes come from the committed manifest, so discovery needs no download; the baselines list
    /// failures only and cannot give them. <c>Suite_TestSetSizes_EqualTheCommittedManifest</c> pins the manifest to the suite.
    /// </summary>
    public static IEnumerable<Func<(string TestSet, int Chunk)>> Chunks()
    {
        foreach (var (testSet, count) in TestSetSizes.Load())
        {
            for (var chunk = 0; chunk * SetSize < count; chunk++)
            {
                var captured = (testSet, chunk);
                yield return () => captured;
            }
        }
    }

    [Test]
    public async Task Suite_TestSetSizes_EqualTheCommittedManifest(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var selection = await XstsSuite.LoadSelectionAsync(cancellationToken);

        var sizes = selection
            .Selected.GroupBy(c => c.TestSet, StringComparer.Ordinal)
            .Select(g => (g.Key, g.Count()))
            .OrderBy(s => s.Key, StringComparer.Ordinal);

        await Assert.That(sizes).IsEquivalentTo(TestSetSizes.Load());
    }

    [Test]
    [Arguments(XstsVersions.V10, SelectedCaseCount10)]
    [Arguments(XstsVersions.V11, SelectedCaseCount11)]
    public async Task Suite_SelectedCaseCountPerVersion_EqualsCommittedConstant(
        string version,
        int expected,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var selection = await XstsSuite.LoadSelectionAsync(cancellationToken);

        await Assert
            .That(selection.Selected.Count(c => string.Equals(c.Version, version, StringComparison.Ordinal)))
            .IsEqualTo(expected);
    }

    [Test]
    public async Task Suite_PrecisionDecimalCases_AreExcludedWithTheReason(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var selection = await XstsSuite.LoadSelectionAsync(cancellationToken);

        var excluded = selection.Excluded.Where(e => e.Reason.Contains("DEF-52", StringComparison.Ordinal));

        await Assert.That(excluded.Count()).IsEqualTo(PrecisionDecimalExcludedCount);
    }

    [Test]
    public async Task Baseline_ListsOnlySelectedCases_InOneNonEmptyFilePerTestSet(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var selection = await XstsSuite.LoadSelectionAsync(cancellationToken);

        var selected = selection
            .Selected.GroupBy(c => c.TestSet, StringComparer.Ordinal)
            .ToDictionary(
                g => g.Key,
                g => new HashSet<string>(g.Select(c => c.Id), StringComparer.Ordinal),
                StringComparer.Ordinal
            );
        foreach (var file in Directory.GetFiles(XstsSuite.BaselineDirectory, "*.txt"))
        {
            var testSet = Path.GetFileNameWithoutExtension(file);
            var baseline = BaselineFiles.Load(XstsSuite.BaselineDirectory, testSet);

            await Assert
                .That(selected.ContainsKey(testSet))
                .IsTrue()
                .Because(testSet + " is not a test set of the selection.");
            await Assert.That(baseline.Count).IsGreaterThan(0).Because(testSet + " has an empty baseline file.");
            await Assert.That(baseline.Ids.Where(id => !selected[testSet].Contains(id))).IsEmpty();
        }
    }

    [Test]
    public async Task Suite_TestSetKeys_AreUniqueIgnoringCase(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var selection = await XstsSuite.LoadSelectionAsync(cancellationToken);

        var keys = selection.Selected.Select(c => c.TestSet).Distinct(StringComparer.Ordinal).ToArray();

        await Assert.That(keys.Distinct(StringComparer.OrdinalIgnoreCase).Count()).IsEqualTo(keys.Length);
    }

    [Test]
    [MethodDataSource(nameof(Chunks))]
    public async Task TestSet_AgainstBaseline_HasNoRegressionAndNoUnexpectedPass(
        string testSet,
        int chunk,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var selection = await XstsSuite.LoadSelectionAsync(cancellationToken);
        var cases = selection
            .Selected.Where(c => string.Equals(c.TestSet, testSet, StringComparison.Ordinal))
            .OrderBy(c => c.Id, StringComparer.Ordinal)
            .Skip(chunk * SetSize)
            .Take(SetSize)
            .ToArray();

        var results = await XstsRunner.RunAsync(
            new XstsSelection(cases, []),
            new SchemaXstsValidator(await XstsSuite.LoadRootAsync(cancellationToken)),
            cancellationToken
        );
        foreach (var xstsCase in cases)
        {
            if (
                xstsCase.Case.Kind == XstsKind.Schema
                && string.Equals(xstsCase.Version, XstsVersions.V10, StringComparison.Ordinal)
            )
            {
                SchemaCaseKeys.Add(SchemaRateReport.KeyOf(xstsCase.TestSet, xstsCase.Id));
            }
        }
        foreach (var result in results)
        {
            ChunkResults.Add(result);
        }

        var report = ConformanceReport.Compare(
            results,
            new Dictionary<string, KnownFailureBaseline>(StringComparer.Ordinal)
            {
                [testSet] = BaselineFiles.Load(XstsSuite.BaselineDirectory, testSet),
            }
        );

        await Assert.That(cases).IsNotEmpty();
        await Assert.That(report.IsSuccess).IsTrue();
    }

    /// <summary>Appends one aggregated rate row per test set after all chunks ran.</summary>
    [After(Class)]
    public static void PublishConformanceRate()
    {
        XstsRateReport.Publish([.. ChunkResults]);
        SchemaRateReport.Publish(
            [.. ChunkResults],
            new HashSet<string>(SchemaCaseKeys, StringComparer.Ordinal),
            Environment.GetEnvironmentVariable(ConformanceRateReporter.SummaryVariable)
        );
    }
}
