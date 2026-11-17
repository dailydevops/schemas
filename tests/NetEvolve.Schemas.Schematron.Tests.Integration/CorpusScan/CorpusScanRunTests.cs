namespace NetEvolve.Schemas.Schematron.Tests.Integration.CorpusScan;

using NetEvolve.Schemas.Conformance.Fetch;
using VerifyTUnit;

public class CorpusScanRunTests
{
    private static readonly HttpClient Client = new() { Timeout = TimeSpan.FromMinutes(10) };

    [Test]
    public async Task ScanSuite_ReportListsEachConstructPerRuleSet_AndSkipsPartFiles()
    {
        var scans = CorpusScanRun.ScanSuite("fixtures", CorpusFiles.FixturesDirectory);

        await Assert
            .That(scans.Select(s => s.Name))
            .IsEquivalentTo([
                "fixtures/default-namespace.sch",
                "fixtures/unusual-prefix.sch",
                "fixtures/with-xinclude.sch",
            ]);
        await Verifier.Verify(CorpusReport.Render(scans), "txt").DisableDiff();
    }

    [Test]
    public async Task LoadPins_ReadsTheRuleSetPins_NotTheStageSources()
    {
        var pins = CorpusScanRun.LoadPins(CorpusFiles.SuitesDirectory);

        await Assert
            .That(pins.Select(p => p.Key))
            .IsEquivalentTo(["stage2-ehf", "stage2-peppol-reporting", "stage3-factur-x", "stage3-french-ctc"]);
    }

    [Test]
    public async Task ScanPinnedCorpus_FindsRuleSetsAndWritesTheJobSummary(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var pins = CorpusScanRun.LoadPins(CorpusFiles.SuitesDirectory);
        var fetcher = new SuiteFetcher(new HttpSuiteTransport(Client), ConformanceCache.ResolveRoot());

        var scans = await CorpusScanRun.ScanAsync(pins, fetcher, cancellationToken);
        var path = CorpusScanRun.WriteSummary(CorpusReport.Render(scans), CorpusFiles.RepositoryRoot);

        foreach (var pin in pins)
        {
            await Assert.That(scans.Any(s => s.Name.StartsWith(pin.Key + "/", StringComparison.Ordinal))).IsTrue();
        }
        await Assert.That(File.Exists(path)).IsTrue();
    }
}
