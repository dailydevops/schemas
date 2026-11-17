#if NET10_0_OR_GREATER
using System.Formats.Tar;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using NetEvolve.Schemas.Conformance.Fetch;
using NetEvolve.Schemas.Conformance.Manifest;

namespace NetEvolve.Schemas.Conformance.Tests.Unit.Fetch;

internal sealed class TempDirectory : IDisposable
{
    public TempDirectory() => Path = Directory.CreateTempSubdirectory("conformance-").FullName;

    public string Path { get; }

    public void Dispose() => Directory.Delete(Path, recursive: true);
}

internal sealed class FakeTransport(byte[] payload) : ISuiteTransport
{
    public int Calls { get; private set; }

    public Task<Stream> OpenAsync(Uri source, CancellationToken cancellationToken)
    {
        Calls++;
        return Task.FromResult<Stream>(new MemoryStream(payload, writable: false));
    }
}

internal static class Fixtures
{
    public static string Sha256Of(byte[] data) => Convert.ToHexStringLower(SHA256.HashData(data));

    public static SuiteManifest Manifest(string key, byte[] archive, ArchiveType type, string? subPath = null) =>
        new(key, new Uri("https://example.invalid/" + key), Sha256Of(archive), type, subPath);

    public static byte[] Zip(params (string Name, string Content)[] entries)
    {
        using var buffer = new MemoryStream();
        using (var zip = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var (name, content) in entries)
            {
                var entry = zip.CreateEntry(name);
                using var writer = new StreamWriter(entry.Open(), Encoding.UTF8);
                writer.Write(content);
            }
        }
        return buffer.ToArray();
    }

    public static byte[] TarGz(params (string Name, string Content)[] entries) =>
        TarGz(
            tar =>
            {
                foreach (var (name, content) in entries)
                {
                    tar.WriteEntry(
                        new PaxTarEntry(TarEntryType.RegularFile, name)
                        {
                            DataStream = new MemoryStream(Encoding.UTF8.GetBytes(content)),
                        }
                    );
                }
            }
        );

    public static byte[] TarGzSymbolicLink(string name, string target) =>
        TarGz(tar => tar.WriteEntry(new PaxTarEntry(TarEntryType.SymbolicLink, name) { LinkName = target }));

    private static byte[] TarGz(Action<TarWriter> write)
    {
        using var buffer = new MemoryStream();
        using (var gzip = new GZipStream(buffer, CompressionLevel.Optimal, leaveOpen: true))
        using (var tar = new TarWriter(gzip, TarEntryFormat.Pax, leaveOpen: true))
        {
            write(tar);
        }
        return buffer.ToArray();
    }
}
#endif
