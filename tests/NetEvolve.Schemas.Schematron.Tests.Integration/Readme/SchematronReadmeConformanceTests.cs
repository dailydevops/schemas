namespace NetEvolve.Schemas.Schematron.Tests.Integration.Readme;

using System.Globalization;
using NetEvolve.Schemas.Conformance.Baseline;
using NetEvolve.Schemas.Conformance.Reporting;
using NetEvolve.Schemas.Conformance.Results;
using NetEvolve.Schemas.Schematron.Tests.Integration.Conformance.Engine;
using NetEvolve.Schemas.Schematron.Tests.Integration.Conformance.Runner;
using NetEvolve.Schemas.Schematron.Tests.Integration.Goldens.En16931;
using NetEvolve.Schemas.Schematron.Tests.Integration.Goldens.Stage4Globals;
using NetEvolve.Schemas.Testing.Readme;

/// <summary>
/// The conformance statement of src/NetEvolve.Schemas.Schematron/README.md equals the value of the conformance run: the counts and the
/// rate of the suite row and of its test sets, every failing and every excluded case with its issue and reason, and the stage 4 row.
/// </summary>
/// <remarks>Like the conformance suites, the class runs on the designated leg only, because the run needs the fetched suite.</remarks>
[Explicit]
[Category(ConformanceCategory.Name)]
public class SchematronReadmeConformanceTests
{
    private const string Readme = "src/NetEvolve.Schemas.Schematron/README.md";

    private static readonly string LinksPath = Path.Combine(
        SchematronSuite.RepositoryRoot,
        "tests",
        "NetEvolve.Schemas.Schematron.Tests.Integration",
        "Conformance",
        "Engine",
        "known-failure-links.txt"
    );

    [Test]
    public async Task Readme_StatesTheCountsAndTheRateOfTheSuiteRun(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var counts = (await Report(cancellationToken)).Counts;
        var readme = ReadmeSamples.ReadText(Readme);

        await Assert.That(counts.Applicable).IsGreaterThan(0);
        await Assert.That(readme).Contains($"{counts.Passed} of {counts.Applicable} applicable cases");
        await Assert.That(readme).Contains(Rate(counts));
        await Assert.That(readme).Contains($"{counts.Failed} known failures");
        await Assert.That(readme).Contains($"{counts.Skipped} excluded cases");
    }

    [Test]
    public async Task Readme_StatesTheRateOfEveryTestSet(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var readme = ReadmeSamples.ReadText(Readme);

        // A test set without applicable cases holds only the excluded cases of a folder; it has no row.
        foreach (var set in (await Report(cancellationToken)).TestSets.Where(s => s.Counts.Applicable > 0))
        {
            var row = $"| `{set.TestSet}` | {set.Counts.Applicable} | {set.Counts.Passed} | {Rate(set.Counts)} |";

            await Assert.That(readme).Contains(row);
        }
    }

    [Test]
    public async Task Readme_ListsEveryFailingAndEveryExcludedCaseWithItsIssueAndReason(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var readme = ReadmeSamples.ReadText(Readme);
        var results = await SchematronEngineRun.GetResultsAsync(cancellationToken);
        var links = KnownFailureLinks.Parse(await File.ReadAllTextAsync(LinksPath, cancellationToken));

        foreach (var result in results)
        {
            if (result.Outcome != CaseOutcome.Pass)
            {
                await Assert.That(readme).Contains($"`{result.CaseId}`");
            }
        }

        foreach (var link in links)
        {
            await Assert.That(readme).Contains($"#{link.Issue.ToString(CultureInfo.InvariantCulture)}");
            await Assert.That(readme).Contains(link.Reason);
        }
    }

    [Test]
    public async Task Readme_NamesTheReasonOfTheExcludedCases(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var readme = ReadmeSamples.ReadText(Readme);
        var skipped = (await SchematronEngineRun.GetResultsAsync(cancellationToken)).Where(r =>
            r.Outcome == CaseOutcome.Skipped
        );

        foreach (var result in skipped)
        {
            await Assert.That(result.Reason).IsNotNull();
            await Assert.That(readme).Contains("SCH-21");
            await Assert.That(readme).Contains("#378");
            await Assert.That(readme).Contains($"`{result.CaseId}`");
        }
    }

    [Test]
    public async Task Readme_StatesTheStage4Row(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var results = await Stage4Run.GetResultsAsync(cancellationToken);
        var passed = results.Count(r => r.Outcome == CaseOutcome.Pass);
        var readme = ReadmeSamples.ReadText(Readme);

        await Assert.That(readme).Contains($"{passed} of {results.Count} cases");
        await Assert.That(readme).Contains(Rate(new TestSetCounts(passed, results.Count - passed, 0)));
    }

    [Test]
    public async Task Readme_StatesTheCountsAndTheRateOfTheEn16931Goldens(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var results = (await En16931Run.GetResultsAsync(cancellationToken)).Values.SelectMany(r => r).ToList();
        var passed = results.Count(r => r.Outcome == CaseOutcome.Pass);
        var readme = ReadmeSamples.ReadText(Readme);

        await Assert.That(results).IsNotEmpty();
        var rate = Rate(new TestSetCounts(passed, results.Count - passed, 0));
        var statement = string.Create(
            CultureInfo.InvariantCulture,
            $"**{passed:N0} of {results.Count:N0} cases pass, a rate of {rate}**"
        );

        await Assert.That(readme).Contains(statement);
    }

    private static async Task<ConformanceReport> Report(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var baselines = new Dictionary<string, KnownFailureBaseline>(StringComparer.Ordinal);
        foreach (var testSet in SchematronEngineRun.TestSets)
        {
            baselines[testSet] = BaselineFiles.Load(SchematronSuite.BaselineDirectory, testSet);
        }

        return ConformanceReport.Compare(await SchematronEngineRun.GetResultsAsync(cancellationToken), baselines);
    }

    // The rate as the job summary prints it: passed / applicable, truncated, never rounded up, to one decimal.
    private static string Rate(TestSetCounts counts)
    {
        var tenths = counts.Passed * 1000 / counts.Applicable;
        return string.Create(CultureInfo.InvariantCulture, $"{tenths / 10}.{tenths % 10}%");
    }
}
