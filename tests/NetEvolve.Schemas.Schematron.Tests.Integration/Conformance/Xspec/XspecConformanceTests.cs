namespace NetEvolve.Schemas.Schematron.Tests.Integration.Conformance.Xspec;

using System.Text;
using NetEvolve.Schemas.Conformance.Baseline;
using NetEvolve.Schemas.Conformance.Reporting;
using NetEvolve.Schemas.Conformance.Results;
using NetEvolve.Schemas.Schematron.Tests.Integration.Conformance.Engine;

/// <summary>
/// The declarative Schematron scenarios of the pinned XSpec project on the real validator: every selected expectation runs, and a test set (one
/// <c>.xspec</c> file) fails on a regression, on an unexpected pass, and on a baseline entry without a linked bug issue.
/// </summary>
/// <remarks>
/// Like the other conformance suites, the class is explicit and selected by its category, so the rate of the suite shows once in the job
/// summary of the conformance workflow.
/// </remarks>
[Explicit]
[Category(ConformanceCategory.Name)]
public class XspecConformanceTests
{
    /// <summary>The number of expectations the pinned release selects and the number of parts it excludes; a new release changes them on purpose.</summary>
    private const int SelectedCount = 233;

    private const int ExcludedCount = 326;

    [Test]
    public async Task Suite_AgainstBaselines_HasNoRegressionAndNoUnexpectedPass(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var run = await XspecSuite.RunAsync(cancellationToken);

        var report = ConformanceReport.Compare(run.Results, Baselines(run));
        ConformanceRateReporter.Collect(
            "NetEvolve.Schemas.Schematron (XSpec)",
            TargetFrameworkName.Of(typeof(XspecConformanceTests).Assembly),
            report
        );

        await Assert.That(report.IsSuccess).IsTrue().Because(Format(report, run.Results));
    }

    [Test]
    public async Task Baseline_EveryEntry_IsASelectedCaseOfItsTestSet(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var run = await XspecSuite.RunAsync(cancellationToken);
        var ids = run.Selection.Selected.Select(c => c.Id).ToHashSet(StringComparer.Ordinal);

        var files = Directory.Exists(XspecSuite.BaselineDirectory)
            ? Directory.GetFiles(XspecSuite.BaselineDirectory, "*.txt")
            : [];
        foreach (var file in files)
        {
            var testSet = Path.GetFileNameWithoutExtension(file);
            await Assert.That(TestSets(run)).Contains(testSet);
            foreach (var id in BaselineFiles.Load(XspecSuite.BaselineDirectory, testSet).Ids)
            {
                await Assert.That(ids).Contains(id);
            }
        }
    }

    [Test]
    public async Task Baseline_EveryEntry_HasALinkedBugIssue(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var run = await XspecSuite.RunAsync(cancellationToken);
        var baseline = TestSets(run)
            .SelectMany(testSet => BaselineFiles.Load(XspecSuite.BaselineDirectory, testSet).Ids)
            .ToList();

        var problems = KnownFailureLinks.Verify(
            baseline,
            KnownFailureLinks.Parse(await File.ReadAllTextAsync(XspecSuite.LinksPath, cancellationToken))
        );

        await Assert.That(problems).IsEmpty().Because(string.Join('\n', problems));
    }

    [Test]
    public async Task Selection_EveryExcludedPart_HasAReason(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var run = await XspecSuite.RunAsync(cancellationToken);

        await Assert.That(run.Selection.Excluded).IsNotEmpty();
        await Assert.That(run.Selection.Excluded.All(e => !string.IsNullOrWhiteSpace(e.Reason))).IsTrue();
    }

    [Test]
    public async Task Selection_Counts_EqualTheCommittedConstants(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var run = await XspecSuite.RunAsync(cancellationToken);

        await Assert.That(run.Selection.Selected.Count).IsEqualTo(SelectedCount);
        await Assert.That(run.Selection.Excluded.Count).IsEqualTo(ExcludedCount);
    }

    [Test]
    public async Task Selection_EveryExcludedPath_ExistsInThePinnedProject(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var run = await XspecSuite.RunAsync(cancellationToken);

        foreach (var (prefix, _) in XspecReader.ExcludedPaths)
        {
            var path = Path.Combine(run.Root, prefix.TrimEnd('/'));
            await Assert.That(File.Exists(path) || Directory.Exists(path)).IsTrue().Because(prefix);
        }
    }

    /// <summary>Regenerates the baseline files from the failures of the run; run it with the explicit filter after a deliberate change.</summary>
    [Test]
    public async Task Baseline_Regenerate(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var run = await XspecSuite.RunAsync(cancellationToken);
        _ = Directory.CreateDirectory(XspecSuite.BaselineDirectory);

        foreach (var testSet in TestSets(run))
        {
            var failures = run
                .Results.Where(r =>
                    string.Equals(r.TestSet, testSet, StringComparison.Ordinal) && r.Outcome == CaseOutcome.Fail
                )
                .Select(r => r.CaseId)
                .ToList();
            if (failures.Count == 0)
            {
                File.Delete(BaselineFiles.PathFor(XspecSuite.BaselineDirectory, testSet));
            }
            else
            {
                BaselineFiles.Save(XspecSuite.BaselineDirectory, testSet, failures);
            }
        }
    }

    private static IReadOnlyList<string> TestSets(XspecRun run) =>
        [.. run.Selection.Selected.Select(c => c.TestSet).Distinct(StringComparer.Ordinal)];

    private static Dictionary<string, KnownFailureBaseline> Baselines(XspecRun run) =>
        TestSets(run)
            .ToDictionary(
                testSet => testSet,
                testSet => BaselineFiles.Load(XspecSuite.BaselineDirectory, testSet),
                StringComparer.Ordinal
            );

    private static string Format(ConformanceReport report, IReadOnlyList<CaseResult> results)
    {
        var builder = new StringBuilder();
        foreach (var set in report.TestSets)
        {
            foreach (var id in set.Regressions)
            {
                var reason = results.First(r => string.Equals(r.CaseId, id, StringComparison.Ordinal)).Reason;
                _ = builder
                    .Append("regression (fails, not in the baseline): ")
                    .Append(id)
                    .Append(": ")
                    .Append(reason)
                    .Append('\n');
            }

            foreach (var id in set.UnexpectedPasses)
            {
                _ = builder
                    .Append("unexpected pass (passes, still in the baseline; remove it): ")
                    .Append(id)
                    .Append('\n');
            }
        }

        return builder.ToString();
    }
}
