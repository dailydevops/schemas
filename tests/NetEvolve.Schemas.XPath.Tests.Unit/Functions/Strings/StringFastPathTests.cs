namespace NetEvolve.Schemas.XPath.Tests.Unit.Functions.Strings;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Functions.Strings;
using NetEvolve.Schemas.XPath.Limits;
using NetEvolve.Schemas.XPath.Tests.Unit.Functions.Sequences;
using NetEvolve.Schemas.XPath.Xdm.Values;
using static NetEvolve.Schemas.XPath.Tests.Unit.Functions.Strings.StringsHarness;

/// <summary>
/// Characterization of <c>fn:translate</c>, <c>fn:upper-case</c>, and <c>fn:lower-case</c> against naive code point references: the
/// ASCII and BMP shortcuts must give exactly the result of the general code point path, for plain, mixed, astral, and unpaired
/// surrogate input, for every kind of map string, and for strings that need no change (the same instance).
/// </summary>
public class StringFastPathTests
{
    private const string Astral = "\U0001F600";
    private const string Deseret = "\U00010400\U00010428";

    private static readonly string AllAscii = string.Concat(Enumerable.Range(0, 0x80).Select(c => (char)c));
    private static readonly string LongMixed = string.Concat(
        Enumerable.Repeat("Hello World 123 ßİ café " + Astral + Deseret + " end ", 40)
    );

    private static readonly string[] Inputs =
    [
        "",
        "a",
        "Z",
        "abc",
        "ABC",
        "Abc-123_xyZ",
        "INV-2026-0001",
        "already lower",
        "ALREADY UPPER",
        "digits 0123456789 and punctuation !\"#$%&'()*+,-./:;<=>?@[\\]^_`{|}~",
        AllAscii,
        "café ß İ ı Σ ς ǅ",
        "a" + Astral + "B" + Deseret + "c",
        "x\uD800y",
        "x\uDC00Y",
        "\uD83D",
        "\uDE00a",
        "A\uD83D",
        LongMixed,
        new string('a', 700),
        new string('Q', 700),
    ];

    private static readonly (string Map, string Translation)[] Maps =
    [
        ("ABCDEFGHIJKLMNOPQRSTUVWXYZ", "abcdefghijklmnopqrstuvwxyz"),
        ("abcdefghijklmnopqrstuvwxyz", "ABCDEFGHIJKLMNOPQRSTUVWXYZ"),
        ("abc", "AB"),
        ("abc", ""),
        ("a", "xyz"),
        ("aab", "xyz"),
        ("abca", "wxyz"),
        ("- ", "_"),
        ("éß", "eS"),
        (Astral, "x"),
        ("a", Astral),
        ("ab", Astral + Deseret),
        ("a" + Astral, "bc"),
        ("a\uD800", "bc"),
        ("a", "b\uDC00"),
        ("\uD83D", "x"),
        ("\uDE00", "x"),
        ("A", "\uD83D"),
        ("z", "z"),
        ("\0\u007F", "xy"),
    ];

    public static IEnumerable<Func<string>> InputRows() => Inputs.Select(i => (Func<string>)(() => i));

    [Test]
    [MethodDataSource(nameof(InputRows))]
    public async Task UpperCase_MatchesTheCodePointReference(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        await Assert.That(CaseMapping.ToUpper(text, XPathLimits.Default)).IsEqualTo(Reference(text, upper: true));
        await Assert.That(Text("upper-case", Str(text))).IsEqualTo(Reference(text, upper: true));
    }

    [Test]
    [MethodDataSource(nameof(InputRows))]
    public async Task LowerCase_MatchesTheCodePointReference(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        await Assert.That(CaseMapping.ToLower(text, XPathLimits.Default)).IsEqualTo(Reference(text, upper: false));
        await Assert.That(Text("lower-case", Str(text))).IsEqualTo(Reference(text, upper: false));
    }

    [Test]
    public async Task Translate_EveryInputAndMap_MatchesTheCodePointReference()
    {
        var failures = new List<string>();
        foreach (var text in Inputs)
        {
            foreach (var (map, translation) in Maps)
            {
                var actual = Text("translate", Str(text), Str(map), Str(translation));
                if (!string.Equals(actual, ReferenceTranslate(text, map, translation), StringComparison.Ordinal))
                {
                    failures.Add($"{Hex(text)} | {Hex(map)} | {Hex(translation)}");
                }
            }
        }

        await Assert.That(string.Join("; ", failures)).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task Translate_AllAsciiUnits_FoldTheCaseOfTheLettersOnly() =>
        await Assert
            .That(Text("translate", Str(AllAscii), Str(Maps[0].Map), Str(Maps[0].Translation)))
            .IsEqualTo(string.Concat(AllAscii.Select(c => c is >= 'A' and <= 'Z' ? (char)(c + 32) : c)));

    [Test]
    public async Task Translate_ASuffixAfterTheLastChange_IsKept() =>
        await Assert.That(Text("translate", Str("xxAyyBzz"), Str("AB"), Str("ab"))).IsEqualTo("xxayybzz");

    [Test]
    public async Task Translate_ADeletionAtTheEnd_DropsTheCharacter() =>
        await Assert.That(Text("translate", Str("abc-"), Str("-"), Str(""))).IsEqualTo("abc");

    [Test]
    [Arguments("upper-case", "ABC 123")]
    [Arguments("lower-case", "abc 123")]
    [Arguments("lower-case", "abc 123 é")]
    [Arguments("lower-case", "abc ß " + Astral)]
    [Arguments("upper-case", "ABC 123 É")]
    public async Task CaseMapping_ThatChangesNothing_ReturnsTheSameInstance(string local, string text)
    {
        var same = new string(text.AsSpan());
        var result = Call(local, Str(same));

        await Assert.That(result[0].TryGetString(out var value)).IsTrue();
        await Assert.That(ReferenceEquals(value, same)).IsTrue();
    }

    [Test]
    [Arguments("xyz", "XYZ")]
    [Arguments("é", "e")]
    [Arguments(Astral, "x")]
    [Arguments("\uD800", "x")]
    public async Task Translate_AMapThatTouchesNothing_ReturnsTheSameInstance(string map, string translation)
    {
        var same = new string("plain ascii tent".AsSpan());
        var result = Call("translate", Str(same), Str(map), Str(translation));

        await Assert.That(result[0].TryGetString(out var value)).IsTrue();
        await Assert.That(ReferenceEquals(value, same)).IsTrue();
    }

    [Test]
    [Arguments("abcdefg", "a", "b")]
    [Arguments("abcdefg", "x", "y")]
    [Arguments("ABCDEFG", "a", "b")]
    [Arguments("aaa", "a", Astral)]
    public async Task Translate_AResultAboveTheLimit_RaisesTheStringLengthError(
        string text,
        string map,
        string translation
    )
    {
        using var context = new XPathEvaluationContext { Limits = XPathLimits.Default.WithMaxStringLength(5) };
        var function = SequencesHarness.Resolve("translate", 3);

        var raised = Raised(() => function.Call(context, [Str(text), Str(map), Str(translation)]));

        await Assert.That(raised).IsEqualTo(XPathLimitErrors.StringLengthExceeded.ToString());
    }

    [Test]
    [Arguments("upper-case", "abcdefg")]
    [Arguments("lower-case", "ABCDEFG")]
    [Arguments("upper-case", "abcßß")]
    public async Task CaseMapping_AResultAboveTheLimit_RaisesTheStringLengthError(string local, string text)
    {
        using var context = new XPathEvaluationContext { Limits = XPathLimits.Default.WithMaxStringLength(5) };
        var function = SequencesHarness.Resolve(local, 1);

        var raised = Raised(() => function.Call(context, [Str(text)]));

        await Assert.That(raised).IsEqualTo(XPathLimitErrors.StringLengthExceeded.ToString());
    }

    private static string Raised(Func<XdmSequence> call)
    {
        try
        {
            _ = call();
            return string.Empty;
        }
        catch (XPathException failure)
        {
            return failure.Diagnostic.Code.ToString();
        }
    }

    // The general path, read naively: one code point at a time through the case table.
    private static string Reference(string text, bool upper)
    {
        var result = new StringBuilder();
        var mapped = new int[CaseMapping.MaxExpansion];
        var index = 0;
        while (index < text.Length)
        {
            var count = CaseMapping.Map(CodePoints.Next(text.AsSpan(), ref index), upper, mapped);
            for (var i = 0; i < count; i++)
            {
                AppendCodePoint(result, mapped[i]);
            }
        }

        return result.ToString();
    }

    private static string ReferenceTranslate(string text, string map, string translation)
    {
        var mapPoints = CodePointList(map);
        var translationPoints = CodePointList(translation);
        var result = new StringBuilder();
        foreach (var point in CodePointList(text))
        {
            var at = mapPoints.IndexOf(point);
            if (at < 0)
            {
                AppendCodePoint(result, point);
            }
            else if (at < translationPoints.Count)
            {
                AppendCodePoint(result, translationPoints[at]);
            }
        }

        return result.ToString();
    }

    private static List<int> CodePointList(string text)
    {
        var points = new List<int>();
        var index = 0;
        while (index < text.Length)
        {
            points.Add(CodePoints.Next(text.AsSpan(), ref index));
        }

        return points;
    }

    // An unpaired surrogate keeps the value of its code unit, as the functions do.
    private static void AppendCodePoint(StringBuilder builder, int codePoint) =>
        _ = codePoint < 0x10000 ? builder.Append((char)codePoint) : builder.Append(char.ConvertFromUtf32(codePoint));
}
