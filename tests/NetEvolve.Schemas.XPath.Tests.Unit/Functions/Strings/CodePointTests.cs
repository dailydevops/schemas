namespace NetEvolve.Schemas.XPath.Tests.Unit.Functions.Strings;

using System.Threading.Tasks;
using NetEvolve.Schemas.XPath.Xdm.Values;
using static NetEvolve.Schemas.XPath.Tests.Unit.Functions.Strings.StringsHarness;

/// <summary>
/// <c>fn:codepoints-to-string</c> and <c>fn:string-to-codepoints</c> (Functions and Operators 1.0, 7.2.3 and 7.2.4), and the
/// code point counting of the string functions for characters outside the Basic Multilingual Plane.
/// </summary>
public class CodePointTests
{
    [Test]
    public async Task CodepointsToString_SpecificationExample() =>
        await Assert
            .That(Text("codepoints-to-string", Ints(2309, 2358, 2378, 2325)))
            .IsEqualTo("\u0905\u0936\u094A\u0915");

    [Test]
    public async Task CodepointsToString_Ascii() =>
        await Assert.That(Text("codepoints-to-string", Ints(72, 101, 108, 108, 111))).IsEqualTo("Hello");

    [Test]
    public async Task CodepointsToString_EmptySequence_IsTheEmptyString() =>
        await Assert.That(Shown("codepoints-to-string", XdmSequence.Empty)).IsEqualTo("string:");

    [Test]
    [Arguments(0x9, "\t")]
    [Arguments(0xA, "\n")]
    [Arguments(0xD, "\r")]
    [Arguments(0x20, " ")]
    [Arguments(0xD7FF, "\uD7FF")]
    [Arguments(0xE000, "\uE000")]
    [Arguments(0xFFFD, "\uFFFD")]
    [Arguments(0x10000, "\U00010000")]
    [Arguments(0x1F600, "\U0001F600")]
    [Arguments(0x10FFFF, "\U0010FFFF")]
    public async Task CodepointsToString_ValidXmlCharacters_AreAccepted(int codePoint, string expected) =>
        await Assert.That(Text("codepoints-to-string", Ints(codePoint))).IsEqualTo(expected);

    [Test]
    [Arguments(0)]
    [Arguments(1)]
    [Arguments(8)]
    [Arguments(0xB)]
    [Arguments(0xC)]
    [Arguments(0x1F)]
    [Arguments(0xD800)]
    [Arguments(0xDBFF)]
    [Arguments(0xDC00)]
    [Arguments(0xDFFF)]
    [Arguments(0xFFFE)]
    [Arguments(0xFFFF)]
    [Arguments(0x110000)]
    [Arguments(-1)]
    public async Task CodepointsToString_InvalidCodePoint_IsFOCH0001(int codePoint) =>
        await Assert.That(Text("codepoints-to-string", Ints(codePoint))).IsEqualTo("error:FOCH0001");

    [Test]
    [Arguments(long.MaxValue)]
    [Arguments(long.MinValue)]
    [Arguments(0x100000000L)] // would wrap to U+0000 if the value were truncated to 32 bits
    [Arguments(0x1_0000_0041L)] // would wrap to U+0041
    public async Task CodepointsToString_CodePointBeyondTheIntRange_IsFOCH0001(long codePoint) =>
        await Assert.That(Text("codepoints-to-string", Ints(codePoint))).IsEqualTo("error:FOCH0001");

    [Test]
    public async Task CodepointsToString_OneInvalidCodePointAmongValidOnes_IsFOCH0001() =>
        await Assert.That(Text("codepoints-to-string", Ints(65, 66, 0, 67))).IsEqualTo("error:FOCH0001");

    [Test]
    public async Task CodepointsToString_ASurrogatePairWrittenAsTwoCodePoints_IsFOCH0001() =>
        await Assert.That(Text("codepoints-to-string", Ints(0xD83D, 0xDE00))).IsEqualTo("error:FOCH0001");

    [Test]
    public async Task CodepointsToString_NotAnInteger_IsXPTY0004() =>
        await Assert.That(Text("codepoints-to-string", Str("65"))).IsEqualTo("error:XPTY0004");

    [Test]
    public async Task StringToCodepoints_SpecificationExample() =>
        await Assert
            .That(Shown("string-to-codepoints", Str("Th\u00E9r\u00E8se")))
            .IsEqualTo("integer:84,integer:104,integer:233,integer:114,integer:232,integer:115,integer:101");

    [Test]
    public async Task StringToCodepoints_EmptyStringAndEmptySequence_AreTheEmptySequence()
    {
        await Assert.That(Shown("string-to-codepoints", Str(""))).IsEqualTo("");
        await Assert.That(Shown("string-to-codepoints", XdmSequence.Empty)).IsEqualTo("");
    }

    [Test]
    public async Task StringToCodepoints_ALoneSurrogate_IsTheCodePointOfItsOwnCodeUnit() =>
        await Assert
            .That(Shown("string-to-codepoints", Str("a\uD800b")))
            .IsEqualTo("integer:97,integer:55296,integer:98");

    [Test]
    public async Task StringToCodepoints_NotAString_IsXPTY0004() =>
        await Assert.That(Text("string-to-codepoints", Int(5))).IsEqualTo("error:XPTY0004");

    // The code points outside the Basic Multilingual Plane: one position each, in every function that counts.
    [Test]
    public async Task OutsideTheBmp_StringLength_CountsOnePositionPerCodePoint()
    {
        await Assert.That(Shown("string-length", Str("\U0001F600"))).IsEqualTo("integer:1");
        await Assert.That(Shown("string-length", Str("a\U0001F600b\U00010000"))).IsEqualTo("integer:4");
        await Assert.That(Shown("string-length", Str("\U0010FFFF\U0010FFFF"))).IsEqualTo("integer:2");
    }

    [Test]
    [Arguments("a\U0001F600b\U0001F601c", 2.0, 2.0, "\U0001F600b")]
    [Arguments("a\U0001F600b\U0001F601c", 4.0, 1.0, "\U0001F601")]
    [Arguments("a\U0001F600b\U0001F601c", 3.0, double.PositiveInfinity, "b\U0001F601c")]
    [Arguments("\U0001F600\U0001F601", 2.0, 1.0, "\U0001F601")]
    [Arguments("\U0001F600\U0001F601", 1.0, 1.0, "\U0001F600")]
    [Arguments("\U0001F600\U0001F601", 3.0, 1.0, "")]
    public async Task OutsideTheBmp_Substring_CountsOnePositionPerCodePoint(
        string source,
        double start,
        double length,
        string expected
    ) => await Assert.That(Text("substring", Str(source), Dbl(start), Dbl(length))).IsEqualTo(expected);

    [Test]
    public async Task OutsideTheBmp_StringToCodepoints_GivesOneIntegerPerCodePoint() =>
        await Assert
            .That(Shown("string-to-codepoints", Str("a\U0001F600\U0010FFFF")))
            .IsEqualTo("integer:97,integer:128512,integer:1114111");

    [Test]
    public async Task OutsideTheBmp_CodepointsToString_WritesOneSurrogatePairPerCodePoint()
    {
        var result = Text("codepoints-to-string", Ints(0x1F600, 0x10FFFF));

        await Assert.That(result.Length).IsEqualTo(4);
        await Assert.That(result).IsEqualTo("\U0001F600\U0010FFFF");
        await Assert.That(Shown("string-length", Str(result))).IsEqualTo("integer:2");
    }

    [Test]
    public async Task OutsideTheBmp_Translate_CountsOnePositionPerCodePointInTheMapAndTheTranslation() =>
        await Assert
            .That(Text("translate", Str("\U0001F600b\U0001F601"), Str("\U0001F600\U0001F601"), Str("\U0001F601")))
            .IsEqualTo("\U0001F601b");

    [Test]
    public async Task RoundTrip_StringToCodepointsAndBack_GivesTheSameString()
    {
        var text = "a\U0001F600\u00E9\U00010000z";
        var codePoints = Call("string-to-codepoints", Str(text));

        await Assert.That(Text("codepoints-to-string", codePoints)).IsEqualTo(text);
    }
}
