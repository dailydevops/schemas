namespace NetEvolve.Schemas.Conformance.Fetch;

using System.IO.Compression;
using NetEvolve.Schemas.Conformance.Manifest;
#if NET7_0_OR_GREATER
using System.Formats.Tar;
#elif !NET7_0_OR_GREATER
using System.Text;
#endif

/// <summary>Extracts suite archives and rejects entries that would leave the target directory.</summary>
public static class ArchiveExtractor
{
    /// <summary>Extracts an archive into a directory.</summary>
    /// <param name="archive">A readable (and for zip, seekable) archive stream.</param>
    /// <param name="type">The archive format.</param>
    /// <param name="targetDirectory">The directory to extract into; created when missing.</param>
    /// <exception cref="InvalidDataException">An entry has an absolute path, leaves the target, or is a link.</exception>
    public static void Extract(Stream archive, ArchiveType type, string targetDirectory)
    {
        ArgumentNullException.ThrowIfNull(archive);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetDirectory);

        var root = Path.GetFullPath(targetDirectory);
        switch (type)
        {
            case ArchiveType.Zip:
                ExtractZip(archive, root);
                break;
            case ArchiveType.TarGz:
                ExtractTarGz(archive, root);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown archive type.");
        }
    }

    private static void ExtractZip(Stream archive, string root)
    {
        using var zip = new ZipArchive(archive, ZipArchiveMode.Read, leaveOpen: true);
        foreach (var entry in zip.Entries)
        {
            var path = ResolveEntryPath(root, entry.FullName);
            if (entry.Name.Length == 0)
            {
                Directory.CreateDirectory(path);
                continue;
            }
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            entry.ExtractToFile(path, overwrite: true);
        }
    }

#if NET7_0_OR_GREATER
    private static void ExtractTarGz(Stream archive, string root)
    {
        using var gzip = new GZipStream(archive, CompressionMode.Decompress, leaveOpen: true);
        using var tar = new TarReader(gzip, leaveOpen: true);
        while (tar.GetNextEntry() is { } entry)
        {
            var path = ResolveEntryPath(root, entry.Name);
            switch (entry.EntryType)
            {
                case TarEntryType.Directory:
                    Directory.CreateDirectory(path);
                    break;
                case TarEntryType.RegularFile:
                case TarEntryType.V7RegularFile:
                    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                    entry.ExtractToFile(path, overwrite: true);
                    break;
                case TarEntryType.SymbolicLink:
                case TarEntryType.HardLink:
                    throw new InvalidDataException($"Archive entry '{entry.Name}' is a link, which is not allowed.");
                default:
                    // Metadata entries (for example PAX headers) carry no file content.
                    break;
            }
        }
    }
#else
    private static void ExtractTarGz(Stream archive, string root)
    {
        // Own minimal reader: System.Formats.Tar only exists on .NET 7+.
        using var gzip = new GZipStream(archive, CompressionMode.Decompress, leaveOpen: true);
        var header = new byte[BlockSize];
        string? nextName = null; // from GNU 'L' or PAX 'path'
        long? nextSize = null; // from PAX 'size'
        while (ReadBlock(gzip, header))
        {
            if (Array.TrueForAll(header, static b => b == 0))
            {
                break; // end-of-archive marker
            }

            var type = (char)header[156];
            var size = nextSize ?? ParseOctal(header, 124, 12);
            var name = nextName ?? ReadHeaderName(header);
            nextName = null;
            nextSize = null;

            switch (type)
            {
                case 'x': // PAX extended header: applies to the next entry
                    foreach (var (key, value) in ReadPaxRecords(ReadData(gzip, size)))
                    {
                        if (key == "path")
                        {
                            nextName = value;
                        }
                        else if (key == "size" && long.TryParse(value, out var paxSize))
                        {
                            nextSize = paxSize;
                        }
                    }
                    // The header name of the 'x' entry itself is irrelevant; only PAX values carry over.
                    break;
                case 'L': // GNU long name for the next entry
                    nextName = Encoding.UTF8.GetString(ReadData(gzip, size)).TrimEnd('\0');
                    break;
                case '0':
                case '\0':
                case '7':
                {
                    var path = ResolveEntryPath(root, name);
                    if (name is [.., '/'])
                    {
                        Directory.CreateDirectory(path);
                        Skip(gzip, Padded(size));
                        break;
                    }
                    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                    using (var file = new FileStream(path, FileMode.Create, FileAccess.Write))
                    {
                        CopyExact(gzip, file, size);
                    }
                    Skip(gzip, Padded(size) - size);
                    break;
                }
                case '5':
                    Directory.CreateDirectory(ResolveEntryPath(root, name));
                    Skip(gzip, Padded(size));
                    break;
                case '1':
                case '2':
                    throw new InvalidDataException($"Archive entry '{name}' is a link, which is not allowed.");
                default: // 'g' global PAX headers, 'K', devices, ...: no file content we need
                    Skip(gzip, Padded(size));
                    break;
            }
        }
    }

    private const int BlockSize = 512;

    private static long Padded(long size) => (size + BlockSize - 1) / BlockSize * BlockSize;

    private static bool ReadBlock(Stream s, byte[] buffer)
    {
        var read = 0;
        while (read < buffer.Length)
        {
            var n = s.Read(buffer, read, buffer.Length - read);
            if (n == 0)
            {
                return read == 0 ? false : throw new InvalidDataException("Truncated tar archive.");
            }
            read += n;
        }
        return true;
    }

    private static string ReadHeaderName(byte[] h)
    {
        var name = ReadString(h, 0, 100);
        // ustar stores long paths as prefix + name.
        return h[257] == (byte)'u' && ReadString(h, 345, 155) is { Length: > 0 } prefix ? prefix + "/" + name : name;
    }

    private static string ReadString(byte[] h, int offset, int length)
    {
        var end = Array.IndexOf(h, (byte)0, offset, length);
        return Encoding.UTF8.GetString(h, offset, (end < 0 ? offset + length : end) - offset);
    }

    private static long ParseOctal(byte[] h, int offset, int length)
    {
        if ((h[offset] & 0x80) != 0)
        {
            throw new InvalidDataException("Base-256 tar sizes are not supported.");
        }
        var text = ReadString(h, offset, length).Trim();
        return text.Length == 0 ? 0 : Convert.ToInt64(text, 8);
    }

    private static byte[] ReadData(Stream s, long size)
    {
        var data = new byte[size];
        using var ms = new MemoryStream(data);
        CopyExact(s, ms, size);
        Skip(s, Padded(size) - size);
        return data;
    }

    private static IEnumerable<(string Key, string Value)> ReadPaxRecords(byte[] data)
    {
        // Each record: "<len> <key>=<value>\n", len counts the whole record.
        var pos = 0;
        while (pos < data.Length)
        {
            var space = Array.IndexOf(data, (byte)' ', pos);
            if (space < 0 || !int.TryParse(Encoding.ASCII.GetString(data, pos, space - pos), out var len) || len <= 0)
            {
                throw new InvalidDataException("Malformed PAX header.");
            }
            var text = Encoding.UTF8.GetString(data, space + 1, pos + len - space - 2); // drop trailing '\n'
            var eq = text.IndexOf('=', StringComparison.Ordinal);
            if (eq > 0)
            {
                yield return (text[..eq], text[(eq + 1)..]);
            }
            pos += len;
        }
    }

    private static void CopyExact(Stream source, Stream target, long count)
    {
        var buffer = new byte[81920];
        while (count > 0)
        {
            var n = source.Read(buffer, 0, (int)Math.Min(buffer.Length, count));
            if (n == 0)
            {
                throw new InvalidDataException("Truncated tar archive.");
            }
            target.Write(buffer, 0, n);
            count -= n;
        }
    }

    private static void Skip(Stream s, long count)
    {
        var buffer = new byte[BlockSize];
        while (count > 0)
        {
            var n = s.Read(buffer, 0, (int)Math.Min(buffer.Length, count));
            if (n == 0)
            {
                throw new InvalidDataException("Truncated tar archive.");
            }
            count -= n;
        }
    }
#endif

    private static string ResolveEntryPath(string root, string entryName)
    {
        if (Path.IsPathRooted(entryName))
        {
            throw new InvalidDataException($"Archive entry '{entryName}' has an absolute path.");
        }

        var path = Path.GetFullPath(Path.Combine(root, entryName));
        var prefix = Path.TrimEndingDirectorySeparator(root) + Path.DirectorySeparatorChar;
        if (!path.StartsWith(prefix, StringComparison.Ordinal) && !string.Equals(path, root, StringComparison.Ordinal))
        {
            throw new InvalidDataException($"Archive entry '{entryName}' leaves the target directory.");
        }
        return path;
    }
}
