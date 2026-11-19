namespace NetEvolve.Schemas.Xml.Input.Decoding;

using System;

/// <summary>
/// Reads one code point from the start of a byte span. Strict: invalid input is never replaced. A scalar value that is
/// not a <c>Char</c> (production 2) is valid in the encoding and is returned; the parser reports it as <c>XML0002</c>, as
/// it does for characters (#712).
/// </summary>
internal static class XmlCodePointReader
{
    /// <summary>The outcome of one read.</summary>
    internal enum ReadStatus
    {
        /// <summary>A code point was read.</summary>
        Ok,

        /// <summary>The span ends inside a sequence that is valid so far.</summary>
        NeedMoreData,

        /// <summary>The sequence is malformed in the encoding, or its value is not a Unicode scalar value.</summary>
        Invalid,
    }

    /// <summary>Reads one code point.</summary>
    /// <param name="encoding">The encoding of the bytes.</param>
    /// <param name="source">The bytes, starting at the first byte of a sequence.</param>
    /// <param name="consumed">The number of bytes of the sequence when the status is <see cref="ReadStatus.Ok"/>.</param>
    /// <param name="codePoint">The code point when the status is <see cref="ReadStatus.Ok"/>.</param>
    /// <returns>The status. A malformed sequence is reported as soon as its prefix proves it, never later.</returns>
    internal static ReadStatus Read(
        XmlInputEncoding encoding,
        ReadOnlySpan<byte> source,
        out int consumed,
        out int codePoint
    )
    {
        consumed = 0;
        codePoint = 0;
        if (source.Length == 0)
        {
            return ReadStatus.NeedMoreData;
        }

        return encoding switch
        {
            XmlInputEncoding.Utf8 => ReadUtf8(source, out consumed, out codePoint),
            XmlInputEncoding.Utf16LittleEndian => ReadUtf16(source, true, out consumed, out codePoint),
            XmlInputEncoding.Utf16BigEndian => ReadUtf16(source, false, out consumed, out codePoint),
            XmlInputEncoding.Ucs4Order1234 => ReadUcs4(source, false, out consumed, out codePoint),
            XmlInputEncoding.Ucs4Order4321 => ReadUcs4(source, true, out consumed, out codePoint),
            XmlInputEncoding.UsAscii => ReadSingleByte(source, 0x7F, out consumed, out codePoint),
            _ => ReadSingleByte(source, 0xFF, out consumed, out codePoint),
        };
    }

    private static ReadStatus ReadSingleByte(ReadOnlySpan<byte> source, int max, out int consumed, out int codePoint)
    {
        consumed = 1;
        codePoint = source[0];
        return codePoint <= max ? ReadStatus.Ok : ReadStatus.Invalid;
    }

    private static ReadStatus ReadUtf8(ReadOnlySpan<byte> source, out int consumed, out int codePoint)
    {
        consumed = 1;
        codePoint = source[0];
        if (codePoint < 0x80)
        {
            return ReadStatus.Ok;
        }

        // C0 and C1 are overlong leads, F5 and above exceed U+10FFFF, 80 to BF are continuation bytes.
        if (codePoint is < 0xC2 or > 0xF4)
        {
            return ReadStatus.Invalid;
        }

        var length =
            codePoint < 0xE0 ? 2
            : codePoint < 0xF0 ? 3
            : 4;

        // The second byte alone excludes overlong forms, encoded surrogates, and values above U+10FFFF.
        var lowest = codePoint switch
        {
            0xE0 => 0xA0,
            0xF0 => 0x90,
            _ => 0x80,
        };
        var highest = codePoint switch
        {
            0xED => 0x9F,
            0xF4 => 0x8F,
            _ => 0xBF,
        };

        var value = codePoint & (0x3F >> (length - 1));
        for (var i = 1; i < length; i++)
        {
            if (i >= source.Length)
            {
                return ReadStatus.NeedMoreData;
            }

            var next = source[i];
            if (next < lowest || next > highest)
            {
                return ReadStatus.Invalid;
            }

            value = (value << 6) | (next & 0x3F);
            lowest = 0x80;
            highest = 0xBF;
        }

        consumed = length;
        codePoint = value;
        return ReadStatus.Ok;
    }

    private static ReadStatus ReadUtf16(
        ReadOnlySpan<byte> source,
        bool littleEndian,
        out int consumed,
        out int codePoint
    )
    {
        consumed = 2;
        codePoint = 0;
        if (source.Length < 2)
        {
            return ReadStatus.NeedMoreData;
        }

        var unit = Unit16(source, littleEndian);
        if (unit is < 0xD800 or > 0xDFFF)
        {
            codePoint = unit;
            return ReadStatus.Ok;
        }

        if (unit >= 0xDC00)
        {
            return ReadStatus.Invalid;
        }

        if (source.Length < 4)
        {
            return ReadStatus.NeedMoreData;
        }

        var low = Unit16(source.Slice(2), littleEndian);
        if (low is < 0xDC00 or > 0xDFFF)
        {
            return ReadStatus.Invalid;
        }

        consumed = 4;
        codePoint = 0x10000 + ((unit - 0xD800) << 10) + (low - 0xDC00);
        return ReadStatus.Ok;
    }

    private static int Unit16(ReadOnlySpan<byte> source, bool littleEndian) =>
        littleEndian ? source[0] | (source[1] << 8) : (source[0] << 8) | source[1];

    private static ReadStatus ReadUcs4(
        ReadOnlySpan<byte> source,
        bool littleEndian,
        out int consumed,
        out int codePoint
    )
    {
        consumed = 4;
        codePoint = 0;
        if (source.Length < 4)
        {
            return ReadStatus.NeedMoreData;
        }

        var value = littleEndian
            ? (uint)(source[0] | (source[1] << 8) | (source[2] << 16)) | ((uint)source[3] << 24)
            : ((uint)source[0] << 24) | (uint)((source[1] << 16) | (source[2] << 8) | source[3]);
        if (value > 0x10FFFF || value is >= 0xD800 and <= 0xDFFF)
        {
            return ReadStatus.Invalid;
        }

        codePoint = (int)value;
        return ReadStatus.Ok;
    }
}
