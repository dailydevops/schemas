namespace NetEvolve.Schemas.Xml.Tests.Unit.Writer;

using System;
using System.Buffers;
using System.IO;
using System.Text;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Writer;
using static NetEvolve.Schemas.Xml.Tests.Unit.Writer.WriterHarness;

public class WriterEdgeTests
{
    [Test]
    public async Task Constructors_RejectNullArguments()
    {
        var table = NewTable();

        await Assert.That(() => new XmlWriter((IBufferWriter<char>)null!, table)).Throws<ArgumentNullException>();
        await Assert.That(() => new XmlWriter((IBufferWriter<byte>)null!, table)).Throws<ArgumentNullException>();
        await Assert.That(() => new XmlWriter((Stream)null!, table)).Throws<ArgumentNullException>();
        await Assert.That(() => new XmlWriter(new ArrayBufferWriter<char>(), null!)).Throws<ArgumentNullException>();
    }

    [Test]
    public async Task Exception_Constructors_CarryCodeMessageAndCause()
    {
        var inner = new InvalidOperationException();

        await Assert.That(new XmlWriteException().Code).IsNull();
        await Assert.That(new XmlWriteException("m").Message).IsEqualTo("m");
        await Assert.That(new XmlWriteException("m", inner).InnerException).IsSameReferenceAs(inner);
        await Assert
            .That(new XmlWriteException(XmlErrorCodes.XML0002).Message)
            .IsEqualTo(XmlErrorCodes.XML0002.Message);
        await Assert.That(() => new XmlWriteException((XmlErrorCode)null!)).Throws<ArgumentNullException>();
    }

    [Test]
    public async Task Options_DefaultAndExplicit_ExposeTheirValues()
    {
        var explicitOptions = new XmlWriterOptions(true, XmlWriterEncoding.Utf16, true);

        await Assert.That(XmlWriterOptions.Default.Canonical).IsFalse();
        await Assert.That(XmlWriterOptions.Default.Encoding).IsEqualTo(XmlWriterEncoding.Utf8);
        await Assert.That(XmlWriterOptions.Default.OmitXmlDeclaration).IsFalse();
        await Assert.That(explicitOptions.Canonical).IsTrue();
        await Assert.That(explicitOptions.Encoding).IsEqualTo(XmlWriterEncoding.Utf16);
        await Assert.That(explicitOptions.OmitXmlDeclaration).IsTrue();
    }

    [Test]
    public async Task OmitXmlDeclaration_LeavesTheDeclarationOut_AndStandaloneNoIsWritten()
    {
        var table = NewTable();
        var plain = new ArrayBufferWriter<char>();
        using (var writer = new XmlWriter(plain, table, new XmlWriterOptions(omitXmlDeclaration: true)))
        {
            writer.StartDocument(new XmlDeclaration("1.0", "", false));
            writer.Start("a");
            writer.End("a");
        }

        var kept = Chars(
            (w, _) =>
            {
                w.StartDocument(new XmlDeclaration("1.0", "", false));
                w.Start("a");
                w.End("a");
            }
        );

        await Assert.That(plain.WrittenSpan.ToString()).IsEqualTo("<a/>");
        await Assert.That(kept).IsEqualTo("<?xml version=\"1.0\" standalone=\"no\"?><a/>");
    }

    [Test]
    public async Task Doctype_SystemOnlyAndQuoting_AreWritten()
    {
        var xml = Chars(
            (w, _) =>
            {
                w.Doctype(new XmlDoctypeInfo("a", "", "a\"b", ""));
                w.Doctype(new XmlDoctypeInfo("a", "", "", ""));
                w.Start("a");
                w.End("a");
            }
        );

        await Assert.That(xml).IsEqualTo("<!DOCTYPE a SYSTEM 'a\"b'><!DOCTYPE a><a/>");
    }

    [Test]
    public async Task Doctype_Invalid_IsRejected()
    {
        var table = NewTable();
        using var writer = new XmlWriter(new ArrayBufferWriter<char>(), table);

        var name = Assert.Throws<XmlWriteException>(() => writer.Doctype(new XmlDoctypeInfo("1a", "", "", "")));
        var quotes = Assert.Throws<XmlWriteException>(() => writer.Doctype(new XmlDoctypeInfo("a", "", "'\"", "")));
        var chars = Assert.Throws<XmlWriteException>(() => writer.Doctype(new XmlDoctypeInfo("a", "", "", "\u0001")));
        writer.Start("a");
        var late = Assert.Throws<XmlWriteException>(() => writer.Doctype(new XmlDoctypeInfo("a", "", "", "")));

        await Assert.That(name.Code).IsSameReferenceAs(XmlErrorCodes.XML3004);
        await Assert.That(quotes.Code).IsSameReferenceAs(XmlErrorCodes.XML0011);
        await Assert.That(chars.Code).IsSameReferenceAs(XmlErrorCodes.XML0002);
        await Assert.That(late.Code).IsSameReferenceAs(XmlErrorCodes.XML0011);
    }

    [Test]
    public async Task NamespaceDeclaration_ReservedOrRepeated_IsRejected()
    {
        var table = NewTable();
        using var writer = new XmlWriter(new ArrayBufferWriter<char>(), table);
        writer.StartElement(N(table, "r"), default);
        writer.NamespaceDeclaration(table.Intern("p"), table.Intern("urn:x"));

        var xmlns = Assert.Throws<XmlWriteException>(() =>
            writer.NamespaceDeclaration(table.Intern("xmlns"), table.Intern("urn:x"))
        );
        var xml = Assert.Throws<XmlWriteException>(() =>
            writer.NamespaceDeclaration(table.Intern("xml"), table.Intern("urn:x"))
        );
        var invalid = Assert.Throws<XmlWriteException>(() =>
            writer.NamespaceDeclaration(table.Intern("1"), table.Intern("urn:x"))
        );
        var twice = Assert.Throws<XmlWriteException>(() =>
            writer.NamespaceDeclaration(table.Intern("p"), table.Intern("urn:y"))
        );

        await Assert.That(xmlns.Code).IsSameReferenceAs(XmlErrorCodes.XML3002);
        await Assert.That(xml.Code).IsSameReferenceAs(XmlErrorCodes.XML3002);
        await Assert.That(invalid.Code).IsSameReferenceAs(XmlErrorCodes.XML3004);
        await Assert.That(twice.Code).IsSameReferenceAs(XmlErrorCodes.XML0006);
    }

    [Test]
    public async Task EventOutsideItsState_ThrowsInvalidOperation()
    {
        var table = NewTable();
        using var writer = new XmlWriter(new ArrayBufferWriter<char>(), table);

        await Assert.That(() => writer.EndAttributes()).Throws<InvalidOperationException>();
        await Assert.That(() => writer.Attribute(N(table, "a"), "v")).Throws<InvalidOperationException>();
        writer.StartElement(N(table, "r"), default);
        await Assert.That(() => writer.Comment("c")).Throws<InvalidOperationException>();
        await Assert.That(() => writer.StartElement(N(table, "s"), default)).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task DefaultNamespaceConflict_IsRedeclaredOnTheChild_AndFreshWhenDeclaredOnTheSameElement()
    {
        var redeclared = Chars(
            (w, _) =>
            {
                w.Start("a", "", "urn:x");
                w.Start("b", "", "urn:y");
                w.End("b", "", "urn:y");
                w.End("a", "", "urn:x");
            }
        );
        var fresh = Chars(
            (w, _) =>
            {
                w.Start("a", "", "urn:x", decls: [("", "urn:y")]);
                w.End("a", "", "urn:x");
            }
        );

        await Assert.That(redeclared).IsEqualTo("<a xmlns=\"urn:x\"><b xmlns=\"urn:y\"/></a>");
        await Assert.That(fresh).IsEqualTo("<ns1:a xmlns=\"urn:y\" xmlns:ns1=\"urn:x\"/>");
    }

    [Test]
    public async Task CharToByteWriter_LoneSurrogatesAndSplitPairs_AreEncodedWithReplacement()
    {
        var bytes = new ArrayBufferWriter<byte>();
        var adapter = new CharToByteWriter(bytes, utf16: false);

        Write(adapter, "a\uD83D");
        Write(adapter, "\uDE00");
        Write(adapter, "\uD83D");
        Write(adapter, "b");
        Write(adapter, "\uDC00");

        await Assert.That(Encoding.UTF8.GetString(bytes.WrittenSpan)).IsEqualTo("a\U0001F600�b�");
        await Assert.That(adapter.GetMemory().Length).IsGreaterThan(0);
        adapter.Advance(0);
    }

    private static void Write(CharToByteWriter adapter, string text)
    {
        text.AsSpan().CopyTo(adapter.GetSpan());
        adapter.Advance(text.Length);
    }
}
