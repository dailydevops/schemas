#if NET10_0_OR_GREATER
using NetEvolve.Schemas.Conformance.Fetch;
using NetEvolve.Schemas.Conformance.Manifest;

namespace NetEvolve.Schemas.Conformance.Tests.Unit.Fetch;

public class SuiteFetcherTests
{
    [Test]
    public async Task FetchAsync_ChecksumMismatch_ThrowsNamingSuiteKeyAndExtractsNothing()
    {
        using var temp = new TempDirectory();
        var archive = Fixtures.Zip(("a.txt", "a"));
        var manifest = Fixtures.Manifest("suite-x", archive, ArchiveType.Zip) with { Sha256 = new string('0', 64) };
        var fetcher = new SuiteFetcher(new FakeTransport(archive), temp.Path);

        var ex = await Assert.ThrowsAsync<SuiteFetchException>(async () => await fetcher.FetchAsync(manifest));

        await Assert.That(ex.Message).Contains("suite-x");
        await Assert.That(Directory.GetFileSystemEntries(temp.Path, "*", SearchOption.AllDirectories)).IsEmpty();
    }

    [Test]
    public async Task FetchAsync_FilledCache_PerformsNoDownload()
    {
        using var temp = new TempDirectory();
        var archive = Fixtures.Zip(("a.txt", "hello"));
        var manifest = Fixtures.Manifest("suite-x", archive, ArchiveType.Zip);
        var transport = new FakeTransport(archive);
        var fetcher = new SuiteFetcher(transport, temp.Path);

        var first = await fetcher.FetchAsync(manifest);
        var second = await fetcher.FetchAsync(manifest);

        await Assert.That(transport.Calls).IsEqualTo(1);
        await Assert.That(second).IsEqualTo(first);
        await Assert.That(await File.ReadAllTextAsync(Path.Combine(second, "a.txt"))).IsEqualTo("hello");
    }

    [Test]
    public async Task FetchAsync_CacheIsKeyedByChecksum()
    {
        using var temp = new TempDirectory();
        var archive = Fixtures.Zip(("a.txt", "a"));
        var manifest = Fixtures.Manifest("suite-x", archive, ArchiveType.Zip);

        var path = await new SuiteFetcher(new FakeTransport(archive), temp.Path).FetchAsync(manifest);

        await Assert.That(Path.GetFileName(path)).IsEqualTo(manifest.Sha256);
    }

    [Test]
    public async Task FetchAsync_TarGzWithSubPath_ReturnsSubDirectory()
    {
        using var temp = new TempDirectory();
        var archive = Fixtures.TarGz(("root/data/b.txt", "b"), ("root/other.txt", "o"));
        var manifest = Fixtures.Manifest("suite-t", archive, ArchiveType.TarGz, "root/data");

        var path = await new SuiteFetcher(new FakeTransport(archive), temp.Path).FetchAsync(manifest);

        await Assert.That(await File.ReadAllTextAsync(Path.Combine(path, "b.txt"))).IsEqualTo("b");
    }

    [Test]
    public async Task FetchAsync_MissingSubPath_Throws()
    {
        using var temp = new TempDirectory();
        var archive = Fixtures.Zip(("a.txt", "a"));
        var manifest = Fixtures.Manifest("suite-x", archive, ArchiveType.Zip, "nope");

        var ex = await Assert.ThrowsAsync<SuiteFetchException>(async () =>
            await new SuiteFetcher(new FakeTransport(archive), temp.Path).FetchAsync(manifest)
        );

        await Assert.That(ex.Message).Contains("suite-x");
    }

    [Test]
    public async Task FetchAsync_UnsafeArchive_FailsAndLeavesNoCacheEntry()
    {
        using var temp = new TempDirectory();
        var archive = Fixtures.Zip(("ok.txt", "x"), ("../evil.txt", "x"));
        var manifest = Fixtures.Manifest("suite-x", archive, ArchiveType.Zip);

        var ex = await Assert.ThrowsAsync<SuiteFetchException>(async () =>
            await new SuiteFetcher(new FakeTransport(archive), temp.Path).FetchAsync(manifest)
        );

        await Assert.That(ex.Message).Contains("suite-x");
        await Assert.That(Directory.GetFileSystemEntries(temp.Path, "*", SearchOption.AllDirectories)).IsEmpty();
    }

    [Test]
    public async Task FetchAsync_InvalidArguments_Throw()
    {
        using var temp = new TempDirectory();
        var fetcher = new SuiteFetcher(new FakeTransport([]), temp.Path);

        await Assert.ThrowsAsync<ArgumentNullException>(async () => await fetcher.FetchAsync(null!));
        await Assert.That(() => new SuiteFetcher(null!, temp.Path)).Throws<ArgumentNullException>();
        await Assert.That(() => new SuiteFetcher(new FakeTransport([]), " ")).Throws<ArgumentException>();
    }
}
#endif
