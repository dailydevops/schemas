namespace NetEvolve.Schemas.XPath.Tests.Unit.Functions.Strings;

using System.Threading.Tasks;
using NetEvolve.Schemas.XPath.Xdm.Values;
using static NetEvolve.Schemas.XPath.Tests.Unit.Functions.Strings.StringsHarness;

/// <summary><c>fn:string-length</c> and <c>fn:normalize-space</c> (Functions and Operators 1.0, 7.4.4 and 7.4.6), with and without the context item.</summary>
public class LengthAndSpaceTests
{
    [Test]
    [Arguments("hello", 5)]
    [Arguments("", 0)]
    [Arguments(" ", 1)]
    [Arguments("a\u00E9\u4E2D", 3)]
    [Arguments("e\u0301", 2)] // a combining mark is a code point of its own
    public async Task StringLength_CountsCodePoints(string text, int expected) =>
        await Assert.That(Shown("string-length", Str(text))).IsEqualTo("integer:" + expected);

    [Test]
    public async Task StringLength_EmptySequence_IsZero() =>
        await Assert.That(Shown("string-length", XdmSequence.Empty)).IsEqualTo("integer:0");

    [Test]
    public async Task StringLength_WithoutArgument_UsesTheStringValueOfTheContextItem()
    {
        await Assert.That(TextWithContextItem(XdmItem.FromString("abc"), "string-length")).IsEqualTo("integer:3");
        await Assert
            .That(TextWithContextItem(XdmItem.FromString("a\U0001F600"), "string-length"))
            .IsEqualTo("integer:2");
        await Assert.That(TextWithContextItem(XdmItem.FromInteger(12345), "string-length")).IsEqualTo("integer:5");
    }

    [Test]
    public async Task StringLength_WithoutArgumentAndWithoutContextItem_IsXPDY0002() =>
        await Assert.That(Text("string-length")).IsEqualTo("error:XPDY0002");

    [Test]
    public async Task StringLength_AnArgumentOfTwoItems_IsXPTY0004() =>
        await Assert.That(Text("string-length", Strs("a", "b"))).IsEqualTo("error:XPTY0004");

    [Test]
    [Arguments("  a  b\t\r\n c  ", "a b c")]
    [Arguments("abc", "abc")]
    [Arguments("", "")]
    [Arguments("   ", "")]
    [Arguments("\t\r\n", "")]
    [Arguments("a b", "a b")]
    [Arguments(" a", "a")]
    [Arguments("a ", "a")]
    [Arguments("a\n\n\nb", "a b")]
    [Arguments("a\u00A0b", "a\u00A0b")] // only the four XML white space characters collapse
    [Arguments("a\u2003b", "a\u2003b")]
    [Arguments("\U0001F600 \U0001F600", "\U0001F600 \U0001F600")]
    public async Task NormalizeSpace_TrimsAndCollapsesTheFourWhiteSpaceCharacters(string text, string expected) =>
        await Assert.That(Text("normalize-space", Str(text))).IsEqualTo(expected);

    [Test]
    public async Task NormalizeSpace_SpecificationExample() =>
        await Assert
            .That(Text("normalize-space", Str("  The    wealthy curled darlings   of our nation. ")))
            .IsEqualTo("The wealthy curled darlings of our nation.");

    [Test]
    public async Task NormalizeSpace_EmptySequence_IsTheEmptyString() =>
        await Assert.That(Shown("normalize-space", XdmSequence.Empty)).IsEqualTo("string:");

    [Test]
    public async Task NormalizeSpace_WithoutArgument_UsesTheStringValueOfTheContextItem() =>
        await Assert.That(TextWithContextItem(XdmItem.FromString("  a   b "), "normalize-space")).IsEqualTo("a b");

    [Test]
    public async Task NormalizeSpace_WithoutArgumentAndWithoutContextItem_IsXPDY0002() =>
        await Assert.That(Text("normalize-space")).IsEqualTo("error:XPDY0002");

    [Test]
    public async Task NormalizeSpace_ThatChangesNothing_ReturnsTheSameString()
    {
        var source = new string(['a', ' ', 'b']);
        var result = Call("normalize-space", Str(source));

        await Assert.That(result[0].TryGetString(out var text)).IsTrue();
        await Assert.That(ReferenceEquals(text, source)).IsTrue();
    }
}
