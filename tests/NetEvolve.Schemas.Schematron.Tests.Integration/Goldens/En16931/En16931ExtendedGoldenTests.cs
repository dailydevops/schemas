namespace NetEvolve.Schemas.Schematron.Tests.Integration.Goldens.En16931;

using NetEvolve.Schemas.Conformance.Baseline;
using NetEvolve.Schemas.Conformance.Reporting;
using NetEvolve.Schemas.Conformance.Results;

/// <summary>
/// The EDIFACT binding of the pinned CEN EN 16931 archive, which validates the ISO 20625 XML form of an invoice, and the complete
/// UBL invoices of <c>test/testfiles</c>. Each is a test set of its own with its own rate row; every document is a positive case.
/// </summary>
public class En16931ExtendedGoldenTests
{
    [Test]
    [Arguments("edifact", "examples/", 9)]
    [Arguments("ubl-testfiles", "testfiles/", 29)]
    public async Task Corpus_EveryDocumentOfTheDirectory_IsOnePositiveCase(
        string testSet,
        string prefix,
        int expectedCount,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(testSet);
        var root = await En16931Run.GetRootAsync(cancellationToken);

        var cases = En16931Run.LoadExtendedCases(root, testSet);

        await Assert.That(cases.Count).IsEqualTo(expectedCount);
        await Assert.That(cases.All(c => c.IsExample && c.Id.StartsWith(prefix, StringComparison.Ordinal))).IsTrue();
        await Assert.That(cases.Select(c => c.Id).Distinct(StringComparer.Ordinal).Count()).IsEqualTo(expectedCount);
    }

    [Test]
    [Arguments("edifact")]
    [Arguments("ubl-testfiles")]
    public async Task TestSet_EveryDocument_IsValid(string testSet, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(testSet);
        var results = (await En16931Run.GetExtendedResultsAsync(cancellationToken))[testSet];

        var report = ConformanceReport.Compare(
            results,
            new Dictionary<string, KnownFailureBaseline>(StringComparer.Ordinal)
        );
        ConformanceRateReporter.Collect(
            En16931Run.RatePackage + ", " + testSet,
            TargetFrameworkName.Of(typeof(En16931ExtendedGoldenTests).Assembly),
            report
        );

        await Assert.That(results).IsNotEmpty();
        await Assert.That(results.All(r => r.TestSet == testSet)).IsTrue();
        foreach (var result in results)
        {
            await Assert.That(result.Outcome).IsEqualTo(CaseOutcome.Pass).Because(result.CaseId + ": " + result.Reason);
        }
    }

    [Test]
    public async Task EdifactSchema_AbstractPatternAndBindingFiles_ExpandWithoutError(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var root = await En16931Run.GetRootAsync(cancellationToken);

        var validator = En16931Runner.Build(En16931Run.SchemaPath(root, "edifact"));

        await Assert.That(validator.Schema.Patterns.Any(p => p.ExpandedFrom is not null)).IsTrue();
        await Assert.That(validator.Schema.Patterns.Any(p => p.Rules.Count > 0)).IsTrue();
    }
}
