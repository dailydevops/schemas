namespace NetEvolve.Schemas.Xml.Tests.Unit.Text.Unicode;

using System.Text;
using NetEvolve.Schemas.Testing.Allocations;
using NetEvolve.Schemas.Xml.Text.Unicode;

public class UnicodeTablesTests
{
    private const int MaxReported = 10;

    private static async Task AssertNoMismatch(List<string> failures) =>
        await Assert.That(string.Join(", ", failures)).IsEqualTo(string.Empty);

    [Test]
    public async Task GetCategory_EveryCodePoint_MatchesPinnedUnicodeData(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var ucd = await UcdData.GetAsync(cancellationToken);
        var failures = new List<string>();
        for (var cp = 0; cp < UcdData.CodePointCount && failures.Count < MaxReported; cp++)
        {
            if ((byte)UnicodeTables.GetCategory(cp) != ucd.Categories[cp])
            {
                failures.Add($"U+{cp:X4}");
            }
        }

        await AssertNoMismatch(failures);
    }

    [Test]
    public async Task GetBlock_EveryCodePoint_MatchesPinnedBlocks(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var ucd = await UcdData.GetAsync(cancellationToken);
        var failures = new List<string>();
        for (var cp = 0; cp < UcdData.CodePointCount && failures.Count < MaxReported; cp++)
        {
            if (UnicodeTables.GetBlock(cp) != ucd.BlockOfCodePoint[cp])
            {
                failures.Add($"U+{cp:X4}");
            }
        }

        await AssertNoMismatch(failures);
    }

    [Test]
    public async Task GetBlockName_EveryBlock_MatchesPinnedBlocks(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var ucd = await UcdData.GetAsync(cancellationToken);
        var failures = new List<string>();
        for (var i = 0; i < ucd.Blocks.Count; i++)
        {
            if (!string.Equals(UnicodeTables.GetBlockName(i), ucd.Blocks[i].Name, StringComparison.Ordinal))
            {
                failures.Add(ucd.Blocks[i].Name);
            }
        }

        await AssertNoMismatch(failures);
        await Assert.That(UnicodeTables.GetBlockName(UnicodeTables.NoBlock)).IsEqualTo("No_Block");
        await Assert.That(UnicodeTables.GetBlockName(ucd.Blocks.Count)).IsEqualTo("No_Block");
    }

    [Test]
    public async Task SimpleCaseFold_EveryCodePoint_MatchesPinnedCaseFolding(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var ucd = await UcdData.GetAsync(cancellationToken);
        var expected = new int[UcdData.CodePointCount];
        for (var cp = 0; cp < expected.Length; cp++)
        {
            expected[cp] = cp;
        }

        foreach (var (key, value) in ucd.Folds)
        {
            expected[key] = value;
        }

        var failures = new List<string>();
        for (var cp = 0; cp < expected.Length && failures.Count < MaxReported; cp++)
        {
            if (UnicodeTables.SimpleCaseFold(cp) != expected[cp])
            {
                failures.Add($"U+{cp:X4}");
            }
        }

        await AssertNoMismatch(failures);
    }

    [Test]
    public async Task Version_IsThePinnedManifestVersion(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var ucd = await UcdData.GetAsync(cancellationToken);

        await Assert.That(ucd.Manifest.SourceUrl.AbsolutePath).Contains($"/{UnicodeTables.Version}/");
    }

    [Test]
    [Arguments(0x0378, "Cn")]
    [Arguments(0xFDD0, "Cn")]
    [Arguments(0xFFFE, "Cn")]
    [Arguments(0x10FFFF, "Cn")]
    [Arguments(0xD800, "Cs")]
    [Arguments(0xDB7F, "Cs")]
    [Arguments(0xDB80, "Cs")]
    [Arguments(0xDBFF, "Cs")]
    [Arguments(0xDC00, "Cs")]
    [Arguments(0xDFFF, "Cs")]
    [Arguments(0xE000, "Co")]
    [Arguments(0x100000, "Co")]
    [Arguments(-1, "Cn")]
    [Arguments(0x110000, "Cn")]
    [Arguments(int.MaxValue, "Cn")]
    [Arguments(int.MinValue, "Cn")]
    public async Task GetCategory_UnassignedSurrogateAndPrivateUse_ReturnsDocumentedCategory(
        int codePoint,
        string expected
    ) => await Assert.That(UnicodeTables.GetCategory(codePoint).ToString()).IsEqualTo(expected);

    [Test]
    [Arguments(0x41, "Lu")]
    [Arguments(0x61, "Ll")]
    [Arguments(0x01C5, "Lt")]
    [Arguments(0x02B0, "Lm")]
    [Arguments(0x3042, "Lo")]
    [Arguments(0x0300, "Mn")]
    [Arguments(0x0903, "Mc")]
    [Arguments(0x20DD, "Me")]
    [Arguments(0x31, "Nd")]
    [Arguments(0x2160, "Nl")]
    [Arguments(0xB2, "No")]
    [Arguments(0x5F, "Pc")]
    [Arguments(0x2D, "Pd")]
    [Arguments(0x28, "Ps")]
    [Arguments(0x29, "Pe")]
    [Arguments(0xAB, "Pi")]
    [Arguments(0xBB, "Pf")]
    [Arguments(0x21, "Po")]
    [Arguments(0x2B, "Sm")]
    [Arguments(0x20AC, "Sc")]
    [Arguments(0x5E, "Sk")]
    [Arguments(0x1F600, "So")]
    [Arguments(0x20, "Zs")]
    [Arguments(0x2028, "Zl")]
    [Arguments(0x2029, "Zp")]
    [Arguments(0x0A, "Cc")]
    [Arguments(0xAD, "Cf")]
    [Arguments(0x4E00, "Lo")]
    [Arguments(0x20000, "Lo")]
    public async Task GetCategory_KnownCodePoints_ReturnsCategory(int codePoint, string expected) =>
        await Assert.That(UnicodeTables.GetCategory(codePoint).ToString()).IsEqualTo(expected);

    [Test]
    [Arguments(0x41, "Basic Latin")]
    [Arguments(0xE9, "Latin-1 Supplement")]
    [Arguments(0x0378, "Greek and Coptic")]
    [Arguments(0x1F600, "Emoticons")]
    [Arguments(0x10FFFF, "Supplementary Private Use Area-B")]
    [Arguments(0xE0080, "No_Block")]
    [Arguments(-1, "No_Block")]
    [Arguments(0x110000, "No_Block")]
    public async Task GetBlock_KnownCodePoints_ReturnsBlockName(int codePoint, string expected) =>
        await Assert.That(UnicodeTables.GetBlockName(UnicodeTables.GetBlock(codePoint))).IsEqualTo(expected);

    [Test]
    [Arguments(0x41, 0x61)]
    [Arguments(0x5A, 0x7A)]
    [Arguments(0x61, 0x61)]
    [Arguments(0xC0, 0xE0)]
    [Arguments(0x1E9E, 0xDF)]
    [Arguments(0xDF, 0xDF)]
    [Arguments(0x0130, 0x0130)]
    [Arguments(0x03A3, 0x03C3)]
    [Arguments(0x03C2, 0x03C3)]
    [Arguments(0x10400, 0x10428)]
    [Arguments(0xAB70, 0x13A0)]
    [Arguments(0x10FFFF, 0x10FFFF)]
    [Arguments(-1, -1)]
    [Arguments(0x110000, 0x110000)]
    public async Task SimpleCaseFold_KnownCodePoints_ReturnsFolding(int codePoint, int expected) =>
        await Assert.That(UnicodeTables.SimpleCaseFold(codePoint)).IsEqualTo(expected);

    [Test]
    [Arguments("BasicLatin", 0x41, true)]
    [Arguments("BasicLatin", 0x80, false)]
    [Arguments("Latin-1Supplement", 0xE9, true)]
    [Arguments("GreekandCoptic", 0x3A9, true)]
    [Arguments("Greek", 0x3A9, true)]
    [Arguments("Greek", 0x370, true)]
    [Arguments("Greek", 0x3FF, true)]
    [Arguments("Greek", 0x400, false)]
    [Arguments("CombiningMarksforSymbols", 0x20D0, true)]
    [Arguments("CombiningMarksforSymbols", 0x20FF, true)]
    [Arguments("CombiningMarksforSymbols", 0x2100, false)]
    [Arguments("CombiningDiacriticalMarksforSymbols", 0x20D0, true)]
    [Arguments("PrivateUse", 0xE000, true)]
    [Arguments("PrivateUse", 0xF8FF, true)]
    [Arguments("PrivateUse", 0xF900, false)]
    [Arguments("PrivateUse", 0xF0000, true)]
    [Arguments("PrivateUse", 0xFFFFD, true)]
    [Arguments("PrivateUse", 0xFFFFE, false)]
    [Arguments("PrivateUse", 0x100000, true)]
    [Arguments("PrivateUse", 0x10FFFD, true)]
    [Arguments("PrivateUse", 0x10FFFE, false)]
    [Arguments("PrivateUse", 0x41, false)]
    [Arguments("PrivateUseArea", 0xE000, true)]
    public async Task FindBlock_NormalizedAndSupersededNames_ResolveToTheirRanges(
        string name,
        int codePoint,
        bool expected
    )
    {
        var handle = UnicodeTables.FindBlock(name.AsSpan());

        await Assert.That(handle).IsNotEqualTo(UnicodeTables.NoBlock);
        await Assert.That(UnicodeTables.IsInBlock(handle, codePoint)).IsEqualTo(expected);
    }

    [Test]
    [Arguments("")]
    [Arguments("basiclatin")]
    [Arguments("Basic Latin")]
    [Arguments("IsBasicLatin")]
    [Arguments("NoBlock")]
    [Arguments("Zzz")]
    public async Task FindBlock_UnknownName_ReturnsNoBlock(string name)
    {
        var handle = UnicodeTables.FindBlock(name.AsSpan());

        await Assert.That(handle).IsEqualTo(UnicodeTables.NoBlock);
        await Assert.That(UnicodeTables.IsInBlock(handle, 0x41)).IsFalse();
    }

    [Test]
    public async Task FindBlock_EveryPinnedBlock_ResolvesByNormalizedNameToExactlyItsRange(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var ucd = await UcdData.GetAsync(cancellationToken);
        var failures = new List<string>();
        foreach (var (first, last, name) in ucd.Blocks)
        {
            var handle = UnicodeTables.FindBlock(UcdData.Normalize(name).AsSpan());
            if (
                handle == UnicodeTables.NoBlock
                || !UnicodeTables.IsInBlock(handle, first)
                || !UnicodeTables.IsInBlock(handle, last)
                || UnicodeTables.IsInBlock(handle, first - 1)
                || UnicodeTables.IsInBlock(handle, last + 1)
            )
            {
                failures.Add(name);
            }
        }

        await AssertNoMismatch(failures);
    }

    [Test]
    public async Task Lookups_AllocateNothing()
    {
        var sink = 0;

        AllocationAssert.NoAllocation(() =>
        {
            for (var cp = 0; cp < 0x110000; cp += 97)
            {
                sink += (int)UnicodeTables.GetCategory(cp);
                sink += UnicodeTables.GetBlock(cp);
                sink += UnicodeTables.SimpleCaseFold(cp);
            }

            sink += UnicodeTables.GetBlockName(UnicodeTables.GetBlock(0x41)).Length;
            sink += UnicodeTables.IsInBlock(UnicodeTables.FindBlock("BasicLatin".AsSpan()), 0x41) ? 1 : 0;
            sink += UnicodeTables.IsInBlock(UnicodeTables.FindBlock("PrivateUse".AsSpan()), 0x100000) ? 1 : 0;
        });

        await Assert.That(sink).IsNotEqualTo(0);
    }

    [Test]
    public async Task LookupSources_UseNoLinqAndNoRuntimeUnicodeData(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var folder = Path.Combine(UcdData.RepositoryRoot(), "src", "NetEvolve.Schemas.Xml", "Text", "Unicode");
        string[] forbidden =
        [
            "System.Linq",
            "CharUnicodeInfo",
            "GetUnicodeCategory",
            "UnicodeCategory.",
            "char.Is",
            "char.To",
            "ToUpperInvariant",
            "ToLowerInvariant",
            "CompareInfo",
        ];
        var hits = new List<string>();
        foreach (var file in Directory.GetFiles(folder, "*.cs"))
        {
            var text = await File.ReadAllTextAsync(file, cancellationToken);
            foreach (var token in forbidden)
            {
                if (text.Contains(token, StringComparison.Ordinal))
                {
                    hits.Add($"{Path.GetFileName(file)}: {token}");
                }
            }
        }

        await Assert.That(string.Join(", ", hits)).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task CommittedTables_AreReproducibleFromThePinnedFiles(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var ucd = await UcdData.GetAsync(cancellationToken);
        var generated = UnicodeTableGenerator.Generate(ucd);
        var path = UnicodeTableGenerator.CommittedPath();
        if (Environment.GetEnvironmentVariable(UnicodeTableGenerator.RegenerateVariable) is { Length: > 0 })
        {
            await File.WriteAllTextAsync(
                path,
                generated,
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
                cancellationToken
            );
        }

        var committed = (await File.ReadAllTextAsync(path, cancellationToken)).Replace(
            "\r\n",
            "\n",
            StringComparison.Ordinal
        );

        await Assert.That(committed).IsEqualTo(generated);
    }
}
