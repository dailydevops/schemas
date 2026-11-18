namespace NetEvolve.Schemas.XPath.Tests.Unit.Functions.Strings;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NetEvolve.Schemas.XPath.Functions.Strings;
using NetEvolve.Schemas.XPath.Limits;

/// <summary>
/// <see cref="UnicodeNormalization"/> against the official conformance data of the pinned Unicode version:
/// <c>NormalizationTest.txt</c> (every row for NFC, NFD, NFKC, and NFKD, and the stability of every code point it does not list) and
/// the combining classes of <c>UnicodeData.txt</c>.
/// </summary>
public class UnicodeNormalizationTests
{
    private const int MaxReported = 10;

    [Test]
    public async Task NormalizationTest_EveryRow_HoldsForEveryForm(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var ucd = await StringsUcd.GetAsync(cancellationToken);
        var (rows, _) = Read(await ucd.ReadLinesAsync("NormalizationTest.txt", cancellationToken));
        var failures = new List<string>();

        foreach (var (c1, c2, c3, c4, c5, source) in rows)
        {
            if (failures.Count >= MaxReported)
            {
                break;
            }

            // NFC: c2 is NFC of c1, c2, and c3; c4 is NFC of c4 and c5.
            Expect(failures, source, "NFC", UnicodeNormalizationForm.Nfc, c2, c1, c2, c3);
            Expect(failures, source, "NFC", UnicodeNormalizationForm.Nfc, c4, c4, c5);

            // NFD: c3 is NFD of c1, c2, and c3; c5 is NFD of c4 and c5.
            Expect(failures, source, "NFD", UnicodeNormalizationForm.Nfd, c3, c1, c2, c3);
            Expect(failures, source, "NFD", UnicodeNormalizationForm.Nfd, c5, c4, c5);

            // NFKC: c4 is NFKC of every column; NFKD: c5 is NFKD of every column.
            Expect(failures, source, "NFKC", UnicodeNormalizationForm.Nfkc, c4, c1, c2, c3, c4, c5);
            Expect(failures, source, "NFKD", UnicodeNormalizationForm.Nfkd, c5, c1, c2, c3, c4, c5);
        }

        await Assert.That(string.Join("; ", failures)).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task NormalizationTest_CodePointsOfNoRow_AreStableUnderEveryForm(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var ucd = await StringsUcd.GetAsync(cancellationToken);
        var (_, single) = Read(await ucd.ReadLinesAsync("NormalizationTest.txt", cancellationToken));
        var failures = new List<string>();

        for (var cp = 0; cp < 0x110000 && failures.Count < MaxReported; cp++)
        {
            if (single.Contains(cp) || (cp >= 0xD800 && cp <= 0xDFFF))
            {
                continue;
            }

            var text = char.ConvertFromUtf32(cp);
            foreach (var form in Forms)
            {
                if (
                    !string.Equals(
                        UnicodeNormalization.Normalize(text, form, XPathLimits.Default),
                        text,
                        StringComparison.Ordinal
                    )
                )
                {
                    failures.Add($"{form} U+{cp:X4}");
                }
            }
        }

        await Assert.That(string.Join(", ", failures)).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task CombiningClass_EveryCodePoint_MatchesUnicodeData(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var ucd = await StringsUcd.GetAsync(cancellationToken);
        var expected = new byte[0x110000];
        foreach (var line in await ucd.ReadLinesAsync("UnicodeData.txt", cancellationToken))
        {
            var fields = line.Split(';');
            expected[int.Parse(fields[0], NumberStyles.HexNumber, CultureInfo.InvariantCulture)] = byte.Parse(
                fields[3],
                CultureInfo.InvariantCulture
            );
        }

        var failures = new List<string>();
        for (var cp = 0; cp < 0x110000 && failures.Count < MaxReported; cp++)
        {
            if (UnicodeNormalization.CombiningClass(cp) != expected[cp])
            {
                failures.Add($"U+{cp:X4}");
            }
        }

        await Assert.That(string.Join(", ", failures)).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task Normalize_HangulSyllables_RoundTripThroughTheAlgorithm()
    {
        var syllables = new StringBuilder();
        for (var cp = 0xAC00; cp <= 0xD7A3; cp += 97)
        {
            _ = syllables.Append((char)cp);
        }

        var text = syllables.ToString();
        var decomposed = UnicodeNormalization.Normalize(text, UnicodeNormalizationForm.Nfd, XPathLimits.Default);

        await Assert.That(decomposed.Length).IsGreaterThan(text.Length);
        await Assert
            .That(UnicodeNormalization.Normalize(decomposed, UnicodeNormalizationForm.Nfc, XPathLimits.Default))
            .IsEqualTo(text);
    }

    [Test]
    public async Task Normalize_ALongCombiningSequence_IsOrderedByCombiningClass()
    {
        // 300 marks of class 230 and 220 alternately: the stable ordering puts every 220 first, and the order within a class is kept.
        var text = new StringBuilder("a");
        for (var i = 0; i < 300; i++)
        {
            _ = text.Append(i % 2 == 0 ? '\u0301' : '\u0323');
        }

        var normalized = UnicodeNormalization.Normalize(
            text.ToString(),
            UnicodeNormalizationForm.Nfd,
            XPathLimits.Default
        );

        await Assert.That(normalized.Length).IsEqualTo(301);
        await Assert.That(normalized.Substring(1, 150)).IsEqualTo(new string('\u0323', 150));
        await Assert.That(normalized.Substring(151)).IsEqualTo(new string('\u0301', 150));
    }

    [Test]
    public async Task Normalize_ThatChangesNothing_ReturnsTheSameInstance()
    {
        var source = new string(['\u00E9', 'a', 'b']);

        await Assert
            .That(
                ReferenceEquals(
                    UnicodeNormalization.Normalize(source, UnicodeNormalizationForm.Nfc, XPathLimits.Default),
                    source
                )
            )
            .IsTrue();
    }

    [Test]
    [Arguments("NFC", UnicodeNormalizationForm.Nfc)]
    [Arguments("NFD", UnicodeNormalizationForm.Nfd)]
    [Arguments("NFKC", UnicodeNormalizationForm.Nfkc)]
    [Arguments("NFKD", UnicodeNormalizationForm.Nfkd)]
    [Arguments("nfkd", UnicodeNormalizationForm.Nfkd)]
    [Arguments(" NfKc\t", UnicodeNormalizationForm.Nfkc)]
    public async Task TryParseForm_AcceptsTheFourFormsTrimmedAndCaseInsensitive(
        string name,
        UnicodeNormalizationForm expected
    )
    {
        var found = UnicodeNormalization.TryParseForm(name.AsSpan(), out var form);

        await Assert.That(found).IsTrue();
        await Assert.That(form).IsEqualTo(expected);
    }

    [Test]
    [Arguments("")]
    [Arguments("  ")]
    [Arguments("FULLY-NORMALIZED")]
    [Arguments("NF")]
    [Arguments("NFCC")]
    [Arguments("N FC")]
    [Arguments("NF\u212A")] // a Kelvin sign is not an ASCII K
    public async Task TryParseForm_RejectsEverythingElse(string name) =>
        await Assert.That(UnicodeNormalization.TryParseForm(name.AsSpan(), out _)).IsFalse();

    [Test]
    public async Task Normalize_NullString_IsRejected()
    {
        var raised = false;
        try
        {
            _ = UnicodeNormalization.Normalize(null!, UnicodeNormalizationForm.Nfc, XPathLimits.Default);
        }
        catch (ArgumentNullException)
        {
            raised = true;
        }

        await Assert.That(raised).IsTrue();
    }

    [Test]
    public async Task Normalize_AFormOutsideTheEnumeration_IsRejected()
    {
        var raised = false;
        try
        {
            _ = UnicodeNormalization.Normalize("\u00E9", (UnicodeNormalizationForm)9, XPathLimits.Default);
        }
        catch (ArgumentOutOfRangeException)
        {
            raised = true;
        }

        await Assert.That(raised).IsTrue();
    }

    private static readonly UnicodeNormalizationForm[] Forms =
    [
        UnicodeNormalizationForm.Nfc,
        UnicodeNormalizationForm.Nfd,
        UnicodeNormalizationForm.Nfkc,
        UnicodeNormalizationForm.Nfkd,
    ];

    private static void Expect(
        List<string> failures,
        string source,
        string name,
        UnicodeNormalizationForm form,
        string expected,
        params string[] inputs
    )
    {
        foreach (var input in inputs)
        {
            var actual = UnicodeNormalization.Normalize(input, form, XPathLimits.Default);
            if (!string.Equals(actual, expected, StringComparison.Ordinal))
            {
                failures.Add($"{name} of {Show(input)} gave {Show(actual)}, expected {Show(expected)} ({source})");
            }
        }
    }

    private static string Show(string text)
    {
        var parts = new List<string>();
        for (var i = 0; i < text.Length; i++)
        {
            var cp =
                char.IsHighSurrogate(text[i]) && i + 1 < text.Length
                    ? char.ConvertToUtf32(text[i], text[++i])
                    : text[i];
            parts.Add(cp.ToString("X4", CultureInfo.InvariantCulture));
        }

        return string.Join(" ", parts);
    }

    // The rows of the five columns, and the code points of Part 1 (single code point rows), which the stability test leaves out.
    private static (
        List<(string C1, string C2, string C3, string C4, string C5, string Source)> Rows,
        HashSet<int> Single
    ) Read(string[] lines)
    {
        var rows = new List<(string, string, string, string, string, string)>();
        var single = new HashSet<int>();
        var part1 = false;
        foreach (var line in lines)
        {
            if (line.StartsWith('@'))
            {
                part1 = line.StartsWith("@Part1", StringComparison.Ordinal);
                continue;
            }

            if (line.Length == 0 || line[0] == '#')
            {
                continue;
            }

            var fields = line.Split(';');
            var columns = new string[5];
            for (var i = 0; i < 5; i++)
            {
                columns[i] = Decode(fields[i]);
            }

            if (part1)
            {
                _ = single.Add(char.ConvertToUtf32(columns[0], 0));
            }

            rows.Add((columns[0], columns[1], columns[2], columns[3], columns[4], fields[0].Trim()));
        }

        return (rows, single);
    }

    private static string Decode(string field)
    {
        var text = new StringBuilder();
        foreach (var part in field.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            _ = text.Append(
                char.ConvertFromUtf32(int.Parse(part, NumberStyles.HexNumber, CultureInfo.InvariantCulture))
            );
        }

        return text.ToString();
    }
}
