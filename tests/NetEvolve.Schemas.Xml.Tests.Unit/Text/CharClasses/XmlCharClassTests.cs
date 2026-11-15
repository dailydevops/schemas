namespace NetEvolve.Schemas.Xml.Tests.Unit.Text.CharClasses;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Xml.Text.CharClasses;

public class XmlCharClassTests
{
    // XML 1.0 (Fifth Edition) production 2: Char.
    private static readonly (int Lo, int Hi)[] CharRanges =
    [
        (0x9, 0x9),
        (0xA, 0xA),
        (0xD, 0xD),
        (0x20, 0xD7FF),
        (0xE000, 0xFFFD),
        (0x10000, 0x10FFFF),
    ];

    // Production 4: NameStartChar.
    private static readonly (int Lo, int Hi)[] NameStartRanges =
    [
        (':', ':'),
        ('A', 'Z'),
        ('_', '_'),
        ('a', 'z'),
        (0xC0, 0xD6),
        (0xD8, 0xF6),
        (0xF8, 0x2FF),
        (0x370, 0x37D),
        (0x37F, 0x1FFF),
        (0x200C, 0x200D),
        (0x2070, 0x218F),
        (0x2C00, 0x2FEF),
        (0x3001, 0xD7FF),
        (0xF900, 0xFDCF),
        (0xFDF0, 0xFFFD),
        (0x10000, 0xEFFFF),
    ];

    // Production 4a: NameChar = NameStartChar + extras.
    private static readonly (int Lo, int Hi)[] NameExtraRanges =
    [
        ('-', '-'),
        ('.', '.'),
        ('0', '9'),
        (0xB7, 0xB7),
        (0x300, 0x36F),
        (0x203F, 0x2040),
    ];

    private static bool In((int Lo, int Hi)[] ranges, int cp)
    {
        foreach (var (lo, hi) in ranges)
        {
            if (cp >= lo && cp <= hi)
            {
                return true;
            }
        }

        return false;
    }

    private static bool OracleChar(int cp) => In(CharRanges, cp);

    private static bool OracleStart(int cp) => In(NameStartRanges, cp);

    private static bool OracleName(int cp) => In(NameStartRanges, cp) || In(NameExtraRanges, cp);

    private static IEnumerable<int> Probes(params (int Lo, int Hi)[][] tables)
    {
        foreach (var table in tables)
        {
            foreach (var (lo, hi) in table)
            {
                yield return lo - 1;
                yield return lo;
                yield return hi;
                yield return hi + 1;
            }
        }
    }

    private static string Utf16(int cp) => char.ConvertFromUtf32(cp);

    private static async Task AssertNoMismatch(List<string> failures) =>
        await Assert.That(string.Join(", ", failures)).IsEqualTo(string.Empty);

    [Test]
    public async Task IsChar_CodePoint_MatchesProduction2AtEveryRangeEdge()
    {
        var failures = new List<string>();
        foreach (var cp in Probes(CharRanges))
        {
            if (XmlCharClass.IsChar(cp) != OracleChar(cp))
            {
                failures.Add($"U+{cp:X}");
            }
        }

        await AssertNoMismatch(failures);
    }

    [Test]
    public async Task IsNameStartChar_CodePoint_MatchesProduction4AtEveryRangeEdge()
    {
        var failures = new List<string>();
        foreach (var cp in Probes(NameStartRanges))
        {
            if (XmlCharClass.IsNameStartChar(cp) != OracleStart(cp))
            {
                failures.Add($"U+{cp:X}");
            }
        }

        await AssertNoMismatch(failures);
    }

    [Test]
    public async Task IsNameChar_CodePoint_MatchesProduction4aAtEveryRangeEdge()
    {
        var failures = new List<string>();
        foreach (var cp in Probes(NameStartRanges, NameExtraRanges))
        {
            if (XmlCharClass.IsNameChar(cp) != OracleName(cp))
            {
                failures.Add($"U+{cp:X}");
            }
        }

        await AssertNoMismatch(failures);
    }

    [Test]
    public async Task CodePointOverloads_OutOfRangeValues_AreRejected()
    {
        await Assert.That(XmlCharClass.IsChar(-1)).IsFalse();
        await Assert.That(XmlCharClass.IsChar(0x110000)).IsFalse();
        await Assert.That(XmlCharClass.IsNameStartChar(-1)).IsFalse();
        await Assert.That(XmlCharClass.IsNameStartChar(0x110000)).IsFalse();
        await Assert.That(XmlCharClass.IsNameChar(-1)).IsFalse();
        await Assert.That(XmlCharClass.IsNameChar(0x110000)).IsFalse();
    }

    [Test]
    public async Task CodeUnitOverloads_AgreeWithOracle_ForEveryBmpUnit()
    {
        var failures = new List<string>();
        for (var cp = 0; cp <= 0xFFFF; cp++)
        {
            var c = (char)cp;
            var surrogate = cp is >= 0xD800 and <= 0xDFFF;
            if (
                XmlCharClass.IsChar(c) != (!surrogate && OracleChar(cp))
                || XmlCharClass.IsNameStartChar(c) != (!surrogate && OracleStart(cp))
                || XmlCharClass.IsNameChar(c) != (!surrogate && OracleName(cp))
            )
            {
                failures.Add($"U+{cp:X}");
            }
        }

        await AssertNoMismatch(failures);
    }

    [Test]
    [Arguments(0x10000)]
    [Arguments(0x10FFF)]
    [Arguments(0x20000)]
    [Arguments(0xEFFFF)]
    public async Task ValidSurrogatePair_InNameRange_IsNameStartAndNameChar(int cp)
    {
        var text = Utf16(cp);

        await Assert.That(XmlCharClass.NameLength(text.AsSpan())).IsEqualTo(2);
        await Assert.That(XmlCharClass.IndexOfNonChar(text.AsSpan())).IsEqualTo(-1);
        await Assert.That(XmlCharClass.IsNameStartChar(cp)).IsTrue();
        await Assert.That(XmlCharClass.IsNameChar(cp)).IsTrue();
    }

    [Test]
    [Arguments(0xF0000)]
    [Arguments(0x10FFFF)]
    public async Task ValidSurrogatePair_AboveEFFFF_IsCharButNotName(int cp)
    {
        var text = Utf16(cp);

        await Assert.That(XmlCharClass.NameLength(text.AsSpan())).IsEqualTo(0);
        await Assert.That(XmlCharClass.IndexOfNonChar(text.AsSpan())).IsEqualTo(-1);
    }

    [Test]
    [Arguments('\uD800')]
    [Arguments('\uDBFF')]
    [Arguments('\uDC00')]
    [Arguments('\uDFFF')]
    public async Task LoneSurrogate_IsNotChar(char surrogate)
    {
        await Assert.That(XmlCharClass.IsChar(surrogate)).IsFalse();
        await Assert.That(XmlCharClass.IsNameStartChar(surrogate)).IsFalse();
        await Assert.That(XmlCharClass.IsNameChar(surrogate)).IsFalse();
        await Assert.That(XmlCharClass.IndexOfNonChar(new[] { 'a', surrogate }.AsSpan())).IsEqualTo(1);
        await Assert.That(XmlCharClass.IndexOfNonChar(new[] { surrogate, 'a' }.AsSpan())).IsEqualTo(0);
        await Assert.That(XmlCharClass.NameLength(new[] { 'a', surrogate }.AsSpan())).IsEqualTo(1);
    }

    [Test]
    public async Task ReversedSurrogatePair_IsNotChar()
    {
        var text = new[] { '\uDC00', '\uD800' };

        await Assert.That(XmlCharClass.IndexOfNonChar(text.AsSpan())).IsEqualTo(0);
    }

    [Test]
    [Arguments(0xFFFE)]
    [Arguments(0xFFFF)]
    [Arguments(0x0)]
    [Arguments(0x8)]
    [Arguments(0xB)]
    [Arguments(0xC)]
    [Arguments(0xE)]
    [Arguments(0x1F)]
    public async Task ForbiddenCharacters_AreRejected(int cp)
    {
        var text = new[] { 'a', (char)cp, 'b' };

        await Assert.That(XmlCharClass.IsChar(cp)).IsFalse();
        await Assert.That(XmlCharClass.IsChar((char)cp)).IsFalse();
        await Assert.That(XmlCharClass.IndexOfNonChar(text.AsSpan())).IsEqualTo(1);
    }

    [Test]
    public async Task NameLength_ReturnsLeadingNameOnly()
    {
        await Assert.That(XmlCharClass.NameLength("a:b-c.d1 rest".AsSpan())).IsEqualTo(8);
        await Assert.That(XmlCharClass.NameLength("1abc".AsSpan())).IsEqualTo(0);
        await Assert.That(XmlCharClass.NameLength("-abc".AsSpan())).IsEqualTo(0);
        await Assert.That(XmlCharClass.NameLength(ReadOnlySpan<char>.Empty)).IsEqualTo(0);
        await Assert.That(XmlCharClass.NameLength("a\u00B7\u0300".AsSpan())).IsEqualTo(3);
        await Assert.That(XmlCharClass.NameLength("a\uD800\uDC00b c".AsSpan())).IsEqualTo(4);
        await Assert.That(XmlCharClass.NameLength("a\uDB7F\uDFFFb".AsSpan())).IsEqualTo(4);
        await Assert.That(XmlCharClass.NameLength("a\uDB80\uDC00b".AsSpan())).IsEqualTo(1);
        await Assert.That(XmlCharClass.NameLength("ab\uD800".AsSpan())).IsEqualTo(2);
    }

    [Test]
    public async Task IndexOfNonChar_AllValid_ReturnsMinusOne()
    {
        await Assert.That(XmlCharClass.IndexOfNonChar("a\t\n\r \uD7FF\uE000\uFFFD".AsSpan())).IsEqualTo(-1);
        await Assert.That(XmlCharClass.IndexOfNonChar(ReadOnlySpan<char>.Empty)).IsEqualTo(-1);
    }

    [Test]
    public async Task Classifying_FourKilobyteSpan_AllocatesNothing()
    {
        var buffer = new char[2048];
        buffer.AsSpan().Fill('x');
        var span = (ReadOnlySpan<char>)buffer;
        _ = XmlCharClass.IndexOfNonChar(span);
        _ = XmlCharClass.NameLength(span);

        var before = GC.GetAllocatedBytesForCurrentThread();
        var index = XmlCharClass.IndexOfNonChar(span);
        var length = XmlCharClass.NameLength(span);
        var after = GC.GetAllocatedBytesForCurrentThread();

        await Assert.That(index).IsEqualTo(-1);
        await Assert.That(length).IsEqualTo(2048);
        await Assert.That(after - before).IsEqualTo(0L);
    }
}
