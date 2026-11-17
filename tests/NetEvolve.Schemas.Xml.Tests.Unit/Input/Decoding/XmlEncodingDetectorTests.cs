namespace NetEvolve.Schemas.Xml.Tests.Unit.Input.Decoding;

using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Input.Decoding;
using static DecodingTestHelper;

public class XmlEncodingDetectorTests
{
    // One case per row of XML 1.0 (Fifth Edition) Appendix F.
    [Test]
    [Arguments("00 00 FE FF", XmlInputEncoding.Ucs4Order1234, 4)]
    [Arguments("FF FE 00 00", XmlInputEncoding.Ucs4Order4321, 4)]
    [Arguments("00 00 FF FE", XmlInputEncoding.Ucs4Order2143, 4)]
    [Arguments("FE FF 00 00", XmlInputEncoding.Ucs4Order3412, 4)]
    [Arguments("FE FF 00 3C", XmlInputEncoding.Utf16BigEndian, 2)]
    [Arguments("FF FE 3C 00", XmlInputEncoding.Utf16LittleEndian, 2)]
    [Arguments("EF BB BF 3C", XmlInputEncoding.Utf8, 3)]
    [Arguments("00 00 00 3C", XmlInputEncoding.Ucs4Order1234, 0)]
    [Arguments("3C 00 00 00", XmlInputEncoding.Ucs4Order4321, 0)]
    [Arguments("00 00 3C 00", XmlInputEncoding.Ucs4Order2143, 0)]
    [Arguments("00 3C 00 00", XmlInputEncoding.Ucs4Order3412, 0)]
    [Arguments("00 3C 00 3F", XmlInputEncoding.Utf16BigEndian, 0)]
    [Arguments("3C 00 3F 00", XmlInputEncoding.Utf16LittleEndian, 0)]
    [Arguments("3C 3F 78 6D", XmlInputEncoding.Utf8, 0)]
    [Arguments("4C 6F A7 94", XmlInputEncoding.Ebcdic, 0)]
    [Arguments("61 62 63 64", XmlInputEncoding.Utf8, 0)]
    public async Task Detect_AppendixFRow_YieldsFamilyAndBomLength(string hex, XmlInputEncoding expected, int bomLength)
    {
        var detection = Detect(Hex(hex));

        await Assert.That(detection.Encoding).IsEqualTo(expected);
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
    [Arguments("", XmlInputEncoding.Utf8, 0)]
    [Arguments("3C", XmlInputEncoding.Utf8, 0)]
    [Arguments("EF BB BF", XmlInputEncoding.Utf8, 3)]
    [Arguments("FF FE", XmlInputEncoding.Utf16LittleEndian, 2)]
    [Arguments("FE FF 00", XmlInputEncoding.Utf16BigEndian, 2)]
    public async Task Detect_ShortFinalInput_UsesAvailableBytes(string hex, XmlInputEncoding expected, int bomLength)
    {
        var detection = Detect(Hex(hex));

        await Assert.That(detection.Encoding).IsEqualTo(expected);
        await Assert.That(detection.BomLength).IsEqualTo(bomLength);
    }

    [Test]
    public async Task TryDetect_ShortNonFinalInput_NeedsMoreData()
    {
        var detected = XmlEncodingDetector.TryDetect(Hex("FF FE 00"), false, out _);

        await Assert.That(detected).IsFalse();
    }
}
