namespace NetEvolve.Schemas.Xml.Tests.Unit.Text.Unicode;

using System.Globalization;
using NetEvolve.Schemas.Conformance.Fetch;
using NetEvolve.Schemas.Conformance.Manifest;
using NetEvolve.Schemas.Xml.Text.Unicode;

/// <summary>The pinned Unicode Character Database files, parsed into per-code-point oracles.</summary>
internal sealed class UcdData
{
    internal const int CodePointCount = 0x110000;

    private static readonly SemaphoreSlim Gate = new(1, 1);
    private static UcdData? _cached;

    private UcdData(
        SuiteManifest manifest,
        byte[] categories,
        List<(int First, int Last, string Name)> blocks,
        short[] blockOfCodePoint,
        List<(int Key, int Value)> folds
    )
    {
        Manifest = manifest;
        Categories = categories;
        Blocks = blocks;
        BlockOfCodePoint = blockOfCodePoint;
        Folds = folds;
    }

    internal SuiteManifest Manifest { get; }

    /// <summary>Gets the <see cref="UnicodeGeneralCategory"/> value per code point; unlisted code points are Cn.</summary>
    internal byte[] Categories { get; }

    /// <summary>Gets the <c>Blocks.txt</c> rows in file order.</summary>
    internal List<(int First, int Last, string Name)> Blocks { get; }

    /// <summary>Gets the index into <see cref="Blocks"/> per code point, or -1 for No_Block.</summary>
    internal short[] BlockOfCodePoint { get; }

    /// <summary>Gets the simple foldings (statuses C and S of <c>CaseFolding.txt</c>), sorted by key.</summary>
    internal List<(int Key, int Value)> Folds { get; }

    internal static async Task<UcdData> GetAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await Gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return _cached ??= await LoadAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _ = Gate.Release();
        }
    }

    internal static string RepositoryRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Schemas.slnx")))
            {
                return dir.FullName;
            }
        }

        throw new DirectoryNotFoundException("Repository root not found.");
    }

    /// <summary>The block name without white space and underscores; hyphens and case are kept (XSD 1.1 G.4.2.3).</summary>
    internal static string Normalize(string blockName) =>
        blockName
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .Replace("_", string.Empty, StringComparison.Ordinal);

    private static async Task<UcdData> LoadAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var path = Path.Combine(RepositoryRoot(), "tests", "conformance", "suites", "ucd.json");
        var manifest = SuiteManifestLoader.Parse(
            await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false),
            path
        );
        using var http = new HttpClient();
        var fetcher = new SuiteFetcher(new HttpSuiteTransport(http), ConformanceCache.ResolveRoot());
        var root = await fetcher.FetchAsync(manifest, cancellationToken).ConfigureAwait(false);

        var unicodeData = await File.ReadAllLinesAsync(Path.Combine(root, "UnicodeData.txt"), cancellationToken)
            .ConfigureAwait(false);
        var blockLines = await File.ReadAllLinesAsync(Path.Combine(root, "Blocks.txt"), cancellationToken)
            .ConfigureAwait(false);
        var foldLines = await File.ReadAllLinesAsync(Path.Combine(root, "CaseFolding.txt"), cancellationToken)
            .ConfigureAwait(false);

        var (blocks, blockOf) = ParseBlocks(blockLines);
        return new UcdData(manifest, ParseCategories(unicodeData), blocks, blockOf, ParseFolds(foldLines));
    }

    private static byte[] ParseCategories(string[] lines)
    {
        var categories = new byte[CodePointCount];
        Array.Fill(categories, (byte)UnicodeGeneralCategory.Cn);
        var rangeStart = -1;
        foreach (var line in lines)
        {
            var fields = line.Split(';');
            var codePoint = Hex(fields[0]);
            var category = (byte)Enum.Parse<UnicodeGeneralCategory>(fields[2]);
            if (fields[1].EndsWith(", First>", StringComparison.Ordinal))
            {
                rangeStart = codePoint;
            }
            else if (fields[1].EndsWith(", Last>", StringComparison.Ordinal))
            {
                Array.Fill(categories, category, rangeStart, codePoint - rangeStart + 1);
                rangeStart = -1;
            }
            else
            {
                categories[codePoint] = category;
            }
        }

        return categories;
    }

    private static (List<(int First, int Last, string Name)> Blocks, short[] BlockOf) ParseBlocks(string[] lines)
    {
        var blocks = new List<(int First, int Last, string Name)>();
        var blockOf = new short[CodePointCount];
        Array.Fill(blockOf, (short)-1);
        foreach (var line in lines)
        {
            if (line.Length == 0 || line[0] == '#')
            {
                continue;
            }

            var semicolon = line.IndexOf(';', StringComparison.Ordinal);
            var range = line[..semicolon].Split("..");
            var first = Hex(range[0]);
            var last = Hex(range[1]);
            Array.Fill(blockOf, (short)blocks.Count, first, last - first + 1);
            blocks.Add((first, last, line[(semicolon + 1)..].Trim()));
        }

        return (blocks, blockOf);
    }

    private static List<(int Key, int Value)> ParseFolds(string[] lines)
    {
        var folds = new List<(int Key, int Value)>();
        foreach (var line in lines)
        {
            if (line.Length == 0 || line[0] == '#')
            {
                continue;
            }

            var fields = line.Split(';');
            if (fields[1].Trim() is "C" or "S")
            {
                folds.Add((Hex(fields[0]), Hex(fields[2])));
            }
        }

        folds.Sort(static (a, b) => a.Key.CompareTo(b.Key));
        return folds;
    }

    private static int Hex(string value) =>
        int.Parse(value.Trim(), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture);
}
