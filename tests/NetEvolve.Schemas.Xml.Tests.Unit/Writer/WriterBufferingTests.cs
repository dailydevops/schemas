namespace NetEvolve.Schemas.Xml.Tests.Unit.Writer;

using System;
using System.Buffers;
using System.Text;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Writer;
using static NetEvolve.Schemas.Xml.Tests.Unit.Writer.WriterHarness;

public class WriterBufferingTests
{
    // Longer than the adapter scratch buffer, with specials, non-ASCII and supplementary characters at every offset.
    private static string LongText()
    {
        var sb = new StringBuilder();
        for (var i = 0; i < 700; i++)
        {
            _ = sb.Append("ab&<>c\u00E9\u20AC\U0001F600\r\n\t\"M\u0100z");
        }

        return sb.ToString();
    }

    private static void Script(XmlWriter w, string text)
    {
        w.Start("a", attrs: [new Att("v", text)]);
        w.Text(text, XmlTextKind.Text);
        w.Text(text, XmlTextKind.CData);
        w.End("a");
        w.EndDocument();
    }

    [Test]
    public async Task Bytes_Utf8_EqualTheEncodedCharacterOutput_ForTextLongerThanTheScratchBuffer()
    {
        var text = LongText();
        var chars = Chars((w, _) => Script(w, text));
        var bytes = Bytes((w, _) => Script(w, text));

        await Assert.That(bytes).IsEquivalentTo(Encoding.UTF8.GetBytes(chars));
    }

    [Test]
    public async Task Bytes_Utf16_EqualTheEncodedCharacterOutput_ForTextLongerThanTheScratchBuffer()
    {
        var text = LongText();
        var chars = Chars((w, _) => Script(w, text));
        var bytes = Bytes((w, _) => Script(w, text), encoding: XmlWriterEncoding.Utf16);

        await Assert.That(bytes).IsEquivalentTo(Encoding.Unicode.GetBytes(chars));
    }

    [Test]
    public async Task Bytes_AreVisibleInTheDestination_AfterEachEvent()
    {
        var table = NewTable();
        var buffer = new ArrayBufferWriter<byte>();
        using var writer = new XmlWriter(buffer, table);

        writer.Start("a");
        await Assert.That(Encoding.UTF8.GetString(buffer.WrittenSpan)).IsEqualTo("<a");
        writer.Start("b");
        await Assert.That(Encoding.UTF8.GetString(buffer.WrittenSpan)).IsEqualTo("<a><b");
        writer.Text("t", XmlTextKind.Text);
        await Assert.That(Encoding.UTF8.GetString(buffer.WrittenSpan)).IsEqualTo("<a><b>t");
        writer.Comment("c");
        await Assert.That(Encoding.UTF8.GetString(buffer.WrittenSpan)).IsEqualTo("<a><b>t<!--c-->");
        writer.ProcessingInstruction(table.Intern("p"), "d");
        await Assert.That(Encoding.UTF8.GetString(buffer.WrittenSpan)).IsEqualTo("<a><b>t<!--c--><?p d?>");
        writer.End("b");
        await Assert.That(Encoding.UTF8.GetString(buffer.WrittenSpan)).IsEqualTo("<a><b>t<!--c--><?p d?></b>");
    }

    [Test]
    public async Task Names_AreValidatedOnEveryUse_AlsoWhenSymbolsAreNotCached()
    {
        var table = NewTable();
        var buffer = new ArrayBufferWriter<char>();
        using var writer = new XmlWriter(buffer, table);
        for (var i = 0; i < 5000; i++)
        {
            _ = table.Intern("n" + i);
        }

        var late = N(table, "n4999", "", "urn:late");
        var bad = N(table, "1bad");
        writer.Start("r");
        for (var round = 0; round < 2; round++)
        {
            writer.StartElement(late, default);
            writer.Attribute(N(table, "n4999"), "x");
            writer.EndAttributes();
            writer.EndElement(late);
        }

        writer.End("r");
        writer.EndDocument();

        await Assert
            .That(buffer.WrittenSpan.ToString())
            .IsEqualTo("<r><n4999 xmlns=\"urn:late\" n4999=\"x\"/><n4999 xmlns=\"urn:late\" n4999=\"x\"/></r>");

        using var second = new XmlWriter(new ArrayBufferWriter<char>(), table);
        for (var round = 0; round < 3; round++)
        {
            await Assert.That(() => second.StartElement(bad, default)).Throws<XmlWriteException>();
        }
    }

    [Test]
    public async Task CharToByteWriter_WriteCharacters_FlushesWhenTheScratchBufferIsFull()
    {
        var bytes = new ArrayBufferWriter<byte>();
        var adapter = new CharToByteWriter(bytes, utf16: false);
        for (var i = 0; i < 2500; i++)
        {
            adapter.Write('x');
        }

        adapter.Write(string.Empty.AsSpan());
        adapter.Flush();
        adapter.Flush();

        await Assert.That(bytes.WrittenCount).IsEqualTo(2500);
    }
}
