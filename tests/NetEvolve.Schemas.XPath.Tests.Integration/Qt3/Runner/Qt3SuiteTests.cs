namespace NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Runner;

using NetEvolve.Schemas.Conformance.Fetch;
using NetEvolve.Schemas.Conformance.Manifest;

public class Qt3SuiteTests
{
    [Test]
    public async Task Manifest_PinsTheCommitArchiveByChecksum()
    {
        var manifest = Qt3Suite.Manifest;

        await Assert.That(manifest.Key).IsEqualTo("qt3");
        await Assert
            .That(manifest.SourceUrl.AbsoluteUri)
            .IsEqualTo("https://github.com/w3c/qt3tests/archive/201a6e466940cdfc727f4babfedcde5332b9f578.zip");
        await Assert
            .That(manifest.Sha256)
            .IsEqualTo("ba9032e4fa5b71da574e3eab7ff16320b7294f6ba586a5cd060430c55974cf29");
        await Assert.That(manifest.ArchiveType).IsEqualTo(ArchiveType.Zip);
    }

    [Test]
    public async Task GetRootAsync_ReturnsTheFetchedSuiteDirectoryOnEveryCall(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var first = await Qt3Suite.GetRootAsync(cancellationToken);
        var second = await Qt3Suite.GetRootAsync(cancellationToken);

        await Assert.That(File.Exists(Path.Combine(first, "catalog.xml"))).IsTrue();
        await Assert.That(second).IsEqualTo(first);
    }

    [Test]
    public async Task GetRootAsync_CanceledToken_Throws()
    {
        using var source = new CancellationTokenSource();
#if NET8_0_OR_GREATER
        await source.CancelAsync();
#else
        source.Cancel();
#endif

        _ = await Assert.ThrowsAsync<OperationCanceledException>(async () => await Qt3Suite.GetRootAsync(source.Token));
    }

    [Test]
    public async Task Fetch_ArchiveWithDifferentBytes_FailsWithChecksumMismatch(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var temp = new TempDirectory();
        var fetcher = new SuiteFetcher(new FakeTransport([1, 2, 3]), temp.Path);

        var ex = await Assert.ThrowsAsync<SuiteFetchException>(async () =>
            await fetcher.FetchAsync(Qt3Suite.Manifest, cancellationToken)
        );

        await Assert.That(ex!.Message).Contains("checksum mismatch");
        await Assert.That(ex.Message).Contains("qt3");
    }
}
