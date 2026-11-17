namespace NetEvolve.Schemas.Conformance.Tests.Unit.Fetch;

using NetEvolve.Schemas.Conformance.Fetch;
using NetEvolve.Schemas.Conformance.Manifest;

public class ArchiveExtractorTests
{
    [Test]
    [Arguments(ArchiveType.Zip, "../evil.txt")]
    [Arguments(ArchiveType.Zip, "a/../../evil.txt")]
    [Arguments(ArchiveType.Zip, "/abs/evil.txt")]
    [Arguments(ArchiveType.TarGz, "../evil.txt")]
    [Arguments(ArchiveType.TarGz, "a/../../evil.txt")]
    [Arguments(ArchiveType.TarGz, "/abs/evil.txt")]
    public async Task Extract_EntryLeavingTarget_IsRejected(
        ArchiveType type,
        string entryName,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var temp = new TempDirectory();
        var target = Path.Combine(temp.Path, "target");
        var bytes = type == ArchiveType.Zip ? Fixtures.Zip((entryName, "x")) : Fixtures.TarGz((entryName, "x"));
        using var stream = new MemoryStream(bytes);

        await Assert.That(() => ArchiveExtractor.Extract(stream, type, target)).Throws<InvalidDataException>();

        await Assert.That(File.Exists(Path.Combine(temp.Path, "evil.txt"))).IsFalse();
    }

    [Test]
    [Arguments(ArchiveType.Zip)]
    [Arguments(ArchiveType.TarGz)]
    public async Task Extract_NestedEntries_WritesFiles(ArchiveType type, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var temp = new TempDirectory();
        var bytes =
            type == ArchiveType.Zip
                ? Fixtures.Zip(("d/e/f.txt", "f"), ("g.txt", "g"))
                : Fixtures.TarGz(("d/e/f.txt", "f"), ("g.txt", "g"));
        using var stream = new MemoryStream(bytes);

        ArchiveExtractor.Extract(stream, type, temp.Path);

        await Assert
            .That(await File.ReadAllTextAsync(Path.Combine(temp.Path, "d", "e", "f.txt"), cancellationToken))
            .IsEqualTo("f");
        await Assert
            .That(await File.ReadAllTextAsync(Path.Combine(temp.Path, "g.txt"), cancellationToken))
            .IsEqualTo("g");
    }

    [Test]
    public async Task Extract_ZipDirectoryEntry_CreatesDirectory(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var temp = new TempDirectory();
        using var stream = new MemoryStream(Fixtures.Zip(("empty/", string.Empty)));

        ArchiveExtractor.Extract(stream, ArchiveType.Zip, temp.Path);

        await Assert.That(Directory.Exists(Path.Combine(temp.Path, "empty"))).IsTrue();
    }

    [Test]
    public async Task Extract_TarSymbolicLink_IsRejected(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var temp = new TempDirectory();
        using var stream = new MemoryStream(Fixtures.TarGzSymbolicLink("link", "../outside"));

        await Assert
            .That(() => ArchiveExtractor.Extract(stream, ArchiveType.TarGz, temp.Path))
            .Throws<InvalidDataException>();
    }

    [Test]
    public async Task Extract_InvalidArguments_Throw(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var stream = new MemoryStream();

        await Assert.That(() => ArchiveExtractor.Extract(null!, ArchiveType.Zip, "x")).Throws<ArgumentNullException>();
        await Assert.That(() => ArchiveExtractor.Extract(stream, ArchiveType.Zip, " ")).Throws<ArgumentException>();
        await Assert
            .That(() => ArchiveExtractor.Extract(stream, (ArchiveType)99, "x"))
            .Throws<ArgumentOutOfRangeException>();
    }
}
