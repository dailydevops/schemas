namespace NetEvolve.Schemas.Schematron.Tests.Unit;

using System.Security.Cryptography;

public class VendoredSchemaChecksumTests
{
    private const string ChecksumFileName = "checksums.txt";

    [Test]
    public async Task EveryVendoredFile_MatchesPinnedChecksum(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var folder = FindSpecsFolder();
        var entries = ReadChecksumList(folder);

        await Assert.That(entries).IsNotEmpty();

        foreach (var (checksum, name) in entries)
        {
            var path = Path.Combine(folder, name);
            await Assert.That(File.Exists(path)).IsTrue();

            var actual = Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(path, cancellationToken)));
            await Assert.That(actual).IsEqualTo(checksum).IgnoringCase();
        }
    }

    [Test]
    public async Task EveryFileInFolder_IsListedInChecksumList()
    {
        var folder = FindSpecsFolder();
        var listed = ReadChecksumList(folder).Select(e => e.Name).ToHashSet(StringComparer.Ordinal);

        var unlisted = Directory
            .EnumerateFiles(folder, "*", SearchOption.AllDirectories)
            .Select(p => Path.GetRelativePath(folder, p).Replace(Path.DirectorySeparatorChar, '/'))
            .Where(n => !string.Equals(n, ChecksumFileName, StringComparison.Ordinal) && !listed.Contains(n))
            .ToArray();

        await Assert.That(listed).IsNotEmpty();
        await Assert.That(unlisted).IsEmpty();
    }

    // Format: one "<sha256-hex>  <relative path>" per line (sha256sum output).
    private static (string Checksum, string Name)[] ReadChecksumList(string folder) =>
        [
            .. File.ReadAllLines(Path.Combine(folder, ChecksumFileName))
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .Select(l => l.Split("  ", 2))
                .Select(p => (p[0], p[1])),
        ];

    private static string FindSpecsFolder()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, "specs", "schematron");
            if (File.Exists(Path.Combine(dir.FullName, "Schemas.slnx")))
            {
                return candidate;
            }
        }

        throw new DirectoryNotFoundException("Repository root (Schemas.slnx) not found.");
    }
}
