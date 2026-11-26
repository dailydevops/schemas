namespace NetEvolve.Schemas.Schematron.Tests.Unit.SuitePins;

using NetEvolve.Schemas.Conformance.Fetch;
using NetEvolve.Schemas.Conformance.Manifest;

public class RuleSetManifestTests
{
    private static readonly HttpClient Client = new() { Timeout = TimeSpan.FromMinutes(10) };

    [Test]
    [Arguments("ehf.json", "stage2-ehf")]
    [Arguments("peppol-reporting.json", "stage2-peppol-reporting")]
    [Arguments("factur-x.json", "stage3-factur-x")]
    [Arguments("french-ctc.json", "stage3-french-ctc")]
    [Arguments("eforms-sdk.json", "eforms-sdk")]
    public async Task Manifest_PinsOneReleaseEntryWithChecksum(string fileName, string key)
    {
        var manifest = SuitePinFiles.Load(fileName);

        await Assert.That(manifest.Key).IsEqualTo(key);
        await Assert.That(manifest.SourceUrl.Scheme).IsEqualTo(Uri.UriSchemeHttps);
        await Assert.That(manifest.SourceUrl.AbsolutePath).DoesNotContain("/heads/");
        await Assert.That(manifest.SourceUrl.AbsolutePath).DoesNotContain("/master");
        await Assert.That(manifest.SourceUrl.AbsolutePath).DoesNotContain("/main");
        await Assert.That(manifest.Sha256.Length).IsEqualTo(64);
    }

    [Test]
    [Arguments("ehf.json", "EHF-CATALOGUE-3.0.sch")]
    [Arguments("peppol-reporting.json", "peppol-end-user-statistics-reporting-1.1.0.sch")]
    [Arguments("factur-x.json", "FACTUR-X_BASIC-WL.sch")]
    [Arguments("french-ctc.json", "EXTENDED-CTC-FR-CII.sch")]
    [Arguments("eforms-sdk.json", "complete-validation.sch")]
    public async Task Fetch_DownloadsEntryAndVerifiesChecksum(
        string fileName,
        string expectedFile,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var manifest = SuitePinFiles.Load(fileName);
        var fetcher = new SuiteFetcher(new HttpSuiteTransport(Client), ConformanceCache.ResolveRoot());

        var root = await fetcher.FetchAsync(manifest, cancellationToken);

        await Assert.That(Directory.EnumerateFiles(root, expectedFile, SearchOption.AllDirectories)).IsNotEmpty();
    }

    [Test]
    public async Task Fetch_ChangedChecksum_FailsNamingTheKey(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var manifest = SuitePinFiles.Load("peppol-reporting.json") with { Sha256 = new string('0', 64) };
        var fetcher = new SuiteFetcher(new HttpSuiteTransport(Client), ConformanceCache.ResolveRoot());

        var ex = await Assert.ThrowsAsync<SuiteFetchException>(async () =>
            await fetcher.FetchAsync(manifest, cancellationToken)
        );

        await Assert.That(ex!.Message).Contains("stage2-peppol-reporting");
    }

    [Test]
    public async Task FetchTarget_LiesOutsideTheTrackedTree(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var root = ConformanceCache.ResolveRoot(null);
        var relative = Path.GetRelativePath(SuitePinFiles.RepositoryRoot, root);
        var gitignore = Path.Combine(SuitePinFiles.RepositoryRoot, ".gitignore");
        var lines = await File.ReadAllLinesAsync(gitignore, cancellationToken);
        var ignored = lines.Any(l => string.Equals(l.Trim(), "artifacts/", StringComparison.Ordinal));

        await Assert.That(relative.Replace(Path.DirectorySeparatorChar, '/')).StartsWith("artifacts/");
        await Assert.That(ignored).IsTrue();
    }
}
