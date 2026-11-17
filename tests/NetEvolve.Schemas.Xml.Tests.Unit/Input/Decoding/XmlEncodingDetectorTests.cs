namespace NetEvolve.Schemas.Xml.Tests.Unit.Input.Decoding;

using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Input.Decoding;
using static DecodingTestHelper;

internal sealed class XmlEncodingDetectorTests
{
    // One case per row of XML 1.0 (Fifth Edition) Appendix F.
    [Test]
    [Arguments("00 00 FE FF", "Ucs4Order1234", 4)]
    [Arguments("FF FE 00 00", "Ucs4Order4321", 4)]
    [Arguments("00 00 FF FE", "Ucs4Order2143", 4)]
    [Arguments("FE FF 00 00", "Ucs4Order3412", 4)]
    [Arguments("FE FF 00 3C", "Utf16BigEndian", 2)]
    [Arguments("FF FE 3C 00", "Utf16LittleEndian", 2)]
    [Arguments("EF BB BF 3C", "Utf8", 3)]
    [Arguments("00 00 00 3C", "Ucs4Order1234", 0)]
    [Arguments("3C 00 00 00", "Ucs4Order4321", 0)]
    [Arguments("00 00 3C 00", "Ucs4Order2143", 0)]
    [Arguments("00 3C 00 00", "Ucs4Order3412", 0)]
    [Arguments("00 3C 00 3F", "Utf16BigEndian", 0)]
    [Arguments("3C 00 3F 00", "Utf16LittleEndian", 0)]
    [Arguments("3C 3F 78 6D", "Utf8", 0)]
    [Arguments("4C 6F A7 94", "Ebcdic", 0)]
    [Arguments("61 62 63 64", "Utf8", 0)]
    public async Task Detect_AppendixFRow_YieldsFamilyAndBomLength(string hex, string expected, int bomLength)
    {
        var detection = Detect(Hex(hex));

        await Assert.That(detection.Encoding.ToString()).IsEqualTo(expected);
        await Assert.That(detection.BomLength).IsEqualTo(bomLength);
        await Assert.That(detection.HasByteOrderMark).IsEqualTo(bomLength > 0);
    }

    [Test]
    [Arguments("00 00 FF FE")]
    [Arguments("FE FF 00 00")]
    [Arguments("00 00 3C 00")]
    [Arguments("00 3C 00 00")]
    [Arguments("4C 6F A7 94")]
    public async Task TryCreate_RowOfDeferredEncodings_YieldsUnsupportedEncodingCode(string hex)
    {
        var created = XmlInputDecoder.TryCreate(Detect(Hex(hex)), out var decoder, out var error);

        await Assert.That(created).IsFalse();
        await Assert.That(decoder).IsNull();
        await Assert.That(error.Code).IsEqualTo(XmlErrorCodes.XML4001);
        await Assert.That(error.ByteOffset).IsEqualTo(0L);
    }

    [Test]
    [Arguments("", "Utf8", 0)]
    [Arguments("3C", "Utf8", 0)]
    [Arguments("EF BB BF", "Utf8", 3)]
    [Arguments("FF FE", "Utf16LittleEndian", 2)]
    [Arguments("FE FF 00", "Utf16BigEndian", 2)]
    public async Task Detect_ShortFinalInput_UsesAvailableBytes(string hex, string expected, int bomLength)
    {
        var detection = Detect(Hex(hex));

        await Assert.That(detection.Encoding.ToString()).IsEqualTo(expected);
        await Assert.That(detection.BomLength).IsEqualTo(bomLength);
    }

    [Test]
    public async Task TryDetect_ShortNonFinalInput_NeedsMoreData()
    {
        var detected = XmlEncodingDetector.TryDetect(Hex("FF FE 00"), false, out _);

        await Assert.That(detected).IsFalse();
    }
}
