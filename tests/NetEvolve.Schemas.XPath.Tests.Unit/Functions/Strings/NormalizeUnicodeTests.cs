namespace NetEvolve.Schemas.XPath.Tests.Unit.Functions.Strings;

using System.Threading.Tasks;
using NetEvolve.Schemas.XPath.Xdm.Values;
using static NetEvolve.Schemas.XPath.Tests.Unit.Functions.Strings.StringsHarness;

/// <summary>
/// <c>fn:normalize-unicode</c> (Functions and Operators 1.0, 7.4.5): NFC, NFD, NFKC, and NFKD by embedded tables, the form name
/// trimmed and case-insensitive, the zero-length form as no normalization, and FOCH0003 for every other form, the fully-normalized
/// form included (DEF-75, #87). The whole table of the pinned Unicode version is checked against <c>NormalizationTest.txt</c> in <c>UnicodeNormalizationTests</c>.
/// </summary>
public class NormalizeUnicodeTests
{
    [Test]
    [Arguments("A\u030A", "NFC", "\u00C5")]
    [Arguments("\u00C5", "NFD", "A\u030A")]
    [Arguments("\u212B", "NFC", "\u00C5")] // angstrom sign: a singleton decomposition
    [Arguments("\u212B", "NFD", "A\u030A")]
    [Arguments("\uFB01", "NFC", "\uFB01")] // the fi ligature has a compatibility mapping only
    [Arguments("\uFB01", "NFKC", "fi")]
    [Arguments("\uFB01", "NFKD", "fi")]
    [Arguments("\u1E9B\u0323", "NFC", "\u1E9B\u0323")]
    [Arguments("\u1E9B\u0323", "NFD", "\u017F\u0323\u0307")]
    [Arguments("\u1E9B\u0323", "NFKC", "\u1E69")]
    [Arguments("\u1E9B\u0323", "NFKD", "s\u0323\u0307")]
    [Arguments("\uAC00", "NFD", "\u1100\u1161")] // Hangul syllable GA
    [Arguments("\u1100\u1161", "NFC", "\uAC00")]
    [Arguments("\uAC01", "NFD", "\u1100\u1161\u11A8")]
    [Arguments("\u1100\u1161\u11A8", "NFC", "\uAC01")]
    [Arguments("q\u0307\u0323", "NFC", "q\u0323\u0307")] // canonical ordering by combining class
    [Arguments("q\u0323\u0307", "NFD", "q\u0323\u0307")]
    [Arguments("a\u0308\u0323", "NFC", "\u1EA1\u0308")]
    [Arguments("\u00A0", "NFKC", " ")] // no-break space has a compatibility mapping
    [Arguments("\u00A0", "NFC", "\u00A0")]
    [Arguments("\u2126", "NFC", "\u03A9")] // ohm sign
    [Arguments("\u0958", "NFC", "\u0915\u093C")] // composition exclusion: it does not recompose
    [Arguments("\U0001D15E", "NFC", "\U0001D157\U0001D165")] // musical symbol half note: outside the BMP, excluded from composition
    [Arguments("a\U0001F600\u0301", "NFC", "a\U0001F600\u0301")]
    [Arguments("e\u0301\U0001F600", "NFC", "\u00E9\U0001F600")]
    public async Task Forms_ApplyTheNormalizationOfTheUnicodeStandard(string input, string form, string expected) =>
        await Assert.That(Hex(Text("normalize-unicode", Str(input), Str(form)))).IsEqualTo(Hex(expected));

    [Test]
    public async Task OneArgument_IsNfc() =>
        await Assert.That(Text("normalize-unicode", Str("A\u030A"))).IsEqualTo("\u00C5");

    [Test]
    [Arguments(" NFC ")]
    [Arguments("nfc")]
    [Arguments("nFc")]
    [Arguments("\tNFC\n")]
    public async Task FormName_IsTrimmedAndCaseInsensitive(string form) =>
        await Assert.That(Text("normalize-unicode", Str("A\u030A"), Str(form))).IsEqualTo("\u00C5");

    [Test]
    public async Task EmptyFormName_MeansNoNormalization()
    {
        await Assert.That(Text("normalize-unicode", Str("A\u030A"), Str(""))).IsEqualTo("A\u030A");
        await Assert.That(Text("normalize-unicode", Str("A\u030A"), Str("  "))).IsEqualTo("A\u030A");
        await Assert.That(Text("normalize-unicode", Str("\u00C5"), Str("")).Length).IsEqualTo(1);
    }

    [Test]
    [Arguments("FULLY-NORMALIZED")]
    [Arguments("fully-normalized")]
    [Arguments("NFX")]
    [Arguments("example.com/notSupported/")]
    [Arguments("N F C")]
    [Arguments("NFC1")]
    public async Task UnsupportedForm_IsFOCH0003(string form) =>
        await Assert.That(Text("normalize-unicode", Str("abc"), Str(form))).IsEqualTo("error:FOCH0003");

    [Test]
    public async Task UnsupportedForm_IsRaisedForTheEmptyStringAndTheEmptySequenceToo()
    {
        await Assert.That(Text("normalize-unicode", Str(""), Str("FULLY-NORMALIZED"))).IsEqualTo("error:FOCH0003");
        await Assert
            .That(Text("normalize-unicode", XdmSequence.Empty, Str("FULLY-NORMALIZED")))
            .IsEqualTo("error:FOCH0003");
    }

    [Test]
    public async Task EmptySequence_IsTheEmptyString()
    {
        await Assert.That(Shown("normalize-unicode", XdmSequence.Empty)).IsEqualTo("string:");
        await Assert.That(Shown("normalize-unicode", XdmSequence.Empty, Str("NFC"))).IsEqualTo("string:");
    }

    [Test]
    [Arguments("NFC")]
    [Arguments("NFD")]
    [Arguments("NFKC")]
    [Arguments("NFKD")]
    public async Task AsciiText_IsReturnedAsIs(string form)
    {
        var source = new string(['f', 'o', 'o', ' ', 'b', 'a', 'r']);
        var result = Call("normalize-unicode", Str(source), Str(form));

        await Assert.That(result[0].TryGetString(out var text)).IsTrue();
        await Assert.That(ReferenceEquals(text, source)).IsTrue();
    }

    [Test]
    public async Task EmptyFormArgument_IsXPTY0004() =>
        await Assert.That(Text("normalize-unicode", Str("abc"), XdmSequence.Empty)).IsEqualTo("error:XPTY0004");

    [Test]
    public async Task ANumberAsTheText_IsXPTY0004() =>
        await Assert.That(Text("normalize-unicode", Int(12))).IsEqualTo("error:XPTY0004");

    [Test]
    public async Task ThreeArguments_AreNotRegistered() =>
        await Assert.That(Text("normalize-unicode", Str("a"), Str("NFC"), Str("x"))).IsEqualTo("error:not-registered");
}
