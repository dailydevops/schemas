namespace NetEvolve.Schemas.Xsd.Tests.Integration.EInvoicing;

using NetEvolve.Schemas.Conformance.Baseline;
using NetEvolve.Schemas.Conformance.Reporting;
using NetEvolve.Schemas.Conformance.Results;

/// <summary>
/// The XSD evidence on real schemas: the e-invoicing schema sets of the pinned archives (UN/CEFACT CII, UBL 2.1, CDAR, Factur-X,
/// Flux10, the Peppol reporting formats) are built once each, and the shipped documents are validated with them. The archives are
/// fetched by their pinned checksums and never committed.
/// </summary>
/// <remarks>
/// A document with the expectation <c>valid</c> must be accepted. Any other document must get the verdict of System.Xml.Schema, so a
/// deviation is a finding. A finding that does not pass is a known failure with the issue that covers it.
/// </remarks>
[Explicit]
[Category(ConformanceCategory.Name)]
public class EInvoicingConformanceTests
{
    public static IEnumerable<Func<string>> TestSets() =>
        EInvoicingRun.LoadManifest().Sets.Select(s => (Func<string>)(() => s.Name));

    [Test]
    [MethodDataSource(nameof(TestSets))]
    public async Task TestSet_AgainstKnownFailures_HasNoRegressionAndNoUnexpectedPass(
        string testSet,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var results = (await EInvoicingRun.GetResultsAsync(cancellationToken))
            .Where(r => string.Equals(r.TestSet, testSet, StringComparison.Ordinal))
            .ToList();
        var known = EInvoicingRun
            .LoadManifest()
            .KnownFailures.Select(l => l.Id)
            .Where(id => results.Any(r => string.Equals(r.CaseId, id, StringComparison.Ordinal)));

        var report = ConformanceReport.Compare(
            results,
            new Dictionary<string, KnownFailureBaseline>(StringComparer.Ordinal)
            {
                [testSet] = KnownFailureBaseline.Parse(KnownFailureBaseline.Format(known)),
            }
        );
        ConformanceRateReporter.Collect(
            EInvoicingRun.RatePackage,
            TargetFrameworkName.Of(typeof(EInvoicingConformanceTests).Assembly),
            report
        );

        var failures = results.Where(r => r.Outcome == CaseOutcome.Fail).Select(r => r.CaseId + ": " + r.Reason);
        await Assert.That(results).IsNotEmpty();
        await Assert.That(report.TestSets).HasSingleItem();
        await Assert.That(report.IsSuccess).IsTrue().Because(string.Join(Environment.NewLine, failures));
    }

    [Test]
    public async Task KnownFailures_EveryEntry_NamesARunCaseAndHasAnIssueAndAReason(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var ids = (await EInvoicingRun.GetResultsAsync(cancellationToken)).Select(r => r.CaseId).ToList();

        foreach (var failure in EInvoicingRun.LoadManifest().KnownFailures)
        {
            await Assert.That(ids).Contains(failure.Id);
            await Assert.That(failure.Issue).IsGreaterThan(0);
            await Assert.That(string.IsNullOrWhiteSpace(failure.Reason)).IsFalse();
        }
    }

    [Test]
    public async Task Exclusions_EveryEntry_NamesADocumentOfASetAndHasAReason(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var roots = await EInvoicingRun.GetRootsAsync(cancellationToken);
        var manifest = EInvoicingRun.LoadManifest();
        var ids = manifest
            .Sets.SelectMany(s =>
                s.Documents.SelectMany(g => EInvoicingRunner.ListDocuments(roots[g.Suite], g))
                    .Select(d => s.Name + "/" + d)
            )
            .ToList();

        foreach (var exclusion in manifest.Excluded)
        {
            await Assert.That(ids).Contains(exclusion.Id);
            await Assert.That(string.IsNullOrWhiteSpace(exclusion.Reason)).IsFalse();
        }
    }

    [Test]
    public async Task Sets_EveryDocumentDirectory_HoldsDocuments(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var roots = await EInvoicingRun.GetRootsAsync(cancellationToken);

        foreach (var group in EInvoicingRun.LoadManifest().Sets.SelectMany(s => s.Documents))
        {
            await Assert.That(EInvoicingRunner.ListDocuments(roots[group.Suite], group)).IsNotEmpty();
        }
    }
}
