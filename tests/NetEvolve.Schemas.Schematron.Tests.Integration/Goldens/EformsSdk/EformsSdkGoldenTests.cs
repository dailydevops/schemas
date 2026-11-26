namespace NetEvolve.Schemas.Schematron.Tests.Integration.Goldens.EformsSdk;

using System.Text;
using NetEvolve.Schemas.Conformance.Baseline;
using NetEvolve.Schemas.Conformance.Reporting;
using NetEvolve.Schemas.Conformance.Results;
using NetEvolve.Schemas.Schematron.Tests.Integration.Conformance.Engine;

/// <summary>
/// The SVRL fidelity of the engine against a reference: every example notice of the pinned eForms SDK is validated with the static rule
/// set, and the fired rule ids with their roles and locations (one test set per validation stage) and the texts (one test set) are
/// compared with the expected SVRL report of the SDK. The archive is fetched by its pinned checksum and never committed.
/// </summary>
/// <remarks>
/// Like the other conformance suites, the class is explicit and selected by its category. A test set fails on a regression, on an
/// unexpected pass, and on a baseline entry without a linked bug issue.
/// </remarks>
[Explicit]
[Category(ConformanceCategory.Name)]
public class EformsSdkGoldenTests
{
    private static readonly string LinksPath = Path.Combine(EformsSdkRun.BaselineDirectory, "known-failure-links.txt");

    public static IEnumerable<Func<string>> TestSets() => BaselineTestSets().Select(name => (Func<string>)(() => name));

    [Test]
    [MethodDataSource(nameof(TestSets))]
    public async Task TestSet_AgainstBaseline_HasNoRegressionAndNoUnexpectedPass(
        string testSet,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var results = (await EformsSdkRun.GetResultsAsync(cancellationToken))
            .Where(r => string.Equals(r.TestSet, testSet, StringComparison.Ordinal))
            .ToList();

        var report = ConformanceReport.Compare(
            results,
            new Dictionary<string, KnownFailureBaseline>(StringComparer.Ordinal)
            {
                [testSet] = BaselineFiles.Load(EformsSdkRun.BaselineDirectory, testSet),
            }
        );
        ConformanceRateReporter.Collect(
            EformsSdkRun.RatePackage,
            TargetFrameworkName.Of(typeof(EformsSdkGoldenTests).Assembly),
            report
        );

        await Assert.That(results).IsNotEmpty();
        await Assert.That(report.TestSets).HasSingleItem();
        await Assert.That(report.IsSuccess).IsTrue().Because(Format(report.TestSets[0], results));
    }

    [Test]
    public async Task Results_EveryTestSet_IsBaselinedOrTheExcludedDynamicStage(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var results = await EformsSdkRun.GetResultsAsync(cancellationToken);

        var unknown = results
            .Select(r => r.TestSet)
            .Distinct(StringComparer.Ordinal)
            .Except(BaselineTestSets().Append("stage-6"), StringComparer.Ordinal)
            .ToList();

        await Assert.That(unknown).IsEmpty().Because($"test sets without a baseline: {string.Join(", ", unknown)}");
    }

    [Test]
    public async Task Baseline_EveryEntry_IsACaseOfItsTestSet(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var results = await EformsSdkRun.GetResultsAsync(cancellationToken);

        foreach (var testSet in BaselineTestSets())
        {
            var ids = results
                .Where(r => string.Equals(r.TestSet, testSet, StringComparison.Ordinal))
                .Select(r => r.CaseId);
            foreach (var id in BaselineFiles.Load(EformsSdkRun.BaselineDirectory, testSet).Ids)
            {
                await Assert.That(ids).Contains(id);
            }
        }
    }

    [Test]
    public async Task Baseline_EveryEntry_HasALinkedBugIssue(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var baseline = BaselineTestSets()
            .SelectMany(testSet => BaselineFiles.Load(EformsSdkRun.BaselineDirectory, testSet).Ids)
            .ToList();

        var problems = KnownFailureLinks.Verify(
            baseline,
            KnownFailureLinks.Parse(
                File.Exists(LinksPath) ? await File.ReadAllTextAsync(LinksPath, cancellationToken) : string.Empty
            )
        );

        await Assert.That(problems).IsEmpty().Because(string.Join('\n', problems));
    }

    internal static IEnumerable<string> BaselineTestSets() =>
        [
            "stage-1",
            "stage-2",
            "stage-3",
            "stage-4",
            "stage-5",
            EformsComparer.TextTestSet,
            EformsComparer.ValidTestSet,
        ];

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
