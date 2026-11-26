namespace NetEvolve.Schemas.Schematron.Tests.Unit.Loading.XInclude.Suite;

using NetEvolve.Schemas.Conformance.Baseline;
using NetEvolve.Schemas.Conformance.Reporting;
using NetEvolve.Schemas.Conformance.Results;

/// <summary>The pinned W3C XInclude test suite run through the XInclude resolver of the Schematron loader; run on demand with the conformance category.</summary>
public class XIncludeConformanceTests
{
    [Test]
    [Explicit]
    [Category(ConformanceCategory.Name)]
    public async Task Baseline_MatchesTheRun_AndEveryEntryIsLinkedToAnIssue(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var results = await RunAsync(cancellationToken);
        var baselines = results
            .Select(r => r.TestSet)
            .Distinct(StringComparer.Ordinal)
            .ToDictionary(
                set => set,
                set => BaselineFiles.Load(XIncludeSuite.BaselineDirectory, set),
                StringComparer.Ordinal
            );

        var report = ConformanceReport.Compare(results, baselines);

        await Assert.That(report.IsSuccess).IsTrue().Because(Describe(report));
        await Assert.That(report.Counts.Passed).IsGreaterThan(0);
    }

    [Test]
    [Explicit]
    [Category(ConformanceCategory.Name)]
    public async Task Suite_HoldsTheCasesOfEveryContributor(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var cases = XIncludeCatalogReader.Read(await XIncludeSuite.FetchAsync(cancellationToken));

        await Assert
            .That(cases.Select(c => c.Contributor).Distinct(StringComparer.Ordinal))
            .IsEquivalentTo(["Imaq", "FourThought", "Nist", "EdUni", "Harold"]);
        await Assert.That(cases.Count).IsEqualTo(174);
    }

    [Test]
    [Explicit]
    [Category(ConformanceCategory.Name)]
    public async Task Baseline_Regenerate(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var results = await RunAsync(cancellationToken);

        foreach (var set in results.Select(r => r.TestSet).Distinct(StringComparer.Ordinal))
        {
            var failures = results
                .Where(r => string.Equals(r.TestSet, set, StringComparison.Ordinal) && r.Outcome == CaseOutcome.Fail)
                .Select(r => r.CaseId)
                .ToList();
            if (failures.Count == 0)
            {
                File.Delete(BaselineFiles.PathFor(XIncludeSuite.BaselineDirectory, set));
            }
            else
            {
                BaselineFiles.Save(XIncludeSuite.BaselineDirectory, set, failures);
            }
        }

        await Assert.That(results).IsNotEmpty();
    }

    private static async Task<IReadOnlyList<CaseResult>> RunAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var root = await XIncludeSuite.FetchAsync(cancellationToken);
        return XIncludeRunner.Run(root, XIncludeCatalogReader.Read(root), cancellationToken);
    }

    private static string Describe(ConformanceReport report) =>
        string.Join(
            Environment.NewLine,
            report.TestSets.Select(s =>
                $"{s.TestSet}: passed {s.Counts.Passed}, failed {s.Counts.Failed}, skipped {s.Counts.Skipped}; regressions [{string.Join(", ", s.Regressions)}]; unexpected passes [{string.Join(", ", s.UnexpectedPasses)}]"
            )
        );
}
