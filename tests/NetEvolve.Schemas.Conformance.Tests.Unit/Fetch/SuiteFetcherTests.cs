namespace NetEvolve.Schemas.Conformance.Tests.Unit.Fetch;

using NetEvolve.Schemas.Conformance.Fetch;
using NetEvolve.Schemas.Conformance.Manifest;

public class SuiteFetcherTests
{
    [Test]
    public async Task FetchAsync_ChecksumMismatch_ThrowsNamingSuiteKeyAndExtractsNothing(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var temp = new TempDirectory();
        var archive = Fixtures.Zip(("a.txt", "a"));
        var manifest = Fixtures.Manifest("suite-x", archive, ArchiveType.Zip) with { Sha256 = new string('0', 64) };
        var fetcher = new SuiteFetcher(new FakeTransport(archive), temp.Path);

        var ex = await Assert.ThrowsAsync<SuiteFetchException>(async () =>
            await fetcher.FetchAsync(manifest, cancellationToken)
        );

        await Assert.That(ex!.Message).Contains("suite-x");
        await Assert.That(Directory.GetFileSystemEntries(temp.Path, "*", SearchOption.AllDirectories)).IsEmpty();
    }

    [Test]
    public async Task FetchAsync_FilledCache_PerformsNoDownload(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var temp = new TempDirectory();
        var archive = Fixtures.Zip(("a.txt", "hello"));
        var manifest = Fixtures.Manifest("suite-x", archive, ArchiveType.Zip);
        var transport = new FakeTransport(archive);
        var fetcher = new SuiteFetcher(transport, temp.Path);

        var first = await fetcher.FetchAsync(manifest, cancellationToken);
        var second = await fetcher.FetchAsync(manifest, cancellationToken);

        await Assert.That(transport.Calls).IsEqualTo(1);
        await Assert.That(second).IsEqualTo(first);
        await Assert
            .That(await File.ReadAllTextAsync(Path.Combine(second, "a.txt"), cancellationToken))
            .IsEqualTo("hello");
    }

    [Test]
    public async Task FetchAsync_CacheIsKeyedByChecksum(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var temp = new TempDirectory();
        var archive = Fixtures.Zip(("a.txt", "a"));
        var manifest = Fixtures.Manifest("suite-x", archive, ArchiveType.Zip);

        var path = await new SuiteFetcher(new FakeTransport(archive), temp.Path).FetchAsync(
            manifest,
            cancellationToken
        );

        await Assert.That(Path.GetFileName(path)).IsEqualTo(manifest.Sha256);
    }

    [Test]
    public async Task FetchAsync_TarGzWithSubPath_ReturnsSubDirectory(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var temp = new TempDirectory();
        var archive = Fixtures.TarGz(("root/data/b.txt", "b"), ("root/other.txt", "o"));
        var manifest = Fixtures.Manifest("suite-t", archive, ArchiveType.TarGz, "root/data");

        var path = await new SuiteFetcher(new FakeTransport(archive), temp.Path).FetchAsync(
            manifest,
            cancellationToken
        );

        await Assert.That(await File.ReadAllTextAsync(Path.Combine(path, "b.txt"), cancellationToken)).IsEqualTo("b");
    }

    [Test]
    public async Task FetchAsync_MissingSubPath_Throws(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var temp = new TempDirectory();
        var archive = Fixtures.Zip(("a.txt", "a"));
        var manifest = Fixtures.Manifest("suite-x", archive, ArchiveType.Zip, "nope");

        var ex = await Assert.ThrowsAsync<SuiteFetchException>(async () =>
            await new SuiteFetcher(new FakeTransport(archive), temp.Path).FetchAsync(manifest, cancellationToken)
        );

        await Assert.That(ex!.Message).Contains("suite-x");
    }

    [Test]
    public async Task FetchAsync_UnsafeArchive_FailsAndLeavesNoCacheEntry(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var temp = new TempDirectory();
        var archive = Fixtures.Zip(("ok.txt", "x"), ("../evil.txt", "x"));
        var manifest = Fixtures.Manifest("suite-x", archive, ArchiveType.Zip);

        var ex = await Assert.ThrowsAsync<SuiteFetchException>(async () =>
            await new SuiteFetcher(new FakeTransport(archive), temp.Path).FetchAsync(manifest, cancellationToken)
        );

        await Assert.That(ex!.Message).Contains("suite-x");
        await Assert.That(Directory.GetFileSystemEntries(temp.Path, "*", SearchOption.AllDirectories)).IsEmpty();
    }

    [Test]
    public async Task FetchAsync_InvalidArguments_Throw(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var temp = new TempDirectory();
        var fetcher = new SuiteFetcher(new FakeTransport([]), temp.Path);

        await Assert.ThrowsAsync<ArgumentNullException>(async () => await fetcher.FetchAsync(null!, cancellationToken));
        await Assert.That(() => new SuiteFetcher(null!, temp.Path)).Throws<ArgumentNullException>();
        await Assert.That(() => new SuiteFetcher(new FakeTransport([]), " ")).Throws<ArgumentException>();
    }
}
