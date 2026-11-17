namespace NetEvolve.Schemas.Xml.Tests.Unit.Writer;

using System;
using System.Collections.Generic;
using System.Text;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Writer;
using static NetEvolve.Schemas.Xml.Tests.Unit.Writer.WriterHarness;

/// <summary>
/// Pins each rule of the documented subset of the canonical form against hand-written expected bytes. The normative text
/// of the suite's canonical form is not available; the real output comparison (#151, XML-54) pins this subset later.
/// </summary>
public class CanonicalFormTests
{
    private static Func<(string Rule, Action<XmlWriter, XmlNameTable> Script, string Expected)> Row(
        string rule,
        Action<XmlWriter, XmlNameTable> script,
        string expected
    ) => () => (rule, script, expected);

    public static IEnumerable<Func<(string, Action<XmlWriter, XmlNameTable>, string)>> Rows()
    {
        foreach (var row in Prolog())
        {
            yield return row;
        }

        foreach (var row in Content())
        {
            yield return row;
        }

        foreach (var row in Markup())
        {
            yield return row;
        }
    }

    private static IEnumerable<Func<(string, Action<XmlWriter, XmlNameTable>, string)>> Prolog()
    {
        yield return Row(
            "xml declaration removed",
            (w, _) =>
            {
                w.StartDocument(new XmlDeclaration("1.0", "UTF-8", true));
                w.Start("a");
                w.End("a");
                w.EndDocument();
            },
            "<a></a>"
        );
        yield return Row(
            "doctype removed",
            (w, _) =>
            {
                w.Doctype(new XmlDoctypeInfo("a", "", "a.dtd", "<!ENTITY e 'v'>"));
                w.Start("a");
                w.End("a");
            },
            "<a></a>"
        );
        yield return Row(
            "attributes sorted by code point",
            (w, _) =>
            {
                w.Start("a", attrs: [new Att("c", "1"), new Att("a", "2"), new Att("b", "3")]);
                w.End("a");
            },
            "<a a=\"2\" b=\"3\" c=\"1\"></a>"
        );
        yield return Row(
            "attributes sorted by code point, not by utf-16 unit",
            (w, _) =>
            {
                w.Start("a", attrs: [new Att("\U0001D400", "1"), new Att("Ａ", "2")]);
                w.End("a");
            },
            "<a Ａ=\"2\" \U0001D400=\"1\"></a>"
        );
    }

    private static IEnumerable<Func<(string, Action<XmlWriter, XmlNameTable>, string)>> Content()
    {
        yield return Row(
            "cdata replaced by characters",
            (w, _) =>
            {
                w.Start("a");
                w.Text("<x>&", XmlTextKind.CData);
                w.End("a");
            },
            "<a>&lt;x&gt;&amp;</a>"
        );
        yield return Row(
            "references replaced by characters",
            (w, _) =>
            {
                w.Start("a");
                w.Text("A", XmlTextKind.Reference);
                w.Text("é", XmlTextKind.Reference);
                w.End("a");
            },
            "<a>Aé</a>"
        );
        yield return Row(
            "empty elements as start and end tag",
            (w, _) =>
            {
                w.Start("a");
                w.Start("b");
                w.End("b");
                w.End("a");
            },
            "<a><b></b></a>"
        );
        yield return Row(
            "double quote delimiters and attribute escaping",
            (w, _) =>
            {
                w.Start("a", attrs: [new Att("b", "x\"'<>&")]);
                w.End("a");
            },
            "<a b=\"x&quot;'&lt;&gt;&amp;\"></a>"
        );
        yield return Row(
            "white space characters in attributes as references",
            (w, _) =>
            {
                w.Start("a", attrs: [new Att("b", "\t\n\r")]);
                w.End("a");
            },
            "<a b=\"&#9;&#10;&#13;\"></a>"
        );
    }

    private static IEnumerable<Func<(string, Action<XmlWriter, XmlNameTable>, string)>> Markup()
    {
        yield return Row(
            "carriage return in text as reference",
            (w, _) =>
            {
                w.Start("a");
                w.Text("x\r\ny\tz>", XmlTextKind.Text);
                w.End("a");
            },
            "<a>x&#13;\ny\tz&gt;</a>"
        );
        yield return Row(
            "comments and processing instructions kept",
            (w, t) =>
            {
                w.Start("a");
                w.Comment(" c ");
                w.ProcessingInstruction(t.Intern("p"), "d  e");
                w.ProcessingInstruction(t.Intern("q"), "");
                w.End("a");
            },
            "<a><!-- c --><?p d  e?><?q?></a>"
        );
        yield return Row(
            "xmlns declarations sorted among the attributes",
            (w, _) =>
            {
                w.Start(
                    "r",
                    "",
                    "urn:d",
                    decls: [("z", "urn:z"), ("", "urn:d"), ("a", "urn:a")],
                    attrs: [new Att("y", "1"), new Att("b", "2")]
                );
                w.End("r", "", "urn:d");
            },
            "<r b=\"2\" xmlns=\"urn:d\" xmlns:a=\"urn:a\" xmlns:z=\"urn:z\" y=\"1\"></r>"
        );
    }

    [Test]
    [MethodDataSource(nameof(Rows))]
    public async Task CanonicalRule_ProducesTheExpectedBytes(
        string rule,
        Action<XmlWriter, XmlNameTable> script,
        string expected
    )
    {
        ArgumentNullException.ThrowIfNull(script);
        var bytes = Bytes(script, canonical: true);

        await Assert.That(Encoding.UTF8.GetString(bytes)).IsEqualTo(expected).Because(rule);
        await Assert.That(bytes).IsEquivalentTo(Encoding.UTF8.GetBytes(expected)).Because(rule);
    }

    [Test]
    public async Task CanonicalOutput_NonAsciiText_IsUtf8WithoutByteOrderMark()
    {
        var bytes = Bytes(
            (w, _) =>
            {
                w.Start("a");
                w.Text("é€\U0001F600", XmlTextKind.Text);
                w.End("a");
            },
            canonical: true,
            encoding: XmlWriterEncoding.Utf16
        );

        byte[] expected =
        [
            0x3C,
            0x61,
            0x3E,
            0xC3,
            0xA9,
            0xE2,
            0x82,
            0xAC,
            0xF0,
            0x9F,
            0x98,
            0x80,
            0x3C,
            0x2F,
            0x61,
            0x3E,
        ];
        await Assert.That(bytes).IsEquivalentTo(expected);
    }
}
