namespace NetEvolve.Schemas.Xml.Tests.Unit.Writer;

using System.Xml.Linq;
using NetEvolve.Schemas.Xml.Contracts;
using static NetEvolve.Schemas.Xml.Tests.Unit.Writer.WriterHarness;

public class EscapingTests
{
    [Test]
    [Arguments("<", "&lt;")]
    [Arguments("&", "&amp;")]
    [Arguments(">", "&gt;")]
    [Arguments("\"", "\"")]
    [Arguments("'", "'")]
    [Arguments("\t", "\t")]
    [Arguments("\n", "\n")]
    [Arguments("\r", "&#13;")]
    [Arguments("]]>", "]]&gt;")]
    public async Task Text_Character_IsEscapedAndSurvivesReRead(string value, string expected)
    {
        var xml = Chars(
            (w, _) =>
            {
                w.Start("a");
                w.Text(value, XmlTextKind.Text);
                w.End("a");
            }
        );

        await Assert.That(xml).IsEqualTo($"<a>{expected}</a>");
        await Assert.That(XElement.Parse(xml).Value).IsEqualTo(value);
    }

    [Test]
    [Arguments("<", "&lt;")]
    [Arguments("&", "&amp;")]
    [Arguments(">", "&gt;")]
    [Arguments("\"", "&quot;")]
    [Arguments("'", "'")]
    [Arguments("\t", "&#9;")]
    [Arguments("\n", "&#10;")]
    [Arguments("\r", "&#13;")]
    public async Task Attribute_Character_IsEscapedAndSurvivesReRead(string value, string expected)
    {
        var xml = Chars(
            (w, _) =>
            {
                w.Start("a", attrs: [new Att("b", value)]);
                w.End("a");
            }
        );

        await Assert.That(xml).IsEqualTo($"<a b=\"{expected}\"/>");
        await Assert.That((string)XElement.Parse(xml).Attribute("b")!).IsEqualTo(value);
    }

    [Test]
    public async Task Text_CDataKind_IsReEmittedAsSectionInPlainMode_AndSplitAtTerminator()
    {
        var xml = Chars(
            (w, _) =>
            {
                w.Start("a");
                w.Text("x<y]]>z", XmlTextKind.CData);
                w.End("a");
            }
        );

        await Assert.That(xml).IsEqualTo("<a><![CDATA[x<y]]]]><![CDATA[>z]]></a>");
        await Assert.That(XElement.Parse(xml).Value).IsEqualTo("x<y]]>z");
    }

    [Test]
    public async Task Text_CDataKindWithCarriageReturn_FallsBackToEscapedText()
    {
        var xml = Chars(
            (w, _) =>
            {
                w.Start("a");
                w.Text("x\ry", XmlTextKind.CData);
                w.End("a");
            }
        );

        await Assert.That(xml).IsEqualTo("<a>x&#13;y</a>");
    }

    [Test]
    public async Task Element_WithoutContent_IsWrittenSelfClosing_AndWithEmptyTextToo()
    {
        var xml = Chars(
            (w, _) =>
            {
                w.Start("a");
                w.Start("b");
                w.Text("", XmlTextKind.Text);
                w.End("b");
                w.End("a");
            }
        );

        await Assert.That(xml).IsEqualTo("<a><b/></a>");
    }

    [Test]
    public async Task Document_DeclarationDoctypeCommentAndPi_AreWrittenInOrder()
    {
        var xml = Chars(
            (w, t) =>
            {
                w.StartDocument(new XmlDeclaration("1.0", "ISO-8859-1", true));
                w.Doctype(new XmlDoctypeInfo("a", "pub", "sys", "<!ELEMENT a EMPTY>"));
                w.Comment(" c ");
                w.ProcessingInstruction(t.Intern("pi"), "d a");
                w.Start("a");
                w.End("a");
                w.EndDocument();
            }
        );

        await Assert
            .That(xml)
            .IsEqualTo(
                "<?xml version=\"1.0\" standalone=\"yes\"?><!DOCTYPE a PUBLIC \"pub\" \"sys\" [<!ELEMENT a EMPTY>]><!-- c --><?pi d a?><a/>"
            );
    }
}
