namespace NetEvolve.Schemas.XPath.Tests.Unit.Functions.Documents;

using System.Threading.Tasks;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary><c>fn:id</c> and <c>fn:idref</c> follow the ID and IDREF types that the attribute declarations of the document type give.</summary>
public class IdTests
{
    private const string WithDtd =
        "<!DOCTYPE r [<!ELEMENT r ANY>"
        + "<!ATTLIST item id ID #IMPLIED n CDATA #IMPLIED>"
        + "<!ATTLIST ref to IDREF #IMPLIED>"
        + "<!ATTLIST refs to IDREFS #IMPLIED>"
        + "<!ATTLIST other id CDATA #IMPLIED to CDATA #IMPLIED>]>"
        + "<r><item id=\"a\" n=\"1\"/><item id=\"b\"/><item id=\"c\"/><ref to=\"a\"/><refs to=\"b c a\"/>"
        + "<other id=\"a\" to=\"a\"/><item id=\"a\" n=\"2\"/></r>";

    private const string WithoutDtd = "<r><item id=\"a\"/><ref to=\"a\"/></r>";

    private static string Run(string expression, string xml = WithDtd) => DocumentsHarness.Run(expression, null, xml);

    [Test]
    public async Task Id_ReturnsTheElementWhoseAttributeIsOfTypeId()
    {
        await Assert.That(Run("id('b')")).IsEqualTo("e:item");
        await Assert.That(Run("id('b')/@id")).IsEqualTo("a:id=b");
    }

    [Test]
    public async Task Id_AnAttributeOfAnotherTypeIsNoId_AndTheValueIsMatchedCaseSensitively()
    {
        await Assert.That(Run("count(id('a'))")).IsEqualTo("1");
        await Assert.That(Run("count(id('A'))")).IsEqualTo("0");
        await Assert.That(Run("count(id('zz'))")).IsEqualTo("0");
    }

    [Test]
    public async Task Id_WithDuplicateIdValues_ReturnsTheFirstElementInDocumentOrder() =>
        await Assert.That(Run("id('a')/@n")).IsEqualTo("a:n=1");

    [Test]
    public async Task Id_ReturnsTheElementsInDocumentOrderWithoutDuplicates()
    {
        await Assert.That(Run("id('c a b a c')/@id")).IsEqualTo("a:id=a|a:id=b|a:id=c");
        await Assert.That(Run("id(('c', 'b', 'c b', 'a'))/@id")).IsEqualTo("a:id=a|a:id=b|a:id=c");
    }

    [Test]
    public async Task Id_SplitsEveryStringOnWhiteSpace()
    {
        await Assert
            .That(Run("id(concat(' c', codepoints-to-string((9, 10, 13, 32)), 'b '))/@id"))
            .IsEqualTo("a:id=b|a:id=c");
        await Assert.That(Run("count(id('   '))")).IsEqualTo("0");
    }

    [Test]
    public async Task Id_EmptyArgument_GivesTheEmptySequence() =>
        await Assert.That(Run("count(id(()))")).IsEqualTo("0");

    [Test]
    public async Task Id_ReadsTheNodesOfAnAttributeAsTheirStringValue()
    {
        await Assert.That(Run("id(/r/ref/@to)/@id")).IsEqualTo("a:id=a");
        await Assert.That(Run("id(/r/refs/@to)/@id")).IsEqualTo("a:id=a|a:id=b|a:id=c");
    }

    [Test]
    public async Task Id_WithoutIdTyping_GivesTheEmptySequence()
    {
        await Assert.That(Run("count(id('a'))", WithoutDtd)).IsEqualTo("0");
        await Assert.That(Run("count(idref('a'))", WithoutDtd)).IsEqualTo("0");
    }

    [Test]
    public async Task Id_SecondArgument_NamesTheTreeToSearch()
    {
        // The context node is in the document of the second argument, so the context and the argument agree.
        await Assert.That(Run("/r/other/id('b', .)/@id")).IsEqualTo("a:id=b");
        await Assert.That(Run("id('b', /r/ref)/@id")).IsEqualTo("a:id=b");
    }

    [Test]
    public async Task Id_FromTheContextItem_RaisesXPDY0002WithoutOneAndXPTY0004ForAnAtomicValue()
    {
        using var empty = new XPathEvaluationContext();
        using var atomic = new XPathEvaluationContext();
        atomic.SetContextItem(XdmItem.FromInteger(1));

        await Assert.That(DocumentsHarness.Run("id('a')", empty)).IsEqualTo("error:XPDY0002");
        await Assert.That(DocumentsHarness.Run("id('a')", atomic)).IsEqualTo("error:XPTY0004");
        await Assert.That(DocumentsHarness.Run("idref('a')", empty)).IsEqualTo("error:XPDY0002");
        await Assert.That(DocumentsHarness.Run("idref('a')", atomic)).IsEqualTo("error:XPTY0004");
    }

    [Test]
    public async Task IdRef_ReturnsTheAttributesOfTypeIdrefAndIdrefsThatNameTheId()
    {
        await Assert.That(Run("idref('a')")).IsEqualTo("a:to=a|a:to=b c a");
        await Assert.That(Run("idref('b')")).IsEqualTo("a:to=b c a");
        await Assert.That(Run("count(idref('zz'))")).IsEqualTo("0");
    }

    [Test]
    public async Task IdRef_AnAttributeOfAnotherTypeIsNoReference()
    {
        await Assert.That(Run("count(idref('a')[local-name() = 'to'])")).IsEqualTo("2");
        await Assert.That(Run("count(idref('a')/parent::other)")).IsEqualTo("0");
    }

    [Test]
    public async Task IdRef_ReturnsTheNodesInDocumentOrderWithoutDuplicates()
    {
        await Assert.That(Run("idref('c b a b')")).IsEqualTo("a:to=a|a:to=b c a");
        await Assert.That(Run("count(idref(('a', 'a b')))")).IsEqualTo("2");
    }

    [Test]
    public async Task IdRef_EmptyArgument_GivesTheEmptySequence() =>
        await Assert.That(Run("count(idref(()))")).IsEqualTo("0");

    [Test]
    public async Task IdRef_SecondArgument_NamesTheTreeToSearch() =>
        await Assert.That(Run("idref('b', /r/other)")).IsEqualTo("a:to=b c a");

    [Test]
    [Arguments("id", XPathVersion.V20)]
    [Arguments("id", XPathVersion.V30)]
    [Arguments("id", XPathVersion.V31)]
    [Arguments("idref", XPathVersion.V20)]
    [Arguments("idref", XPathVersion.V30)]
    [Arguments("idref", XPathVersion.V31)]
    public async Task Registration_TakesOneOrTwoArgumentsInEveryVersion(string local, XPathVersion version)
    {
        await Assert.That(DocumentsHarness.TryResolve(local, 0, out _, version)).IsFalse();
        await Assert.That(DocumentsHarness.TryResolve(local, 1, out _, version)).IsTrue();
        await Assert.That(DocumentsHarness.TryResolve(local, 2, out _, version)).IsTrue();
        await Assert.That(DocumentsHarness.TryResolve(local, 3, out _, version)).IsFalse();
    }
}
