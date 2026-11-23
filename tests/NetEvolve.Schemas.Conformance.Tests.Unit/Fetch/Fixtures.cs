namespace NetEvolve.Schemas.Conformance.Tests.Unit.Fetch;

using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using NetEvolve.Schemas.Conformance.Fetch;
using NetEvolve.Schemas.Conformance.Manifest;
#if NET7_0_OR_GREATER
using System.Formats.Tar;
#endif

internal static class Fixtures
{
    public static string Sha256Of(byte[] data) => HexEncoding.ToLower(SHA256.HashData(data));

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

#if NET7_0_OR_GREATER
    public static byte[] TarGz(params (string Name, string Content)[] entries) =>
        TarGz(tar =>
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
        });

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
#else
    public static byte[] TarGz(params (string Name, string Content)[] entries) =>
        TarGz(tar =>
        {
            foreach (var (name, content) in entries)
            {
                WriteEntry(tar, '0', name, Encoding.UTF8.GetBytes(content), linkName: null);
            }
        });

    public static byte[] TarGzSymbolicLink(string name, string target) =>
        TarGz(tar => WriteEntry(tar, '2', name, [], target));

    private static byte[] TarGz(Action<Stream> write)
    {
        using var buffer = new MemoryStream();
        using (var gzip = new GZipStream(buffer, CompressionLevel.Optimal, leaveOpen: true))
        {
            write(gzip);
            gzip.Write(new byte[1024]); // end-of-archive
        }
        return buffer.ToArray();
    }

    // Hand-written PAX tar: System.Formats.Tar is unavailable below .NET 7.
    private static void WriteEntry(Stream tar, char type, string name, byte[] data, string? linkName)
    {
        var record = $" path={name}\n";
        var len = record.Length + 1;
        len =
            (record.Length + len.ToString(System.Globalization.CultureInfo.InvariantCulture).Length) is var l
            && l.ToString(System.Globalization.CultureInfo.InvariantCulture).Length
                == len.ToString(System.Globalization.CultureInfo.InvariantCulture).Length
                ? l
                : l + 1;
        WriteBlock(tar, 'x', "pax", Encoding.UTF8.GetBytes(len + record));
        WriteBlock(tar, type, "entry", data, linkName);
    }

    private static void WriteBlock(Stream tar, char type, string name, byte[] data, string? linkName = null)
    {
        var header = new byte[512];
        Encoding.ASCII.GetBytes(name).CopyTo(header, 0);
        Encoding.ASCII.GetBytes("0000644\0").CopyTo(header, 100);
        Encoding.ASCII.GetBytes(Convert.ToString(data.Length, 8).PadLeft(11, '0') + "\0").CopyTo(header, 124);
        Encoding.ASCII.GetBytes("00000000000\0").CopyTo(header, 136);
        header[156] = (byte)type;
        if (linkName is not null)
        {
            Encoding.UTF8.GetBytes(linkName).CopyTo(header, 157);
        }
        Encoding.ASCII.GetBytes("ustar\000").CopyTo(header, 257);
        for (var i = 148; i < 156; i++)
        {
            header[i] = (byte)' ';
        }
        var sum = header.Sum(b => b);
        Encoding.ASCII.GetBytes(Convert.ToString(sum, 8).PadLeft(6, '0') + "\0 ").CopyTo(header, 148);
        tar.Write(header);
        tar.Write(data);
        tar.Write(new byte[(512 - data.Length % 512) % 512]);
    }
#endif
}
