namespace NetEvolve.Schemas.Schematron.Tests.Integration.Goldens.Harness;

using NetEvolve.Schemas.Conformance.Fetch;
using NetEvolve.Schemas.Conformance.Manifest;

public class GoldenCorpusTests
{
    [Test]
    public async Task Resolve_PinnedSource_FetchesThroughTheManifestAndReturnsTheFile(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var fixture = new GoldenFixture();

        var document = await fixture.Corpus.ResolveAsync(GoldenFixture.Source("b"), cancellationToken);

        await Assert.That(document.Id).IsEqualTo("golden-rules/docs/b.xml");
        await Assert.That(await File.ReadAllTextAsync(document.Path, cancellationToken)).IsEqualTo("<doc/>");
        await Assert.That(fixture.Transport.Calls).IsEqualTo(1);
    }

    [Test]
    public async Task Resolve_SecondDocumentOfTheSuite_ReusesTheCachedArchive(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var fixture = new GoldenFixture();
        var corpus = fixture.Corpus;

        _ = await corpus.ResolveAsync(GoldenFixture.Source("a"), cancellationToken);
        _ = await corpus.ResolveAsync(GoldenFixture.Source("b"), cancellationToken);

        await Assert.That(fixture.Transport.Calls).IsEqualTo(1);
    }

    [Test]
    public async Task EnsurePinned_SuiteWithoutManifest_IsRejectedNamingTheSuite()
    {
        using var fixture = new GoldenFixture();

        await Assert
            .That(() => fixture.Corpus.EnsurePinned(new GoldenDocumentSource("unpinned-suite", "docs/a.xml")))
            .Throws<GoldenSourceException>()
            .WithMessageContaining("unpinned-suite", StringComparison.Ordinal);
    }

    [Test]
    [Arguments("")]
    [Arguments("   ")]
    [Arguments("abc")]
    [Arguments("zz00000000000000000000000000000000000000000000000000000000000000")]
    public async Task Constructor_ManifestWithoutPinnedChecksum_IsRejected(string checksum)
    {
        using var fixture = new GoldenFixture();
        var unpinned = fixture.Manifest with { Sha256 = checksum };

        await Assert
            .That(() => _ = new GoldenCorpus([unpinned], fixture.Fetcher))
            .Throws<GoldenSourceException>()
            .WithMessageContaining(GoldenFixture.SuiteKey, StringComparison.Ordinal);
    }

    [Test]
    public async Task Resolve_ChangedChecksum_FailsNamingTheSuiteAndNeverReturnsADocument(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var fixture = new GoldenFixture();
        var tampered = fixture.Manifest with { Sha256 = new string('0', 64) };
        var corpus = new GoldenCorpus([tampered], fixture.Fetcher);

        var ex = await Assert.ThrowsAsync<SuiteFetchException>(async () =>
            await corpus.ResolveAsync(GoldenFixture.Source("a"), cancellationToken)
        );

        await Assert.That(ex!.Message).Contains(GoldenFixture.SuiteKey);
    }

    [Test]
    [Arguments("../outside.xml")]
    [Arguments("docs/../../outside.xml")]
    [Arguments("/docs/a.xml")]
    [Arguments("docs/missing.xml")]
    [Arguments("")]
    public async Task Resolve_PathOutsideTheSuiteOrMissing_IsRejected(string path, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var fixture = new GoldenFixture();

        _ = await Assert.ThrowsAsync<GoldenSourceException>(async () =>
            await fixture.Corpus.ResolveAsync(new GoldenDocumentSource(GoldenFixture.SuiteKey, path), cancellationToken)
        );
    }

    [Test]
    public async Task Constructor_UppercaseChecksum_IsRejected()
    {
        using var fixture = new GoldenFixture();
        var uppercase = fixture.Manifest with { Sha256 = fixture.Manifest.Sha256.ToUpperInvariant() };

        await Assert
            .That(() => _ = new GoldenCorpus([uppercase], fixture.Fetcher))
            .Throws<GoldenSourceException>()
            .WithMessageContaining(GoldenFixture.SuiteKey, StringComparison.Ordinal);
    }

    [Test]
    public async Task Constructor_NullManifestKey_ThrowsArgumentNullExceptionNamingTheKey()
    {
        using var fixture = new GoldenFixture();
        var keyless = fixture.Manifest with { Key = null! };

        var ex = await Assert.ThrowsAsync<ArgumentNullException>(() =>
        {
            _ = new GoldenCorpus([keyless], fixture.Fetcher);
            return Task.CompletedTask;
        });

        await Assert.That(ex!.ParamName).Contains("Key");
    }

    [Test]
    [Arguments("")]
    [Arguments("   ")]
    public async Task Constructor_ManifestKeyWithoutText_ThrowsArgumentException(string key)
    {
        using var fixture = new GoldenFixture();
        var keyless = fixture.Manifest with { Key = key };

        await Assert.That(() => _ = new GoldenCorpus([keyless], fixture.Fetcher)).Throws<ArgumentException>();
    }

    [Test]
    public async Task Constructor_DuplicateSuiteKey_IsRejected()
    {
        using var fixture = new GoldenFixture();

        await Assert
            .That(() => _ = new GoldenCorpus([fixture.Manifest, fixture.Manifest], fixture.Fetcher))
            .Throws<GoldenSourceException>()
            .WithMessageContaining(GoldenFixture.SuiteKey, StringComparison.Ordinal);
    }

    [Test]
    public async Task Constructor_NullArguments_Throw()
    {
        using var fixture = new GoldenFixture();

        await Assert.That(() => _ = new GoldenCorpus(null!, fixture.Fetcher)).Throws<ArgumentNullException>();
        await Assert.That(() => _ = new GoldenCorpus([fixture.Manifest], null!)).Throws<ArgumentNullException>();
    }
}
