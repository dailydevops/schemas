namespace NetEvolve.Schemas.Xsd.Tests.Integration.Xsts;

using NetEvolve.Schemas.Conformance.Baseline;
using NetEvolve.Schemas.Conformance.Results;

public class XstsSuiteTests
{
    /// <summary>The number of cases the pinned suite selects for XSD 1.0; a new pin changes it on purpose.</summary>
    private const int SelectedCaseCount10 = 39271;

    /// <summary>The number of cases the pinned suite selects for XSD 1.1; a new pin changes it on purpose.</summary>
    private const int SelectedCaseCount11 = 41389;

    /// <summary>The number of cases excluded because they need the precisionDecimal datatype (DEF-52, #64).</summary>
    private const int PrecisionDecimalExcludedCount = 122;

    /// <summary>The number of selected cases that one set test runs (decision P6 of the conformance spike).</summary>
    private const int SetSize = 100;

    public static IEnumerable<Func<(string TestSet, int Chunk)>> Chunks()
    {
        foreach (var file in Directory.GetFiles(XstsSuite.BaselineDirectory, "*.txt").Order(StringComparer.Ordinal))
        {
            var testSet = Path.GetFileNameWithoutExtension(file);
            var count = BaselineFiles.Load(XstsSuite.BaselineDirectory, testSet).Count;
            for (var chunk = 0; chunk * SetSize < count; chunk++)
            {
                var captured = (testSet, chunk);
                yield return () => captured;
            }
        }
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
    public async Task Baseline_ListsEverySelectedCase_InOneFilePerTestSet(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var selection = await XstsSuite.LoadSelectionAsync(cancellationToken);

        var expected = selection
            .Selected.GroupBy(c => c.TestSet, StringComparer.Ordinal)
            .ToDictionary(
                g => g.Key,
                g => g.Select(c => c.Id).Order(StringComparer.Ordinal).ToArray(),
                StringComparer.Ordinal
            );
        var files = Directory
            .GetFiles(XstsSuite.BaselineDirectory, "*.txt")
            .Select(f => Path.GetFileNameWithoutExtension(f))
            .Order(StringComparer.Ordinal);

        await Assert.That(files).IsEquivalentTo(expected.Keys.Order(StringComparer.Ordinal));
        foreach (var (testSet, ids) in expected)
        {
            var baseline = BaselineFiles.Load(XstsSuite.BaselineDirectory, testSet);
            await Assert.That(baseline.Ids).IsEquivalentTo(ids);
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
            UnboundXstsValidator.Instance,
            cancellationToken
        );
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
}
