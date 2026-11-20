namespace NetEvolve.Schemas.Xml.Tests.Unit.Regex.XPathDialect;

using NetEvolve.Schemas.Xml.Regex.XPathDialect;
using NetEvolve.Schemas.Xml.Text.Unicode;

public class RegexCaseVariantsTests
{
    [Test]
    [Arguments('a', new[] { 0x41, 0x61 })]
    [Arguments('A', new[] { 0x41, 0x61 })]
    [Arguments('k', new[] { 0x4B, 0x6B, 0x212A })]
    [Arguments(0x212A, new[] { 0x4B, 0x6B, 0x212A })]
    [Arguments('s', new[] { 0x53, 0x73, 0x17F })]
    [Arguments(0x1C5, new[] { 0x1C4, 0x1C5, 0x1C6 })]
    [Arguments(0x3C2, new[] { 0x3A3, 0x3C2, 0x3C3 })]
    [Arguments(0xDF, new[] { 0xDF, 0x1E9E })]
    [Arguments(0x10400, new[] { 0x10400, 0x10428 })]
    public async Task TryGetVariants_ReturnsTheWholeClassInAscendingOrder(int codePoint, int[] expected)
    {
        var variants = VariantsOf(codePoint);

        await Assert.That(variants).IsEquivalentTo(expected);
        await Assert.That(variants![0]).IsLessThan(variants[^1]);
    }

    [Test]
    [Arguments('1')]
    [Arguments(' ')]
    [Arguments(0x130)]
    [Arguments(0x131)]
    [Arguments(0x4E00)]
    [Arguments(0x10FFFF)]
    [Arguments(-1)]
    [Arguments(0x110000)]
    public async Task TryGetVariants_OfACodePointWithoutVariants_ReturnsFalse(int codePoint) =>
        await Assert.That(RegexCaseVariants.TryGetVariants(codePoint, out _)).IsFalse();

    [Test]
    public async Task Close_AddsTheVariantsOfEveryMember()
    {
        var closed = RegexCaseVariants.Close([0x41, 0x5A, 0x31, 0x31]);

        await Assert.That(Contains(closed, 'a')).IsTrue();
        await Assert.That(Contains(closed, 'z')).IsTrue();
        await Assert.That(Contains(closed, 0x212A)).IsTrue();
        await Assert.That(Contains(closed, 0x17F)).IsTrue();
        await Assert.That(Contains(closed, '1')).IsTrue();
        await Assert.That(Contains(closed, '0')).IsFalse();
        await Assert.That(Contains(closed, 0x131)).IsFalse();
    }

    [Test]
    public async Task Close_OfAnEmptySet_IsEmpty() =>
        await Assert.That(RegexCaseVariants.Close([]).Length).IsEqualTo(0);

    [Test]
    public async Task Close_OfASetWithoutCasedMembers_KeepsItUnchanged()
    {
        var set = new[] { 0x30, 0x39, 0x4E00, 0x4E10 };

        await Assert.That(RegexCaseVariants.Close(set)).IsEquivalentTo(set);
    }

    [Test]
    public async Task Close_IsIdempotent_AndSymmetric()
    {
        var once = RegexCaseVariants.Close([0x6B, 0x6B]);
        var twice = RegexCaseVariants.Close(once);

        await Assert.That(twice).IsEquivalentTo(once);
        await Assert.That(RegexCaseVariants.Close([0x212A, 0x212A])).IsEquivalentTo(once);
    }

    [Test]
    public async Task ScanLimit_CoversEveryCodePointWithASimpleFolding()
    {
        // The tables are built by scanning up to the limit; a Unicode bump that folds a code point above it fails here.
        var folding = 0;
        for (var cp = RegexCaseVariants.ScanLimit + 1; cp <= 0x10FFFF; cp++)
        {
            if (UnicodeTables.SimpleCaseFold(cp) != cp)
            {
                folding++;
            }
        }

        await Assert.That(folding).IsEqualTo(0);
    }

    [Test]
    public async Task Classes_AreTheSameOnEveryFramework()
    {
        // FNV-1a over every code point of a class and its variants, in code point order; the value is fixed by the pinned tables.
        var hash = 2166136261u;
        var classes = 0;
        for (var cp = 0; cp <= RegexCaseVariants.ScanLimit; cp++)
        {
            if (!RegexCaseVariants.TryGetVariants(cp, out var variants))
            {
                continue;
            }

            classes++;
            hash = (hash ^ (uint)cp) * 16777619u;
            for (var i = 0; i < variants.Length; i++)
            {
                hash = (hash ^ (uint)variants[i]) * 16777619u;
            }
        }

        await Assert.That(classes).IsEqualTo(ExpectedMembers);
        await Assert.That(hash).IsEqualTo(ExpectedHash);
    }

    private const int ExpectedMembers = 0;
    private const uint ExpectedHash = 0u;

    private static int[]? VariantsOf(int codePoint) =>
        RegexCaseVariants.TryGetVariants(codePoint, out var variants) ? variants.ToArray() : null;

    private static bool Contains(int[] ranges, int codePoint)
    {
        for (var i = 0; i < ranges.Length; i += 2)
        {
            if (codePoint >= ranges[i] && codePoint <= ranges[i + 1])
            {
                return true;
            }
        }

        return false;
    }
}
