namespace NetEvolve.Schemas.XPath.Tests.Unit.Functions.Strings;

using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using NetEvolve.Schemas.XPath.Xdm.Values;
using static NetEvolve.Schemas.XPath.Tests.Unit.Functions.Strings.StringsHarness;

/// <summary>
/// <c>fn:upper-case</c> and <c>fn:lower-case</c> (Functions and Operators 1.0, 7.4.8 and 7.4.9): the full case mappings of the pinned
/// Unicode version (the simple mappings of <c>UnicodeData.txt</c> and the unconditional entries of <c>SpecialCasing.txt</c>), applied
/// by embedded tables, so every test leg gives the same result and no culture is consulted.
/// </summary>
public class CaseTests
{
    // Each row: the input and its upper-case and lower-case mapping, as code points in hexadecimal.
    // The rows are literals of the pinned Unicode version 18.0.0, so a leg that took its answer from the runtime would differ.
    private static readonly (string Input, string Upper, string Lower)[] Pinned =
    [
        ("0061 0062 0063", "0041 0042 0043", "0061 0062 0063"),
        ("0041 0062 0043 0031 0021", "0041 0042 0043 0031 0021", "0061 0062 0063 0031 0021"),
        ("00DF", "0053 0053", "00DF"), // sharp s: the full mapping has two letters
        ("0149", "02BC 004E", "0149"), // n preceded by apostrophe
        ("01F0", "004A 030C", "01F0"),
        ("0390", "0399 0308 0301", "0390"),
        ("FB17", "0544 053D", "FB17"), // Armenian small ligature men xeh
        ("FB00", "0046 0046", "FB00"),
        ("0130", "0130", "0069 0307"), // capital i with dot above: the lower-case mapping has two code points
        ("0131", "0049", "0131"), // dotless i
        ("00B5", "039C", "00B5"), // micro sign
        ("00FF", "0178", "00FF"),
        ("017F", "0053", "017F"), // long s
        ("01C4 01C5 01C6", "01C4 01C4 01C4", "01C6 01C6 01C6"), // dz with caron: capital, title-case, small
        ("01CB", "01CA", "01CC"),
        ("0391 03A3", "0391 03A3", "03B1 03C3"), // no Final_Sigma: a capital sigma always maps to the medial small sigma
        ("03C2", "03A3", "03C2"), // final small sigma
        ("1E9E", "1E9E", "00DF"), // capital sharp s
        ("0587", "0535 0552", "0587"),
        ("10400", "10400", "10428"), // Deseret, outside the Basic Multilingual Plane
        ("10428", "10400", "10428"),
        ("1F600", "1F600", "1F600"), // emoji: no mapping
        ("0061 10428 0062", "0041 10400 0042", "0061 10428 0062"),
        ("2160", "2160", "2170"), // roman numeral one
        ("24B6", "24B6", "24D0"), // circled capital a
        ("FF21", "FF21", "FF41"), // fullwidth capital a
        ("4E2D", "4E2D", "4E2D"),
    ];

    [Test]
    public async Task UpperCase_PinnedSamples_GiveTheSameResultOnEveryLeg()
    {
        foreach (var (input, upper, _) in Pinned)
        {
            await Assert.That(Hex(Text("upper-case", Str(FromHex(input))))).IsEqualTo(upper).Because(input);
        }
    }

    [Test]
    public async Task LowerCase_PinnedSamples_GiveTheSameResultOnEveryLeg()
    {
        foreach (var (input, _, lower) in Pinned)
        {
            await Assert.That(Hex(Text("lower-case", Str(FromHex(input))))).IsEqualTo(lower).Because(input);
        }
    }

    [Test]
    [Arguments("tr-TR")]
    [Arguments("az-Latn-AZ")]
    [Arguments("lt-LT")]
    [Arguments("")]
    public async Task CaseMapping_IgnoresTheCurrentCulture(string name)
    {
        var saved = CultureInfo.CurrentCulture;
        var savedUi = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(name);
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(name);

            await Assert.That(Text("lower-case", Str("TITLE I"))).IsEqualTo("title i");
            await Assert.That(Text("upper-case", Str("title i"))).IsEqualTo("TITLE I");
            await Assert.That(Hex(Text("lower-case", Str("\u0130")))).IsEqualTo("0069 0307");
            await Assert.That(Hex(Text("upper-case", Str("\u0131")))).IsEqualTo("0049");
        }
        finally
        {
            CultureInfo.CurrentCulture = saved;
            CultureInfo.CurrentUICulture = savedUi;
        }
    }

    [Test]
    public async Task CaseMapping_RunsOnAThreadOfAnotherCulture_WithTheSameResult()
    {
        string? result = null;
        var thread = new Thread(() =>
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
            result = Text("lower-case", Str("I"));
        });
        thread.Start();
        thread.Join();

        await Assert.That(result).IsEqualTo("i");
    }

    [Test]
    public async Task UpperCase_SpecificationExample() =>
        await Assert.That(Text("upper-case", Str("abCd0"))).IsEqualTo("ABCD0");

    [Test]
    public async Task LowerCase_SpecificationExample() =>
        await Assert.That(Text("lower-case", Str("ABc!D"))).IsEqualTo("abc!d");

    [Test]
    [Arguments("upper-case")]
    [Arguments("lower-case")]
    public async Task EmptySequenceAndEmptyString_GiveTheEmptyString(string local)
    {
        await Assert.That(Shown(local, XdmSequence.Empty)).IsEqualTo("string:");
        await Assert.That(Shown(local, Str(""))).IsEqualTo("string:");
    }

    [Test]
    [Arguments("upper-case", "ABC 123 !")]
    [Arguments("lower-case", "abc 123 !")]
    public async Task AString_WithoutAChange_IsReturnedAsIs(string local, string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        var source = new string(text.ToCharArray());
        var result = Call(local, Str(source));

        await Assert.That(result[0].TryGetString(out var returned)).IsTrue();
        await Assert.That(ReferenceEquals(returned, source)).IsTrue();
    }

    [Test]
    [Arguments("upper-case")]
    [Arguments("lower-case")]
    public async Task AnArgumentOfTwoItems_IsXPTY0004(string local) =>
        await Assert.That(Text(local, Strs("a", "b"))).IsEqualTo("error:XPTY0004");

    [Test]
    [Arguments("upper-case")]
    [Arguments("lower-case")]
    public async Task NoArgumentAndTwoArguments_AreNotRegistered(string local)
    {
        await Assert.That(Text(local)).IsEqualTo("error:not-registered");
        await Assert.That(Text(local, Str("a"), Str("b"))).IsEqualTo("error:not-registered");
    }

    private static string FromHex(string codePoints)
    {
        var text = new System.Text.StringBuilder();
        foreach (var part in codePoints.Split(' '))
        {
            _ = text.Append(
                char.ConvertFromUtf32(int.Parse(part, NumberStyles.HexNumber, CultureInfo.InvariantCulture))
            );
        }

        return text.ToString();
    }
}
