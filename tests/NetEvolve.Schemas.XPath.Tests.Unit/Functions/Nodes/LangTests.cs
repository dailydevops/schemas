namespace NetEvolve.Schemas.XPath.Tests.Unit.Functions.Nodes;

using System.Threading.Tasks;

/// <summary>
/// <c>fn:lang</c> (Functions and Operators 1.0, 14.5): the nearest <c>xml:lang</c> on the node or an ancestor decides, the match is
/// case-insensitive, and a language matches itself and every sublanguage that continues it after a hyphen.
/// </summary>
public class LangTests
{
    [Test]
    [Arguments("en", "r", "boolean:true")]
    [Arguments("en-US", "r", "boolean:true")]
    [Arguments("EN", "r", "boolean:true")]
    [Arguments("en-us", "r", "boolean:true")]
    [Arguments("EN-us", "r", "boolean:true")]
    [Arguments("e", "r", "boolean:false")]
    [Arguments("en-U", "r", "boolean:false")]
    [Arguments("en-US-x", "r", "boolean:false")]
    [Arguments("de", "r", "boolean:false")]
    public async Task Lang_OnTheElementThatHoldsTheAttribute_MatchesTheLanguageAndItsSublanguages(
        string test,
        string node,
        string expected
    ) => await Assert.That(NodesHarness.Run("lang", "string:" + test, "node:" + node)).IsEqualTo(expected);

    [Test]
    [Arguments("en", "a", "boolean:true")]
    [Arguments("en", "a/t", "boolean:true")]
    [Arguments("en", "a@x", "boolean:true")]
    [Arguments("en", "r@lang", "boolean:true")]
    [Arguments("en-us", "a/t", "boolean:true")]
    [Arguments("en", "cm", "boolean:true")]
    [Arguments("en", "pi", "boolean:true")]
    [Arguments("en", "h", "boolean:true")]
    public async Task Lang_WithXmlLangOnAnAncestor_InheritsIt(string test, string node, string expected) =>
        await Assert.That(NodesHarness.Run("lang", "string:" + test, "node:" + node)).IsEqualTo(expected);

    [Test]
    [Arguments("de", "b", "boolean:true")]
    [Arguments("de", "c", "boolean:true")]
    [Arguments("de", "b@lang", "boolean:true")]
    [Arguments("en", "c", "boolean:false")]
    [Arguments("en", "b", "boolean:false")]
    [Arguments("en", "g", "boolean:true")]
    [Arguments("EN", "g", "boolean:true")]
    [Arguments("fr", "g", "boolean:false")]
    public async Task Lang_TheNearestXmlLangDecides_EvenWhenItDoesNotMatch(string test, string node, string expected) =>
        await Assert.That(NodesHarness.Run("lang", "string:" + test, "node:" + node)).IsEqualTo(expected);

    [Test]
    [Arguments("en", "doc")]
    [Arguments("en", "o")]
    [Arguments("", "o")]
    [Arguments("en", "q")]
    [Arguments("en", "o.doc")]
    public async Task Lang_WithoutAnyXmlLang_IsFalse(string test, string node) =>
        await Assert.That(NodesHarness.Run("lang", "string:" + test, "node:" + node)).IsEqualTo("boolean:false");

    [Test]
    [Arguments("string:", "d", "boolean:true")]
    [Arguments("()", "d", "boolean:true")]
    [Arguments("string:", "r", "boolean:false")]
    [Arguments("()", "r", "boolean:false")]
    [Arguments("string:en", "d", "boolean:false")]
    public async Task Lang_WithTheEmptyTestLanguage_IsTheZeroLengthString(string test, string node, string expected) =>
        await Assert.That(NodesHarness.Run("lang", test, "node:" + node)).IsEqualTo(expected);

    [Test]
    [Arguments("de", "c", "boolean:true")]
    [Arguments("en", "c", "boolean:false")]
    [Arguments("en", "a/t", "boolean:true")]
    [Arguments("en", "g", "boolean:true")]
    [Arguments("EN-us", "r", "boolean:true")]
    public async Task Lang_WithoutNodeArgument_UsesTheContextItem(string test, string contextNode, string expected) =>
        await Assert
            .That(NodesHarness.RunWithContextItem("node:" + contextNode, "lang", "string:" + test))
            .IsEqualTo(expected);

    [Test]
    public async Task Lang_WithoutNodeArgumentAndWithoutContextItem_RaisesXPDY0002() =>
        await Assert.That(NodesHarness.Run("lang", "string:en")).IsEqualTo("error:XPDY0002");

    [Test]
    [Arguments("integer:5")]
    [Arguments("string:r")]
    public async Task Lang_WithNonNodeContextItem_RaisesXPTY0004(string contextItem) =>
        await Assert
            .That(NodesHarness.RunWithContextItem(contextItem, "lang", "string:en"))
            .IsEqualTo("error:XPTY0004");

    [Test]
    public async Task Lang_WithTheEmptySequenceAsNode_RaisesXPTY0004() =>
        await Assert.That(NodesHarness.Run("lang", "string:en", "()")).IsEqualTo("error:XPTY0004");

    [Test]
    [Arguments("string:en", "node:r|node:a")]
    [Arguments("string:en", "integer:5")]
    [Arguments("integer:5", "node:r")]
    [Arguments("string:en|string:de", "node:r")]
    public async Task Lang_WithWrongArgumentTypes_RaisesXPTY0004(string test, string node) =>
        await Assert.That(NodesHarness.Run("lang", test, node)).IsEqualTo("error:XPTY0004");

    [Test]
    public async Task Lang_WithoutArguments_IsNotRegistered() =>
        await Assert.That(NodesHarness.Run("lang")).IsEqualTo("error:not-registered");
}
