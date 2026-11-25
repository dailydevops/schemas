namespace NetEvolve.Schemas.Schematron.Tests.Integration.Conformance.Engine;

using System.Text;
using NetEvolve.Schemas.Conformance.Baseline;
using NetEvolve.Schemas.Conformance.Reporting;
using NetEvolve.Schemas.Conformance.Results;
using NetEvolve.Schemas.Schematron.Tests.Integration.Conformance.Runner;

/// <summary>
/// The Schematron conformance suite on the real validator: every selected core and SVRL case of the bindings xslt-1 and xslt-2 runs, and a
/// test set fails on a regression, on an unexpected pass, and on a baseline entry without a linked bug issue.
/// </summary>
/// <remarks>
/// Like the other conformance suites, the class is explicit and selected by its category, so the rate of the
/// Schematron suite shows once in the job summary of the conformance workflow.
/// </remarks>
[Explicit]
[Category(ConformanceCategory.Name)]
public class SchematronEngineConformanceTests
{
    private static readonly string LinksPath = Path.Combine(
        SchematronSuite.RepositoryRoot,
        "tests",
        "NetEvolve.Schemas.Schematron.Tests.Integration",
        "Conformance",
        "Engine",
        "known-failure-links.txt"
    );

    public static IEnumerable<Func<string>> TestSets() =>
        SchematronEngineRun.TestSets.Select(name => (Func<string>)(() => name));

    [Test]
    [MethodDataSource(nameof(TestSets))]
    public async Task TestSet_AgainstBaseline_HasNoRegressionAndNoUnexpectedPass(
        string testSet,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var results = (await SchematronEngineRun.GetResultsAsync(cancellationToken))
            .Where(r => string.Equals(r.TestSet, testSet, StringComparison.Ordinal))
            .ToList();

        var report = ConformanceReport.Compare(
            results,
            new Dictionary<string, KnownFailureBaseline>(StringComparer.Ordinal)
            {
                [testSet] = BaselineFiles.Load(SchematronSuite.BaselineDirectory, testSet),
            }
        );
        ConformanceRateReporter.Collect(
            "NetEvolve.Schemas.Schematron",
            TargetFrameworkName.Of(typeof(SchematronEngineConformanceTests).Assembly),
            report
        );

        await Assert.That(report.TestSets).HasSingleItem();
        await Assert.That(report.IsSuccess).IsTrue().Because(Format(report.TestSets[0], results));
    }

    [Test]
    public async Task Baseline_EveryEntry_IsASelectedCaseOfItsTestSet(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var selection = await SchematronSuite.LoadSelectionAsync(cancellationToken);

        await Assert
            .That(selection.Selected.Select(c => c.TestSet).Distinct(StringComparer.Ordinal))
            .IsEquivalentTo(SchematronEngineRun.TestSets);
        foreach (var testSet in SchematronEngineRun.TestSets)
        {
            var ids = selection
                .Selected.Where(c => string.Equals(c.TestSet, testSet, StringComparison.Ordinal))
                .Select(c => c.Id);
            foreach (var id in BaselineFiles.Load(SchematronSuite.BaselineDirectory, testSet).Ids)
            {
                await Assert.That(ids).Contains(id);
            }
        }
    }

    [Test]
    public async Task Baseline_EveryEntry_HasALinkedBugIssue(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var baseline = SchematronEngineRun
            .TestSets.SelectMany(testSet => BaselineFiles.Load(SchematronSuite.BaselineDirectory, testSet).Ids)
            .ToList();

        var problems = KnownFailureLinks.Verify(
            baseline,
            KnownFailureLinks.Parse(await File.ReadAllTextAsync(LinksPath, cancellationToken))
        );

        await Assert.That(problems).IsEmpty().Because(string.Join('\n', problems));
    }

    [Test]
    public async Task Selection_EveryExcludedCase_HasAReason(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var selection = await SchematronSuite.LoadSelectionAsync(cancellationToken);

        await Assert.That(selection.Excluded).IsNotEmpty();
        await Assert.That(selection.Excluded.All(e => !string.IsNullOrWhiteSpace(e.Reason))).IsTrue();
    }

    private static string Format(TestSetReport report, IReadOnlyList<CaseResult> results)
    {
        var builder = new StringBuilder();
        foreach (var id in report.Regressions)
        {
            var reason = results.First(r => string.Equals(r.CaseId, id, StringComparison.Ordinal)).Reason;
            _ = builder
                .Append("regression (fails, not in the baseline): ")
                .Append(id)
                .Append(": ")
                .Append(reason)
                .Append('\n');
        }

        foreach (var id in report.UnexpectedPasses)
        {
            _ = builder.Append("unexpected pass (passes, still in the baseline; remove it): ").Append(id).Append('\n');
        }

        return builder.ToString();
    }
}
