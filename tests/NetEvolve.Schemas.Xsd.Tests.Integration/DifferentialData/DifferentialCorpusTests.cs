namespace NetEvolve.Schemas.Xsd.Tests.Integration.DifferentialData;

using NetEvolve.Schemas.Xsd.Tests.Integration.Conformance;
using NetEvolve.Schemas.Xsd.Tests.Integration.Differential;
using NetEvolve.Schemas.Xsd.Tests.Integration.Xsts;

/// <summary>
/// The differential run over the XSTS 1.0 corpus: this library and System.Xml.Schema read every schema and instance case, and
/// every difference is an entry of the deviation list or a case that the known-failures lists track with a bug issue.
/// </summary>
[BaselineLeg]
public class DifferentialCorpusTests
{
    /// <summary>The test sets of the pinned suite; the names come from the committed manifest, so discovery needs no download.</summary>
    public static IEnumerable<Func<string>> TestSets()
    {
        foreach (var (testSet, _) in TestSetSizes.Load())
        {
            var captured = testSet;
            yield return () => captured;
        }
    }

    [Test]
    [MethodDataSource(nameof(TestSets))]
    public async Task TestSet_EveryDifferenceToSystemXmlSchema_IsTriaged(
        string testSet,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var tracked = TrackedDefects.Load();
        var cases = new List<DifferentialCase>();
        foreach (var differentialCase in await DifferentialCorpus.LoadAsync(cancellationToken))
        {
            if (
                differentialCase.Id.StartsWith(testSet + "/", StringComparison.Ordinal)
                && !tracked.ContainsKey(differentialCase.Id)
            )
            {
                cases.Add(differentialCase);
            }
        }

        var report = await DifferentialRunner.RunAsync(
            cases,
            new OursEngine(await XstsSuite.LoadRootAsync(cancellationToken)),
            new ThrowTolerantEngine(new SystemXmlSchemaEngine()),
            DeviationList.Load(DeviationData.ListPath),
            DifferentialLeg.Current(typeof(DifferentialCorpusTests).Assembly),
            cancellationToken
        );

        await Assert.That(report.BaselineRun).IsTrue();
        await Assert.That(report.Failures).IsEmpty();
    }

    [Test]
    public async Task Corpus_HasTheCasesOfXsd10Only_WithUniqueIds(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var cases = await DifferentialCorpus.LoadAsync(cancellationToken);
        var selection = await XstsSuite.LoadSelectionAsync(cancellationToken);

        await Assert
            .That(cases.Count)
            .IsEqualTo(
                selection.Selected.Count(c => string.Equals(c.Version, XstsVersions.V10, StringComparison.Ordinal))
            );
        await Assert.That(cases.Select(c => c.Id).Distinct(StringComparer.Ordinal).Count()).IsEqualTo(cases.Count);
    }
}
