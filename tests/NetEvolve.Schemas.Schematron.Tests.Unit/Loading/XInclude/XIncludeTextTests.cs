namespace NetEvolve.Schemas.Schematron.Tests.Unit.Loading.XInclude;

using System.Text;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Loading.XInclude.XIncludeFixture;

public class XIncludeTextTests
{
    private static Outcome RunText(byte[] bytes, string? encoding, string fallback = "")
    {
        var attributes = " parse=\"text\"" + (encoding is null ? string.Empty : $" encoding=\"{encoding}\"");
        return Run(Doc(Inc("t.txt", attributes, fallback)), new ByteResources().Add("file:///a/t.txt", bytes));
    }

    private static byte[] WithBom(byte[] bom, byte[] content)
    {
        var all = new byte[bom.Length + content.Length];
        bom.CopyTo(all, 0);
        content.CopyTo(all, bom.Length);
        return all;
    }

    [Test]
    public async Task Utf8_IsTheDefault_AndABomIsRemoved()
    {
        var plain = RunText(Encoding.UTF8.GetBytes("hé"), null);
        var bom = RunText(WithBom([0xEF, 0xBB, 0xBF], Encoding.UTF8.GetBytes("hé")), null);

        await Assert.That(plain.Text).IsEqualTo("<root>hé</root>");
        await Assert.That(bom.Text).IsEqualTo("<root>hé</root>");
    }

    [Test]
    public async Task Utf16Bom_SelectsTheByteOrder_WithoutAnEncodingAttribute()
    {
        var little = RunText(WithBom([0xFF, 0xFE], Encoding.Unicode.GetBytes("ab")), null);
        var big = RunText(WithBom([0xFE, 0xFF], Encoding.BigEndianUnicode.GetBytes("ab")), null);

        await Assert.That(little.Text).IsEqualTo("<root>ab</root>");
        await Assert.That(big.Text).IsEqualTo("<root>ab</root>");
    }

    [Test]
    public async Task EncodingAttribute_Utf8_RemovesABom()
    {
        var outcome = RunText(WithBom([0xEF, 0xBB, 0xBF], Encoding.UTF8.GetBytes("x")), "UTF-8");

        await Assert.That(outcome.Text).IsEqualTo("<root>x</root>");
    }

    [Test]
    public async Task EncodingAttribute_Utf16_UsesTheBom_AndDefaultsToBigEndian()
    {
        var little = RunText(WithBom([0xFF, 0xFE], Encoding.Unicode.GetBytes("ab")), "utf-16");
        var big = RunText(WithBom([0xFE, 0xFF], Encoding.BigEndianUnicode.GetBytes("ab")), "utf-16");
        var none = RunText(Encoding.BigEndianUnicode.GetBytes("ab"), "utf-16");

        await Assert.That(little.Text).IsEqualTo("<root>ab</root>");
        await Assert.That(big.Text).IsEqualTo("<root>ab</root>");
        await Assert.That(none.Text).IsEqualTo("<root>ab</root>");
    }

    [Test]
    public async Task EncodingAttribute_Utf16Explicit_IgnoresNoBom()
    {
        var little = RunText(Encoding.Unicode.GetBytes("ab"), "utf-16le");
        var big = RunText(Encoding.BigEndianUnicode.GetBytes("ab"), "UTF-16BE");

        await Assert.That(little.Text).IsEqualTo("<root>ab</root>");
        await Assert.That(big.Text).IsEqualTo("<root>ab</root>");
    }

    [Test]
    public async Task EncodingAttribute_Latin1AndAscii_Decode()
    {
        var latin = RunText([0x68, 0xE9], "iso-8859-1");
        var ascii = RunText([0x68, 0x69], "us-ascii");

        await Assert.That(latin.Text).IsEqualTo("<root>hé</root>");
        await Assert.That(ascii.Text).IsEqualTo("<root>hi</root>");
    }

    [Test]
    public async Task SurrogatePair_IsAccepted()
    {
        var outcome = RunText(Encoding.UTF8.GetBytes("\U0001F600"), null);

        await Assert.That(outcome.Ok).IsTrue();
        await Assert.That(outcome.Text).IsEqualTo("<root>\U0001F600</root>");
    }

    [Test]
    public async Task TabsAndLineBreaks_AreAccepted()
    {
        var outcome = RunText(Encoding.UTF8.GetBytes("a\tb\r\nc\n"), null);

        await Assert.That(outcome.Ok).IsTrue();
    }

    [Test]
    [Arguments(new byte[] { 0xFF, 0xFE, 0xFD }, null)]
    [Arguments(new byte[] { 0x61, 0x80 }, "us-ascii")]
    [Arguments(new byte[] { 0x61 }, "utf-16le")]
    public async Task UndecodableBytes_ReportModuleInvalid(byte[] bytes, string? encoding)
    {
        var outcome = RunText(bytes, encoding);

        await Assert.That(outcome.Ok).IsFalse();
        await Assert.That(outcome.Problems.Count).IsEqualTo(1);
        await Assert.That(outcome.Problems[0].Code).IsEqualTo(SchematronDiagnosticCodes.ModuleInvalid);
    }

    [Test]
    [Arguments(new byte[] { 0x61, 0x01 })]
    [Arguments(new byte[] { 0x61, 0xEF, 0xBF, 0xBF })]
    [Arguments(new byte[] { 0x61, 0xEF, 0xBF, 0xBE })]
    public async Task CharactersNotAllowedInXml_ReportModuleInvalid(byte[] bytes)
    {
        var outcome = RunText(bytes, "utf-8");

        await Assert.That(outcome.Problems.Count).IsEqualTo(1);
        await Assert.That(outcome.Problems[0].Code).IsEqualTo(SchematronDiagnosticCodes.ModuleInvalid);
    }

    [Test]
    public async Task UnsupportedEncoding_ReportsUnsupported()
    {
        var outcome = RunText([0x61], "shift_jis");

        await Assert.That(outcome.Ok).IsFalse();
        await Assert.That(outcome.Problems[0].Code).IsEqualTo(SchematronDiagnosticCodes.Unsupported);
    }

    [Test]
    public async Task UndecodableOrUnsupportedText_UsesTheFallback()
    {
        var bad = RunText([0xFF, 0xFE, 0xFD], null, Fallback("<f/>"));
        var unknown = RunText([0x61], "shift_jis", Fallback("<f/>"));

        await Assert.That(bad.Ok).IsTrue();
        await Assert.That(bad.Text).IsEqualTo("<root><f></f></root>");
        await Assert.That(unknown.Text).IsEqualTo("<root><f></f></root>");
    }
}
