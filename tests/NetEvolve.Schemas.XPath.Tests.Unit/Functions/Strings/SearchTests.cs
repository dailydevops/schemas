namespace NetEvolve.Schemas.XPath.Tests.Unit.Functions.Strings;

using System.Threading.Tasks;
using NetEvolve.Schemas.XPath.Xdm.Values;
using static NetEvolve.Schemas.XPath.Tests.Unit.Functions.Strings.StringsHarness;

/// <summary>
/// <c>fn:contains</c>, <c>fn:starts-with</c>, <c>fn:ends-with</c>, <c>fn:substring-before</c>, and <c>fn:substring-after</c>
/// (Functions and Operators 1.0, 7.5): the examples of the specification, the empty sequence, the empty string, and the collation
/// argument, which obtains the collation through <c>CollationArgument</c> and so accepts the codepoint collation only.
/// </summary>
public class SearchTests
{
    private const string Other = "http://www.w3.org/2005/xpath-functions/collation/html-ascii-case-insensitive";

    [Test]
    [Arguments("contains", "tattoo", "t", "boolean:true")]
    [Arguments("contains", "tattoo", "ttoo", "boolean:true")]
    [Arguments("contains", "tattoo", "z", "boolean:false")]
    [Arguments("contains", "tattoo", "", "boolean:true")]
    [Arguments("contains", "", "", "boolean:true")]
    [Arguments("contains", "", "a", "boolean:false")]
    [Arguments("contains", "a\U0001F600b", "\U0001F600", "boolean:true")]
    [Arguments("contains", "ab", "\U0001F600", "boolean:false")]
    [Arguments("contains", "Tattoo", "tat", "boolean:false")]
    [Arguments("starts-with", "tattoo", "tat", "boolean:true")]
    [Arguments("starts-with", "tattoo", "att", "boolean:false")]
    [Arguments("starts-with", "tattoo", "", "boolean:true")]
    [Arguments("starts-with", "", "", "boolean:true")]
    [Arguments("starts-with", "", "a", "boolean:false")]
    [Arguments("starts-with", "\U0001F600b", "\U0001F600", "boolean:true")]
    [Arguments("ends-with", "tattoo", "too", "boolean:true")]
    [Arguments("ends-with", "tattoo", "tat", "boolean:false")]
    [Arguments("ends-with", "tattoo", "", "boolean:true")]
    [Arguments("ends-with", "", "", "boolean:true")]
    [Arguments("ends-with", "", "a", "boolean:false")]
    [Arguments("ends-with", "b\U0001F600", "\U0001F600", "boolean:true")]
    [Arguments("substring-before", "tattoo", "attoo", "string:t")]
    [Arguments("substring-before", "tattoo", "tatto", "string:")]
    [Arguments("substring-before", "tattoo", "tattoo", "string:")]
    [Arguments("substring-before", "tattoo", "", "string:")]
    [Arguments("substring-before", "tattoo", "z", "string:")]
    [Arguments("substring-before", "a\U0001F600b", "b", "string:a\U0001F600")]
    [Arguments("substring-after", "tattoo", "tat", "string:too")]
    [Arguments("substring-after", "tattoo", "tattoo", "string:")]
    [Arguments("substring-after", "tattoo", "", "string:tattoo")]
    [Arguments("substring-after", "tattoo", "z", "string:")]
    [Arguments("substring-after", "tattoo", "o", "string:o")]
    [Arguments("substring-after", "a\U0001F600b", "\U0001F600", "string:b")]
    public async Task TwoArguments_ExamplesAndEdges(string local, string first, string second, string expected) =>
        await Assert.That(Shown(local, Str(first), Str(second))).IsEqualTo(expected);

    [Test]
    [Arguments("contains", "boolean:false")]
    [Arguments("starts-with", "boolean:false")]
    [Arguments("ends-with", "boolean:false")]
    [Arguments("substring-before", "string:")]
    [Arguments("substring-after", "string:")]
    public async Task EmptySequenceAsTheFirstArgument_IsTheEmptyString(string local, string expected) =>
        await Assert.That(Shown(local, XdmSequence.Empty, Str("a"))).IsEqualTo(expected);

    [Test]
    [Arguments("contains", "boolean:true")]
    [Arguments("starts-with", "boolean:true")]
    [Arguments("ends-with", "boolean:true")]
    [Arguments("substring-before", "string:")]
    [Arguments("substring-after", "string:abc")]
    public async Task EmptySequenceAsTheSecondArgument_IsTheEmptyString(string local, string expected) =>
        await Assert.That(Shown(local, Str("abc"), XdmSequence.Empty)).IsEqualTo(expected);

    [Test]
    [Arguments("contains", "boolean:true")]
    [Arguments("starts-with", "boolean:true")]
    [Arguments("ends-with", "boolean:true")]
    [Arguments("substring-before", "string:")]
    [Arguments("substring-after", "string:")]
    public async Task BothEmptySequences_AreTwoEmptyStrings(string local, string expected) =>
        await Assert.That(Shown(local, XdmSequence.Empty, XdmSequence.Empty)).IsEqualTo(expected);

    [Test]
    [Arguments("contains", "att", "boolean:true")]
    [Arguments("starts-with", "tat", "boolean:true")]
    [Arguments("ends-with", "too", "boolean:true")]
    [Arguments("substring-before", "attoo", "string:t")]
    [Arguments("substring-after", "tat", "string:too")]
    public async Task CodepointCollationArgument_IsAccepted(string local, string second, string expected) =>
        await Assert.That(Shown(local, Str("tattoo"), Str(second), Str(Codepoint))).IsEqualTo(expected);

    [Test]
    [Arguments("contains")]
    [Arguments("starts-with")]
    [Arguments("ends-with")]
    [Arguments("substring-before")]
    [Arguments("substring-after")]
    public async Task OtherCollationArgument_IsFOCH0002(string local) =>
        await Assert.That(Text(local, Str("tattoo"), Str("t"), Str(Other))).IsEqualTo("error:FOCH0002");

    [Test]
    [Arguments("contains")]
    [Arguments("starts-with")]
    [Arguments("ends-with")]
    [Arguments("substring-before")]
    [Arguments("substring-after")]
    public async Task ThreeArgumentsWithAnEmptyCollation_IsXPTY0004(string local) =>
        await Assert.That(Text(local, Str("tattoo"), Str("t"), XdmSequence.Empty)).IsEqualTo("error:XPTY0004");

    [Test]
    [Arguments("contains")]
    [Arguments("starts-with")]
    [Arguments("ends-with")]
    [Arguments("substring-before")]
    [Arguments("substring-after")]
    public async Task OneArgumentAndFourArguments_AreNotRegistered(string local)
    {
        await Assert.That(Text(local, Str("a"))).IsEqualTo("error:not-registered");
        await Assert.That(Text(local, Str("a"), Str("a"), Str(Codepoint), Str("x"))).IsEqualTo("error:not-registered");
    }

    [Test]
    public async Task SubstringBeforeAndAfter_AreCaseSensitive()
    {
        await Assert.That(Text("substring-before", Str("Tattoo"), Str("t"))).IsEqualTo("Ta");
        await Assert.That(Text("substring-after", Str("Tattoo"), Str("T"))).IsEqualTo("attoo");
    }
}
