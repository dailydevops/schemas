namespace NetEvolve.Schemas.XPath.Tests.Unit.Functions.Strings;

using System.Threading.Tasks;
using NetEvolve.Schemas.XPath.Xdm.Values;
using static NetEvolve.Schemas.XPath.Tests.Unit.Functions.Strings.StringsHarness;

/// <summary><c>fn:translate</c> (Functions and Operators 1.0, 7.4.7): the examples of the specification, duplicates in the map string, and code points outside the Basic Multilingual Plane.</summary>
public class TranslateTests
{
    [Test]
    [Arguments("bar", "abc", "ABC", "BAr")]
    [Arguments("--aaa--", "abc-", "ABC", "AAA")]
    [Arguments("abcdabc", "abc", "AB", "ABdAB")]
    [Arguments("abc", "", "", "abc")]
    [Arguments("", "abc", "ABC", "")]
    [Arguments("aaa", "aa", "bc", "bbb")] // the first occurrence in the map string wins
    [Arguments("abc", "abc", "", "")]
    [Arguments("abc", "b", "BBB", "aBc")] // surplus characters of the translate string are ignored
    public async Task Translate_ExamplesAndEdges(string source, string map, string translation, string expected) =>
        await Assert.That(Text("translate", Str(source), Str(map), Str(translation))).IsEqualTo(expected);

    [Test]
    public async Task Translate_EmptySequence_IsTheEmptyString() =>
        await Assert.That(Shown("translate", XdmSequence.Empty, Str("abc"), Str("ABC"))).IsEqualTo("string:");

    [Test]
    [Arguments("a\U0001F600b", "\U0001F600", "x", "axb")]
    [Arguments("a\U0001F600b", "a", "\U0001F600", "\U0001F600\U0001F600b")]
    [Arguments("a\U0001F600b", "\U0001F600", "", "ab")]
    [Arguments("\U0001F600\U0001F601", "\U0001F601\U0001F600", "AB", "BA")]
    [Arguments("abc", "abc", "\U0001F600\U0001F601", "\U0001F600\U0001F601c")] // one position per code point in the translate string
    public async Task Translate_CodePointsOutsideTheBmp_CountOnePositionEach(
        string source,
        string map,
        string translation,
        string expected
    ) => await Assert.That(Text("translate", Str(source), Str(map), Str(translation))).IsEqualTo(expected);

    [Test]
    public async Task Translate_MissingMapString_IsXPTY0004() =>
        await Assert.That(Text("translate", Str("abc"), XdmSequence.Empty, Str("x"))).IsEqualTo("error:XPTY0004");

    [Test]
    public async Task Translate_TwoArguments_IsNotRegistered() =>
        await Assert.That(Text("translate", Str("abc"), Str("a"))).IsEqualTo("error:not-registered");

    [Test]
    public async Task Translate_ThatChangesNothing_ReturnsTheSameString()
    {
        var source = new string(['a', 'b', 'c']);
        var result = Call("translate", Str(source), Str("xyz"), Str("XYZ"));

        await Assert.That(result[0].TryGetString(out var text)).IsTrue();
        await Assert.That(ReferenceEquals(text, source)).IsTrue();
    }
}
