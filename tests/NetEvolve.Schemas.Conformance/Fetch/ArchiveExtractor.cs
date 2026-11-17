namespace NetEvolve.Schemas.Conformance.Fetch;

using System.Formats.Tar;
using System.IO.Compression;
using NetEvolve.Schemas.Conformance.Manifest;

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
