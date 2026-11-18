namespace NetEvolve.Schemas.XPath.Tests.Unit.Functions.Strings;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using NetEvolve.Schemas.XPath.Functions.Strings;
using NetEvolve.Schemas.XPath.Limits;

/// <summary>
/// <see cref="CaseMapping"/> against an independent reading of the pinned files: every code point from U+0000 to U+10FFFF, upper and
/// lower, is compared with the mapping that a naive scan of <c>UnicodeData.txt</c> and the unconditional rows of
/// <c>SpecialCasing.txt</c> gives. The generator is not reused, so the check is not circular.
/// </summary>
public class CaseMappingTests
{
    private const int MaxReported = 10;

    [Test]
    public async Task Map_EveryCodePoint_MatchesThePinnedFiles(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var ucd = await StringsUcd.GetAsync(cancellationToken);
        var (upper, lower) = Oracle(
            await ucd.ReadLinesAsync("UnicodeData.txt", cancellationToken),
            await ucd.ReadLinesAsync("SpecialCasing.txt", cancellationToken)
        );

        var failures = new List<string>();
        var buffer = new int[3];
        for (var cp = 0; cp < 0x110000 && failures.Count < MaxReported; cp++)
        {
            Check(cp, true, upper, buffer, failures);
            Check(cp, false, lower, buffer, failures);
        }

        await Assert.That(string.Join(", ", failures)).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task Map_ReportsTheLengthOfAnExpansion()
    {
        var buffer = new int[3];

        await Assert.That(CaseMapping.Map(0x61, upper: true, buffer)).IsEqualTo(1);
        await Assert.That(buffer[0]).IsEqualTo(0x41);
        await Assert.That(CaseMapping.Map(0xDF, upper: true, buffer)).IsEqualTo(2);
        await Assert.That(buffer[0]).IsEqualTo(0x53);
        await Assert.That(buffer[1]).IsEqualTo(0x53);
        await Assert.That(CaseMapping.Map(0x390, upper: true, buffer)).IsEqualTo(3);
        await Assert.That(CaseMapping.Map(0x130, upper: false, buffer)).IsEqualTo(2);
        await Assert.That(buffer[0]).IsEqualTo(0x69);
        await Assert.That(buffer[1]).IsEqualTo(0x307);
        await Assert.That(CaseMapping.Map(0x4E2D, upper: true, buffer)).IsEqualTo(1);
        await Assert.That(buffer[0]).IsEqualTo(0x4E2D);
    }

    [Test]
    [Arguments(-1)]
    [Arguments(0x110000)]
    [Arguments(int.MaxValue)]
    public async Task Map_ACodePointOutsideTheRange_MapsToItself(int codePoint)
    {
        var buffer = new int[3];

        await Assert.That(CaseMapping.Map(codePoint, upper: true, buffer)).IsEqualTo(1);
        await Assert.That(buffer[0]).IsEqualTo(codePoint);
    }

    [Test]
    public async Task Map_ADestinationOfTwoItems_IsRejected()
    {
        var raised = false;
        try
        {
            _ = CaseMapping.Map(0x61, upper: true, new int[2]);
        }
        catch (ArgumentOutOfRangeException)
        {
            raised = true;
        }

        await Assert.That(raised).IsTrue();
    }

    [Test]
    public async Task ToUpper_ThatChangesNothing_ReturnsTheSameInstance()
    {
        var source = new string(['A', 'B', '1', ' ', '\u00D7']);

        await Assert.That(ReferenceEquals(CaseMapping.ToUpper(source, XPathLimits.Default), source)).IsTrue();
        await Assert.That(ReferenceEquals(CaseMapping.ToLower("abc ", XPathLimits.Default), "abc ")).IsTrue();
    }

    [Test]
    public async Task ToUpper_AStringWithAnUnpairedSurrogate_KeepsIt()
    {
        var result = CaseMapping.ToUpper("a\uD800b", XPathLimits.Default);

        await Assert.That(result).IsEqualTo("A\uD800B");
    }

    [Test]
    public async Task ToLower_ABigString_IsMappedAcrossTheScratchBuffer()
    {
        var source = new string('A', 5000) + "\u0130" + new string('B', 5000);
        var expected = new string('a', 5000) + "i\u0307" + new string('b', 5000);

        await Assert.That(CaseMapping.ToLower(source, XPathLimits.Default)).IsEqualTo(expected);
    }

    [Test]
    public async Task ToUpper_NullString_IsRejected()
    {
        var raised = false;
        try
        {
            _ = CaseMapping.ToUpper(null!, XPathLimits.Default);
        }
        catch (ArgumentNullException)
        {
            raised = true;
        }

        await Assert.That(raised).IsTrue();
    }

    private static void Check(
        int codePoint,
        bool upper,
        Dictionary<int, int[]> expected,
        int[] buffer,
        List<string> failures
    )
    {
        var count = CaseMapping.Map(codePoint, upper, buffer);
        var want = expected.TryGetValue(codePoint, out var mapped) ? mapped : [codePoint];
        var same = count == want.Length;
        for (var i = 0; same && i < count; i++)
        {
            same = buffer[i] == want[i];
        }

        if (!same)
        {
            failures.Add($"{(upper ? "upper" : "lower")} U+{codePoint:X4}");
        }
    }

    // A naive reading of the two files: the simple mappings first, then the unconditional rows of SpecialCasing.txt over them.
    private static (Dictionary<int, int[]> Upper, Dictionary<int, int[]> Lower) Oracle(
        string[] unicodeData,
        string[] specialCasing
    )
    {
        var upper = new Dictionary<int, int[]>();
        var lower = new Dictionary<int, int[]>();
        foreach (var line in unicodeData)
        {
            var fields = line.Split(';');
            var codePoint = int.Parse(fields[0], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            if (fields[12].Length > 0)
            {
                upper[codePoint] = [int.Parse(fields[12], NumberStyles.HexNumber, CultureInfo.InvariantCulture)];
            }

            if (fields[13].Length > 0)
            {
                lower[codePoint] = [int.Parse(fields[13], NumberStyles.HexNumber, CultureInfo.InvariantCulture)];
            }
        }

        foreach (var line in specialCasing)
        {
            if (line.Length == 0 || line[0] == '#')
            {
                continue;
            }

            var fields = line.Split('#')[0].Split(';');
            if (fields.Length > 4 && fields[4].Trim().Length > 0)
            {
                continue;
            }

            var codePoint = int.Parse(fields[0].Trim(), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            lower[codePoint] = Parts(fields[1]);
            upper[codePoint] = Parts(fields[3]);
        }

        return (upper, lower);
    }

    private static int[] Parts(string text)
    {
        var parts = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var result = new int[parts.Length];
        for (var i = 0; i < result.Length; i++)
        {
            result[i] = int.Parse(parts[i], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        }

        return result;
    }
}
