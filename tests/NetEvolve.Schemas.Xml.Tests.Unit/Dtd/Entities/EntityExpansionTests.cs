namespace NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Entities;

using System;
using NetEvolve.Schemas.Xml.Contracts;

/// <summary>XML 1.0 sections 4.1, 4.4.2 and 4.5: expansion of internal general entities in content and attribute values.</summary>
public class EntityExpansionTests
{
    [Test]
    [Arguments("<!ENTITY e \"x\">", "<a>[&e;]</a>", "<a|'[x]'|/a")]
    [Arguments("<!ENTITY e \"x&amp;y&#65;\">", "<a>&e;</a>", "<a|'x&yA'|/a")]
    [Arguments("<!ENTITY e \"a&f;c\"><!ENTITY f \"b\">", "<a>&e;</a>", "<a|'abc'|/a")]
    [Arguments("<!ENTITY e \"\">", "<a>x&e;y</a>", "<a|'xy'|/a")]
    [Arguments("<!ENTITY e \"1\"><!ENTITY e \"2\">", "<a>&e;</a>", "<a|'1'|/a")]
    [Arguments("<!ENTITY e \"<b x='1'>t</b>!\">", "<a>&e;</a>", "<a|<b|@x=1|'t'|/b|'!'|/a")]
    [Arguments("<!ENTITY e \"<!-- c --><?p d?>t\">", "<a>&e;</a>", "<a|'t'|/a")]
    [Arguments("<!ENTITY e \"<![CDATA[<x]]>\">", "<a>&e;</a>", "<a|'<x'|/a")]
    [Arguments("<!ENTITY e \"a\r\nb\">", "<a>&e;</a>", "<a|'a\nb'|/a")]
    [Arguments("<!ENTITY e \"<p:b/>\">", "<a xmlns:p=\"u\">&e;</a>", "<a|<b|/b|/a")]
    [Arguments("<!ENTITY e \"<b xmlns='u'><c/></b>\">", "<a>&e;<c/></a>", "<a|<b|<c|/c|/b|<c|/c|/a")]
    public async Task Content_InternalEntity_IsExpanded(string declarations, string content, string expected)
    {
        ArgumentNullException.ThrowIfNull(declarations);
        ArgumentNullException.ThrowIfNull(content);
        var run = EntityHarness.Read($"<!DOCTYPE a [{declarations}]>{content}");

        await Assert.That(run.Code).IsNull();
        await Assert.That(run.Status).IsEqualTo(XmlReadStatus.Completed);
        await Assert.That(run.Events).IsEqualTo(expected);
    }

    [Test]
    public async Task Content_CharacterReferenceForLessThan_InsideAnEntity_IsMarkup()
    {
        var run = EntityHarness.Read("<!DOCTYPE a [<!ENTITY e \"&#60;b/>\">]><a>&e;</a>");

        await Assert.That(run.Code).IsNull();
        await Assert.That(run.Events).IsEqualTo("<a|<b|/b|/a");
    }

    [Test]
    public async Task Content_DoubleEscapedLessThan_InsideAnEntity_IsData()
    {
        var run = EntityHarness.Read("<!DOCTYPE a [<!ENTITY e \"&#38;#60;\">]><a>&e;</a>");

        await Assert.That(run.Code).IsNull();
        await Assert.That(run.Events).IsEqualTo("<a|'<'|/a");
    }

    [Test]
    public async Task Content_EntityOpeningAndClosingBalancedElements_NestsInsideTheOuterElement()
    {
        var run = EntityHarness.Read("<!DOCTYPE a [<!ENTITY e \"<b><c/></b>\">]><a>&e;&e;</a>");

        await Assert.That(run.Code).IsNull();
        await Assert.That(run.Events).IsEqualTo("<a|<b|<c|/c|/b|<b|<c|/c|/b|/a");
    }

    [Test]
    public async Task Content_ReferenceTextAroundAnEntity_CoalescesIntoOneTextRun()
    {
        var run = EntityHarness.Read("<!DOCTYPE a [<!ENTITY e \"b\">]><a>a&e;&#99;&e;d</a>");

        await Assert.That(run.Events).IsEqualTo("<a|'abcbd'|/a");
    }

    [Test]
    [Arguments("<!ENTITY e \"v\">", "<a x=\"[&e;]\"/>", "<a|@x=[v]|/a")]
    [Arguments("<!ENTITY e \"1&f;3\"><!ENTITY f \"2\">", "<a x='&e;'/>", "<a|@x=123|/a")]
    [Arguments("<!ENTITY e 'say \"hi\"'>", "<a x=\"&e;\"/>", "<a|@x=say \"hi\"|/a")]
    [Arguments("<!ENTITY e \"a&#9;b&#10;c&#13;d\">", "<a x=\"&e;\"/>", "<a|@x=a b c d|/a")]
    [Arguments("<!ENTITY e \"a\r\nb\">", "<a x=\"&e;\"/>", "<a|@x=a b|/a")]
    [Arguments("<!ENTITY e \"&#38;#60;\">", "<a x=\"&e;\"/>", "<a|@x=<|/a")]
    [Arguments("<!ENTITY e \"&amp;&lt;&#65;\">", "<a x=\"&e;\"/>", "<a|@x=&<A|/a")]
    [Arguments("<!ENTITY e \"\">", "<a x=\"p&e;q\"/>", "<a|@x=pq|/a")]
    [Arguments("<!ENTITY e \"v\">", "<a x=\"&e;\" y=\"&e;&e;\"/>", "<a|@x=v|@y=vv|/a")]
    public async Task Attribute_InternalEntity_IsExpandedAndNormalized(
        string declarations,
        string content,
        string expected
    )
    {
        ArgumentNullException.ThrowIfNull(declarations);
        ArgumentNullException.ThrowIfNull(content);
        var run = EntityHarness.Read($"<!DOCTYPE a [{declarations}]>{content}");

        await Assert.That(run.Code).IsNull();
        await Assert.That(run.Status).IsEqualTo(XmlReadStatus.Completed);
        await Assert.That(run.Events).IsEqualTo(expected);
    }

    [Test]
    public async Task Attribute_CharacterReferenceForLessThan_InsideAnEntity_RaisesXml0013()
    {
        var run = EntityHarness.Read("<!DOCTYPE a [<!ENTITY e \"&#60;\">]><a x=\"&e;\"/>");

        await Assert.That(run.Status).IsEqualTo(XmlReadStatus.Error);
        await Assert.That(run.Code).IsEqualTo("XML0013");
    }

    [Test]
    public async Task Attribute_LiteralLessThanInsideANestedEntity_RaisesXml0013()
    {
        var run = EntityHarness.Read("<!DOCTYPE a [<!ENTITY e \"&f;\"><!ENTITY f \"x&#60;y\">]><a x=\"&e;\"/>");

        await Assert.That(run.Code).IsEqualTo("XML0013");
    }

    [Test]
    public async Task Attribute_EntityInsideAnElementThatComesFromAnEntity_IsExpanded()
    {
        var run = EntityHarness.Read("<!DOCTYPE a [<!ENTITY e \"<b x='&f;'/>\"><!ENTITY f \"v\">]><a>&e;</a>");

        await Assert.That(run.Code).IsNull();
        await Assert.That(run.Events).IsEqualTo("<a|<b|@x=v|/b|/a");
    }

    // XML 1.0 section 4.5: the replacement text keeps a carriage return written as a character reference; raw line ends of
    // the literal are normalized (section 2.11) before character references are expanded (#645).
    [Test]
    [Arguments("<!ENTITY e \"&#13;\">", "<doc>&e;</doc>", "<doc|'\r'|/doc")]
    [Arguments("<!ENTITY e \"&#xD;\">", "<doc>&e;</doc>", "<doc|'\r'|/doc")]
    [Arguments("<!ENTITY e \"a&#13;&#10;b\">", "<doc>&e;</doc>", "<doc|'a\r\nb'|/doc")]
    [Arguments("<!ENTITY e \"a\r\nb\rc&#13;d\">", "<doc>&e;</doc>", "<doc|'a\nb\nc\rd'|/doc")]
    [Arguments("<!ENTITY e \"&f;\"><!ENTITY f \"&#13;\">", "<doc>&e;</doc>", "<doc|'\r'|/doc")]
    [Arguments("<!ENTITY e \"&#38;#13;\">", "<doc>&e;</doc>", "<doc|'\r'|/doc")]
    [Arguments("<!ENTITY e \"<![CDATA[x&#13;y]]>\">", "<doc>&e;</doc>", "<doc|'x\ry'|/doc")]
    public async Task Content_CarriageReturnFromACharacterReference_IsKept(
        string declarations,
        string content,
        string expected
    )
    {
        ArgumentNullException.ThrowIfNull(declarations);
        ArgumentNullException.ThrowIfNull(content);
        var run = EntityHarness.Read($"<!DOCTYPE doc [{declarations}]>{content}");

        await Assert.That(run.Code).IsNull();
        await Assert.That(run.Events).IsEqualTo(expected);
    }

    [Test]
    [Arguments("<!ENTITY e \"&#13;&#10;\">", "<doc a=\"x&e;y\"/>", "<doc|@a=x  y|/doc")]
    [Arguments("<!ENTITY e \"&#13;\">", "<doc a=\"x&e;y\"/>", "<doc|@a=x y|/doc")]
    [Arguments("<!ENTITY e \"&#10;&#13;\">", "<doc a=\"x&e;y\"/>", "<doc|@a=x  y|/doc")]
    [Arguments(
        "<!ENTITY e \"&f;&g;\"><!ENTITY f \"&#13;\"><!ENTITY g \"&#10;\">",
        "<doc a=\"&e;\"/>",
        "<doc|@a=  |/doc"
    )]
    [Arguments("<!ENTITY e \"x\r\ny\">", "<doc a=\"&e;\"/>", "<doc|@a=x y|/doc")]
    [Arguments("<!ENTITY e \"<b a='&#13;&#10;'/>\">", "<doc>&e;</doc>", "<doc|<b|@a=  |/b|/doc")]
    public async Task Attribute_WhiteSpaceFromCharacterReferences_BecomesOneSpaceEach(
        string declarations,
        string content,
        string expected
    )
    {
        ArgumentNullException.ThrowIfNull(declarations);
        ArgumentNullException.ThrowIfNull(content);
        var run = EntityHarness.Read($"<!DOCTYPE doc [{declarations}]>{content}");

        await Assert.That(run.Code).IsNull();
        await Assert.That(run.Events).IsEqualTo(expected);
    }

    // W3C xmltest valid/sa/068 and valid/sa/110: the conformance harness only checks acceptance.
    [Test]
    public async Task XmlTestValidSa068_CarriageReturnEntity_ReadsACarriageReturn()
    {
        var run = EntityHarness.Read("<!DOCTYPE doc [\n<!ENTITY e \"&#13;\">\n]>\n<doc>&e;</doc>\n");

        await Assert.That(run.Code).IsNull();
        await Assert.That(run.Events).IsEqualTo("<doc|'\r'|/doc");
    }

    [Test]
    public async Task XmlTestValidSa110_CarriageReturnLineFeedEntity_InAnAttributeIsTwoSpaces()
    {
        var run = EntityHarness.Read("<!DOCTYPE doc [\n<!ENTITY e \"&#13;&#10;\">\n]>\n<doc a=\"x&e;y\"></doc>\n");

        await Assert.That(run.Code).IsNull();
        await Assert.That(run.Events).IsEqualTo("<doc|@a=x  y|/doc");
    }
}
