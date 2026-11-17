namespace NetEvolve.Schemas.Xml.Tests.Unit.Writer;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Writer;
using static NetEvolve.Schemas.Xml.Tests.Unit.Writer.WriterHarness;

public class InvalidInputTests
{
    private static Func<(string Name, bool OpenTag, Action<XmlWriter, XmlNameTable> Act, string Alias)> Row(
        string name,
        bool openTag,
        Action<XmlWriter, XmlNameTable> act,
        string alias
    ) => () => (name, openTag, act, alias);

    public static IEnumerable<Func<(string, bool, Action<XmlWriter, XmlNameTable>, string)>> Rows()
    {
        foreach (var row in Names())
        {
            yield return row;
        }

        foreach (var row in Characters())
        {
            yield return row;
        }

        foreach (var row in Markup())
        {
            yield return row;
        }
    }

    private static IEnumerable<Func<(string, bool, Action<XmlWriter, XmlNameTable>, string)>> Names()
    {
        yield return Row(
            "element name starting with a digit",
            false,
            (w, t) => w.StartElement(N(t, "1a"), default),
            "XML3004"
        );
        yield return Row(
            "element local name with colon",
            false,
            (w, t) => w.StartElement(N(t, "a:b"), default),
            "XML3004"
        );
        yield return Row("empty element local name", false, (w, t) => w.StartElement(N(t, ""), default), "XML3004");
        yield return Row(
            "invalid element prefix",
            false,
            (w, t) => w.StartElement(N(t, "a", "1p", "urn:x"), default),
            "XML3004"
        );
        yield return Row(
            "prefix without namespace",
            false,
            (w, t) => w.StartElement(N(t, "a", "p"), default),
            "XML3001"
        );
        yield return Row(
            "attribute name starting with a digit",
            true,
            (w, t) => w.Attribute(N(t, "1a"), "v"),
            "XML3004"
        );
    }

    private static IEnumerable<Func<(string, bool, Action<XmlWriter, XmlNameTable>, string)>> Characters()
    {
        yield return Row(
            "attribute value with control character",
            true,
            (w, t) => w.Attribute(N(t, "a"), "\u0001"),
            "XML0002"
        );
        yield return Row(
            "attribute value with lone high surrogate",
            true,
            (w, t) => w.Attribute(N(t, "a"), "x\uD800"),
            "XML0002"
        );
        yield return Row(
            "attribute value with noncharacter",
            true,
            (w, t) => w.Attribute(N(t, "a"), "\uFFFE"),
            "XML0002"
        );
        yield return Row(
            "prefixed declaration without uri",
            true,
            (w, t) => w.NamespaceDeclaration(t.Intern("p"), default),
            "XML3005"
        );
        yield return Row("text with null character", false, (w, _) => w.Text("a\u0000", XmlTextKind.Text), "XML0002");
        yield return Row(
            "text with lone low surrogate",
            false,
            (w, _) => w.Text("\uDC00x", XmlTextKind.Text),
            "XML0002"
        );
    }

    private static IEnumerable<Func<(string, bool, Action<XmlWriter, XmlNameTable>, string)>> Markup()
    {
        yield return Row("comment with double hyphen", false, (w, _) => w.Comment("a--b"), "XML0014");
        yield return Row("comment ending with hyphen", false, (w, _) => w.Comment("a-"), "XML0014");
        yield return Row("comment with control character", false, (w, _) => w.Comment("\u0001"), "XML0002");
        yield return Row(
            "processing instruction data with terminator",
            false,
            (w, t) => w.ProcessingInstruction(t.Intern("p"), "a?>b"),
            "XML0011"
        );
        yield return Row(
            "processing instruction data with control character",
            false,
            (w, t) => w.ProcessingInstruction(t.Intern("p"), "\u0001"),
            "XML0002"
        );
        yield return Row(
            "reserved target xml",
            false,
            (w, t) => w.ProcessingInstruction(t.Intern("xml"), ""),
            "XML0015"
        );
        yield return Row(
            "reserved target XmL",
            false,
            (w, t) => w.ProcessingInstruction(t.Intern("XmL"), ""),
            "XML0015"
        );
        yield return Row(
            "target starting with a digit",
            false,
            (w, t) => w.ProcessingInstruction(t.Intern("1x"), ""),
            "XML3004"
        );
        yield return Row("target with colon", false, (w, t) => w.ProcessingInstruction(t.Intern("a:b"), ""), "XML3004");
        yield return Row("end tag of another element", false, (w, t) => w.EndElement(N(t, "x")), "XML0005");
    }

    [Test]
    [MethodDataSource(nameof(Rows))]
    public async Task InvalidEvent_IsRejectedWithItsCode_AndWritesNothing(
        string name,
        bool openTag,
        Action<XmlWriter, XmlNameTable> act,
        string alias
    )
    {
        ArgumentNullException.ThrowIfNull(act);
        var table = NewTable();
        var output = new RewindableCharWriter();
        using var writer = new XmlWriter(output, table);
        writer.StartElement(N(table, "r"), default);
        if (!openTag)
        {
            writer.EndAttributes();
        }

        var before = output.Count;
        var failure = Assert.Throws<XmlWriteException>(() => act(writer, table));

        await Assert.That(failure.Code!.Alias).IsEqualTo(alias).Because(name);
        await Assert.That(output.Count).IsEqualTo(before).Because(name);
    }

    [Test]
    public async Task DuplicateAttribute_IsRejected_ByQualifiedAndExpandedName()
    {
        var table = NewTable();
        var output = new RewindableCharWriter();
        using var writer = new XmlWriter(output, table);
        writer.StartElement(N(table, "r"), default);
        writer.Attribute(N(table, "a"), "1");
        writer.Attribute(N(table, "b", "p", "urn:x"), "1");

        var plain = Assert.Throws<XmlWriteException>(() => writer.Attribute(N(table, "a"), "2"));
        var expanded = Assert.Throws<XmlWriteException>(() => writer.Attribute(N(table, "b", "q", "urn:x"), "2"));

        await Assert.That(plain.Code).IsSameReferenceAs(XmlErrorCodes.XML0006);
        await Assert.That(expanded.Code).IsSameReferenceAs(XmlErrorCodes.XML3003);
    }

    [Test]
    public async Task DocumentStructure_IsEnforced()
    {
        var table = NewTable();
        using var writer = new XmlWriter(new RewindableCharWriter(), table);

        var noRoot = Assert.Throws<XmlWriteException>(writer.EndDocument);
        var textOutside = Assert.Throws<XmlWriteException>(() => writer.Text("x", XmlTextKind.Text));
        var badVersion = Assert.Throws<XmlWriteException>(() =>
            writer.StartDocument(new XmlDeclaration("1.1", "", null))
        );
        writer.StartElement(N(table, "r"), default);
        writer.EndAttributes();
        var unclosed = Assert.Throws<XmlWriteException>(writer.EndDocument);
        writer.EndElement(N(table, "r"));
        var secondRoot = Assert.Throws<XmlWriteException>(() => writer.StartElement(N(table, "s"), default));

        await Assert.That(noRoot.Code).IsSameReferenceAs(XmlErrorCodes.XML0012);
        await Assert.That(textOutside.Code).IsSameReferenceAs(XmlErrorCodes.XML0018);
        await Assert.That(badVersion.Code).IsSameReferenceAs(XmlErrorCodes.XML0010);
        await Assert.That(unclosed.Code).IsSameReferenceAs(XmlErrorCodes.XML0001);
        await Assert.That(secondRoot.Code).IsSameReferenceAs(XmlErrorCodes.XML0012);
    }

    [Test]
    public async Task FrozenNameTable_RejectsAFreshPrefix_WithItsCode()
    {
        var table = NewTable();
        var names = new[] { N(table, "a", "p", "urn:x"), N(table, "b", "p", "urn:y") };
        table.Freeze();
        using var writer = new XmlWriter(new RewindableCharWriter(), table);
        writer.StartElement(names[0], default);
        writer.EndAttributes();
        writer.StartElement(names[1], default);

        var failure = Assert.Throws<XmlWriteException>(writer.EndAttributes);

        await Assert.That(failure.Code).IsSameReferenceAs(XmlErrorCodes.XML1116);
    }
}
