namespace NetEvolve.Schemas.Xml.Input.Decoding;

using System;

/// <summary>Detects the encoding family from the first bytes, one branch per row of XML 1.0 (Fifth Edition) Appendix F.</summary>
internal static class XmlEncodingDetector
{
    private const int WindowLength = 4;

    /// <summary>Detects the encoding from the first bytes.</summary>
    /// <param name="head">The first bytes of the input; four are enough.</param>
    /// <param name="isFinal">Whether <paramref name="head"/> is the whole input.</param>
    /// <param name="detection">The detection result when the method returns <see langword="true"/>.</param>
    /// <returns><see langword="false"/> when fewer than four bytes are available and more may follow.</returns>
    internal static bool TryDetect(ReadOnlySpan<byte> head, bool isFinal, out XmlEncodingDetection detection)
    {
        if (head.Length < WindowLength && !isFinal)
        {
            detection = default;
            return false;
        }

        detection = head.Length < WindowLength ? DetectShort(head) : Detect(head);
        return true;
    }

    // Rows with a byte order mark come first, then the rows without; "##" is any byte.
    private static XmlEncodingDetection Detect(ReadOnlySpan<byte> head)
    {
        var quad = unchecked((uint)((head[0] << 24) | (head[1] << 16) | (head[2] << 8) | head[3]));
        switch (quad)
        {
            case 0x0000FEFF:
                return new(XmlInputEncoding.Ucs4Order1234, 4);
            case 0xFFFE0000:
                return new(XmlInputEncoding.Ucs4Order4321, 4);
            case 0x0000FFFE:
                return new(XmlInputEncoding.Ucs4Order2143, 4);
            case 0xFEFF0000:
                return new(XmlInputEncoding.Ucs4Order3412, 4);
            case 0x0000003C:
                return new(XmlInputEncoding.Ucs4Order1234, 0);
            case 0x3C000000:
                return new(XmlInputEncoding.Ucs4Order4321, 0);
            case 0x00003C00:
                return new(XmlInputEncoding.Ucs4Order2143, 0);
            case 0x003C0000:
                return new(XmlInputEncoding.Ucs4Order3412, 0);
            case 0x003C003F:
                return new(XmlInputEncoding.Utf16BigEndian, 0);
            case 0x3C003F00:
                return new(XmlInputEncoding.Utf16LittleEndian, 0);
            case 0x4C6FA794:
                return new(XmlInputEncoding.Ebcdic, 0);
            default:
                return DetectShort(head);
        }
    }

    // Byte order marks of UTF-16 and UTF-8; every other start is the ASCII-compatible row or the UTF-8 default.
    private static XmlEncodingDetection DetectShort(ReadOnlySpan<byte> head)
    {
        if (head.Length >= 2 && head[0] == 0xFE && head[1] == 0xFF)
        {
            return new(XmlInputEncoding.Utf16BigEndian, 2);
        }

        if (head.Length >= 2 && head[0] == 0xFF && head[1] == 0xFE)
        {
            return new(XmlInputEncoding.Utf16LittleEndian, 2);
        }

        return head.Length >= 3 && head[0] == 0xEF && head[1] == 0xBB && head[2] == 0xBF
            ? new(XmlInputEncoding.Utf8, 3)
            : new(XmlInputEncoding.Utf8, 0);
    }
}
