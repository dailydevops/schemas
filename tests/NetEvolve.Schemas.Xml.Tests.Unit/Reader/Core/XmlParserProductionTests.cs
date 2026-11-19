namespace NetEvolve.Schemas.Xml.Tests.Unit.Reader.Core;

using System;
using NetEvolve.Schemas.Xml.Contracts;

/// <summary>Event sequences for valid input and coded errors for invalid input, per production of XML 1.0 (Fifth Edition).</summary>
public class XmlParserProductionTests
{
    internal static XmlErrorCode CodeOf(string alias)
    {
        foreach (var code in XmlErrorCodes.All)
        {
            if (string.Equals(code.Alias, alias, StringComparison.Ordinal))
            {
                return code;
            }
        }

        throw new ArgumentException("Unknown code " + alias, nameof(alias));
    }

    // Productions 39 to 44: element, STag, Attribute, ETag, content, EmptyElemTag.
    [Test]
    [Arguments("<a/>", "SD()|<a|>|/a|ED")]
    [Arguments("<a></a>", "SD()|<a|>|/a|ED")]
    [Arguments("<a  x = \"1\"\n  />", "SD()|<a|@x=1|>|/a|ED")]
    [Arguments("<a></a >", "SD()|<a|>|/a|ED")]
    [Arguments("<a></a\r\n>", "SD()|<a|>|/a|ED")]
    [Arguments("<a>text</a>", "SD()|<a|>|T'text'|/a|ED")]
    [Arguments("<a x=\"1\" y='2'/>", "SD()|<a|@x=1|@y=2|>|/a|ED")]
    [Arguments("<a x=\"\"/>", "SD()|<a|@x=|>|/a|ED")]
    [Arguments("<a x='say \"hi\"' y=\"it's\"/>", "SD()|<a|@x=say \"hi\"|@y=it's|>|/a|ED")]
    [Arguments("<a><b/><c/></a>", "SD()|<a|>|<b|>|/b|<c|>|/c|/a|ED")]
    [Arguments("<a>x<b/>y</a>", "SD()|<a|>|T'x'|<b|>|/b|T'y'|/a|ED")]
    [Arguments("<a😀 x😀=\"😀\">😀</a😀>", "SD()|<a😀|@x😀=😀|>|T'😀'|/a😀|ED")]
    public async Task Pump_ElementProductions_EmitEvents(string xml, string expected)
    {
        ArgumentNullException.ThrowIfNull(xml);
        var run = ReaderHarness.Run(xml);

        await Assert.That(run.Status).IsEqualTo(XmlReadStatus.Completed);
        await Assert.That(run.Events).IsEqualTo(expected);
    }

    // Production 10 and section 3.3.3: attribute value normalization and references.
    [Test]
    [Arguments("<a x=\"a\tb\nc\r\nd\re\"/>", "SD()|<a|@x=a b c d e|>|/a|ED")]
    [Arguments("<a x=\"&lt;&#65;&#x42;&amp;&gt;&apos;&quot;\"/>", "SD()|<a|@x=<AB&>'\"|>|/a|ED")]
    [Arguments("<a x=\"a&#10;b&#9;c&#13;d\"/>", "SD()|<a|@x=a\nb\tc\rd|>|/a|ED")]
    [Arguments("<a x=\"&#x1F600;😀\"/>", "SD()|<a|@x=😀😀|>|/a|ED")]
    public async Task Pump_AttributeValue_IsNormalizedAndResolved(string xml, string expected)
    {
        ArgumentNullException.ThrowIfNull(xml);
        var run = ReaderHarness.Run(xml);

        await Assert.That(run.Status).IsEqualTo(XmlReadStatus.Completed);
        await Assert.That(run.Events).IsEqualTo(expected);
    }

    // Productions 14 and 15 (character data and references in it): line ends, references, surrogate pairs.
    [Test]
    [Arguments("<a>x &amp; y &#x1F600; &#65; &lt;&gt;&apos;&quot;</a>", "SD()|<a|>|T'x & y 😀 A <>'\"'|/a|ED")]
    [Arguments("<a>l1\r\nl2\rl3\nl4</a>", "SD()|<a|>|T'l1\nl2\nl3\nl4'|/a|ED")]
    [Arguments("<a>] ]] ]x ]]x</a>", "SD()|<a|>|T'] ]] ]x ]]x'|/a|ED")]
    [Arguments("<a>\t </a>", "SD()|<a|>|T'\t '|/a|ED")]
    public async Task Pump_CharacterData_EmitsText(string xml, string expected)
    {
        ArgumentNullException.ThrowIfNull(xml);
        var run = ReaderHarness.Run(xml);

        await Assert.That(run.Status).IsEqualTo(XmlReadStatus.Completed);
        await Assert.That(run.Events).IsEqualTo(expected);
    }

    // Productions 15 to 17 and 18 to 21: comments, processing instructions, CDATA sections.
    [Test]
    [Arguments("<!--c--><a/>", "SD()|C'c'|<a|>|/a|ED")]
    [Arguments("<a><!-- x - y --></a>", "SD()|<a|>|C' x - y '|/a|ED")]
    [Arguments("<a><!----></a>", "SD()|<a|>|C''|/a|ED")]
    [Arguments("<a><!--a\r\nb--></a>", "SD()|<a|>|C'a\nb'|/a|ED")]
    [Arguments("<?t d?><a/>", "SD()|PI(t,d)|<a|>|/a|ED")]
    [Arguments("<?t?><a/>", "SD()|PI(t,)|<a|>|/a|ED")]
    [Arguments("<?t  d  e ?><a/>", "SD()|PI(t,d  e )|<a|>|/a|ED")]
    [Arguments("<a><?t a\r\nb?></a>", "SD()|<a|>|PI(t,a\nb)|/a|ED")]
    [Arguments("<a><?xml-stylesheet href='x'?></a>", "SD()|<a|>|PI(xml-stylesheet,href='x')|/a|ED")]
    [Arguments("<a><![CDATA[<x>&]]></a>", "SD()|<a|>|CD'<x>&'|/a|ED")]
    [Arguments("<a><![CDATA[]]></a>", "SD()|<a|>|/a|ED")]
    [Arguments("<a><![CDATA[a]b]]c]]></a>", "SD()|<a|>|CD'a]b]]c'|/a|ED")]
    [Arguments("<a><![CDATA[x]]]]></a>", "SD()|<a|>|CD'x]]'|/a|ED")]
    [Arguments("<a>x<![CDATA[y]]>z</a>", "SD()|<a|>|T'x'|CD'y'|T'z'|/a|ED")]
    [Arguments("<a><![CDATA[a\r\nb\rc]]></a>", "SD()|<a|>|CD'a\nb\nc'|/a|ED")]
    public async Task Pump_CommentPiAndCData_EmitEvents(string xml, string expected)
    {
        ArgumentNullException.ThrowIfNull(xml);
        var run = ReaderHarness.Run(xml);

        await Assert.That(run.Status).IsEqualTo(XmlReadStatus.Completed);
        await Assert.That(run.Events).IsEqualTo(expected);
    }

    // The XML declaration and the prolog and epilog: white space outside the root is not reported.
    [Test]
    [Arguments("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><a/>", "SD(1.0)|<a|>|/a|ED")]
    [Arguments("<?xml version='1.0'?>\n<a/>\n<!--c-->\n", "SD(1.0)|<a|>|/a|C'c'|ED")]
    [Arguments("﻿<a/>", "SD()|<a|>|/a|ED")]
    [Arguments("﻿<?xml version=\"1.0\"?><a/>", "SD(1.0)|<a|>|/a|ED")]
    [Arguments("\r\n  <a/>\r\n", "SD()|<a|>|/a|ED")]
    public async Task Pump_PrologAndEpilog_EmitEvents(string xml, string expected)
    {
        ArgumentNullException.ThrowIfNull(xml);
        var run = ReaderHarness.Run(xml);

        await Assert.That(run.Status).IsEqualTo(XmlReadStatus.Completed);
        await Assert.That(run.Events).IsEqualTo(expected);
    }

    // Namespaces: the reader calls the namespace layer; declarations precede the attributes, even when they come later.
    [Test]
    [Arguments("<p:a xmlns:p=\"u\" p:x=\"1\"/>", "SD()|<{u}p:a|xmlns:p=u|@{u}p:x=1|>|/{u}p:a|ED")]
    [Arguments("<p:a p:x=\"1\" xmlns:p=\"u\"/>", "SD()|<{u}p:a|xmlns:p=u|@{u}p:x=1|>|/{u}p:a|ED")]
    [Arguments("<a xmlns=\"u\"><b/></a>", "SD()|<{u}a|xmlns:=u|>|<{u}b|>|/{u}b|/{u}a|ED")]
    [Arguments("<a xmlns=\"u\" x=\"1\"/>", "SD()|<{u}a|xmlns:=u|@x=1|>|/{u}a|ED")]
    [Arguments("<a xmlns=\"u\"><b xmlns=\"\"/></a>", "SD()|<{u}a|xmlns:=u|>|<b|xmlns:=|>|/b|/{u}a|ED")]
    [Arguments(
        "<a xmlns:p=\"u\"><p:b xmlns:p=\"v\"/><p:c/></a>",
        "SD()|<a|xmlns:p=u|>|<{v}p:b|xmlns:p=v|>|/{v}p:b|<{u}p:c|>|/{u}p:c|/a|ED"
    )]
    [Arguments("<a xml:lang=\"en\"/>", "SD()|<a|@{http://www.w3.org/XML/1998/namespace}xml:lang=en|>|/a|ED")]
    public async Task Pump_Namespaces_ResolveNames(string xml, string expected)
    {
        ArgumentNullException.ThrowIfNull(xml);
        var run = ReaderHarness.Run(xml);

        await Assert.That(run.Status).IsEqualTo(XmlReadStatus.Completed);
        await Assert.That(run.Events).IsEqualTo(expected);
    }

    [Test]
    // Input ends inside a construct.
    [Arguments("<a>", "XML0001")]
    [Arguments("<a", "XML0001")]
    [Arguments("<a x=", "XML0001")]
    [Arguments("<a x='1'", "XML0001")]
    [Arguments("<a></a", "XML0001")]
    [Arguments("<a><![CDATA[x", "XML0001")]
    [Arguments("<a><!--x", "XML0001")]
    [Arguments("<a><?t x", "XML0001")]
    [Arguments("<a>&amp", "XML0001")]
    [Arguments("<?xml version=\"1.0\"", "XML0001")]
    // Character and syntax errors.
    [Arguments("<a>\u0001</a>", "XML0002")]
    [Arguments("<a>\uD800</a>", "XML0002")]
    [Arguments("<a><!--\uFFFE--></a>", "XML0002")]
    [Arguments("<a><?t \u0000?></a>", "XML0002")]
    [Arguments("<?xml?><a/>", "XML0003")]
    [Arguments("<a>]]></a>", "XML0004")]
    [Arguments("<a></b>", "XML0005")]
    [Arguments("</a>", "XML0005")]
    [Arguments("<a/></a>", "XML0005")]
    [Arguments("<p:a xmlns:p=\"u\" xmlns:q=\"u\"></q:a>", "XML0005")]
    [Arguments("<a x=\"1\" x=\"2\"/>", "XML0006")]
    [Arguments("<a xmlns:p=\"u\" xmlns:p=\"v\"/>", "XML0006")]
    [Arguments("<?xml version=\"1.0\"?><?xml version=\"1.0\"?><a/>", "XML0007")]
    [Arguments("<a/><?xml x?>", "XML0007")]
    [Arguments("<a>&foo;</a>", "XML0008")]
    [Arguments("<a x=\"&foo;\"/>", "XML0008")]
    [Arguments("<a>&#0;</a>", "XML0009")]
    [Arguments("<a>&#xD800;</a>", "XML0009")]
    [Arguments("<a>&#x110000;</a>", "XML0009")]
    [Arguments("<?xml version=\"2.0\"?><a/>", "XML0010")]
    [Arguments("<?xml version=\"1.1\"?><a/>", "XML0010")]
    [Arguments("<a b></a>", "XML0011")]
    [Arguments("<a b=1/>", "XML0011")]
    [Arguments("<1/>", "XML0011")]
    [Arguments("< a/>", "XML0011")]
    [Arguments("<a x=\"1\"y=\"2\"/>", "XML0011")]
    [Arguments("<a/ >", "XML0011")]
    [Arguments("<a></a x>", "XML0011")]
    [Arguments("<a><?1 x?></a>", "XML0011")]
    [Arguments("<a><?t?x?></a>", "XML0011")]
    [Arguments("<a><!x></a>", "XML0011")]
    [Arguments("", "XML0012")]
    [Arguments("<!--c-->", "XML0012")]
    [Arguments("<a/><b/>", "XML0012")]
    [Arguments("<a x=\"<\"/>", "XML0013")]
    [Arguments("<a><!-- -- --></a>", "XML0014")]
    [Arguments("<a><!--a--->", "XML0014")]
    [Arguments("<a/><?XML x?>", "XML0015")]
    [Arguments("<?Xml?><a/>", "XML0015")]
    [Arguments("<a>&foo<", "XML0016")]
    [Arguments("<a>& </a>", "XML0016")]
    [Arguments("<a>&#;</a>", "XML0016")]
    [Arguments("<a>&#x;</a>", "XML0016")]
    [Arguments("<?xml version=\"1.0\" standalone=\"maybe\"?><a/>", "XML0017")]
    [Arguments("text<a/>", "XML0018")]
    [Arguments("<a/>text", "XML0018")]
    [Arguments("<a/>&amp;", "XML0018")]
    [Arguments("<a/><![CDATA[x]]>", "XML0018")]
    [Arguments("&#32;<a/>", "XML0018")]
    // Namespaces and the document type declaration.
    [Arguments("<p:a/>", "XML3001")]
    [Arguments("<a xmlns:xmlns=\"u\"/>", "XML3002")]
    [Arguments("<a xmlns:p=\"u\" xmlns:q=\"u\" p:x=\"1\" q:x=\"2\"/>", "XML3003")]
    [Arguments("<a:b:c/>", "XML3004")]
    [Arguments("<a xmlns:p=\"\"/>", "XML3005")]
    [Arguments("<a><?p:t data?></a>", "XML3004")]
    [Arguments("<?p:t?><a/>", "XML3004")]
    [Arguments("<!DOCTYPE a><a/>", "XML2001")]
    public async Task Pump_InvalidInput_ReportsCode(string xml, string alias)
    {
        ArgumentNullException.ThrowIfNull(xml);
        var run = ReaderHarness.Run(xml);

        await Assert.That(run.Status).IsEqualTo(XmlReadStatus.Error);
        await Assert.That(run.Code).IsSameReferenceAs(CodeOf(alias));
    }

    [Test]
    public async Task Pump_EndTagMismatch_ReportsPositionOfTheEndTag()
    {
        var run = ReaderHarness.Run("<a>\n  </b>");

        await Assert.That(run.Code).IsSameReferenceAs(XmlErrorCodes.XML0005);
        await Assert.That(run.ErrorPosition).IsEqualTo(new XmlPosition(2, 3, 6));
    }

    [Test]
    public async Task Pump_StartElement_CarriesThePositionOfTheStartTag()
    {
        var run = ReaderHarness.Run("<a>\r\n <b/></a>");

        await Assert.That(run.Positions).IsEqualTo(new XmlPosition(1, 1, 0) + ";" + new XmlPosition(2, 2, 6) + ";");
    }

    [Test]
    public async Task Pump_Position_CountsUtf16UnitsAndCrLfAsOneLineBreak()
    {
        var run = ReaderHarness.Run("<a>😀\r\n\r\n\t</a>");

        // 3 + 2 units, CRLF twice (4 units, two lines), tab, then 4 more.
        await Assert.That(run.Position).IsEqualTo(new XmlPosition(3, 6, 14));
    }

    [Test]
    public async Task Pump_ByteOrderMark_IsNotCounted()
    {
        var run = ReaderHarness.Run("﻿<a/>");

        await Assert.That(run.Position).IsEqualTo(new XmlPosition(1, 5, 4));
    }
}
