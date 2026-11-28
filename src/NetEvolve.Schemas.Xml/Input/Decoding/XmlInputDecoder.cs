namespace NetEvolve.Schemas.Xml.Input.Decoding;

using System;
using NetEvolve.Schemas.Xml.Buffers;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Text.CharClasses;

/// <summary>
/// Resumable, strict decoder from bytes to UTF-16 characters. A sequence split across two calls decodes like one
/// that is not; invalid input is a coded error with the byte offset and is never replaced.
/// </summary>
/// <remarks>
/// Not thread-safe. The byte order mark found by detection is consumed here, not by the caller, and its bytes count in
/// the offsets. Limitation: only UTF-8, UTF-16 and UCS-4 (octet orders 1234 and 4321), ISO-8859-1 and US-ASCII are
/// decoded; every other encoding is <c>XML4001</c> (DEF-62, DEF-64). Declared names are matched ASCII case-insensitively.
/// </remarks>
internal sealed class XmlInputDecoder
{
    private const int UnitLength = 4;

    private enum Declared
    {
        Unknown,
        Utf8,
        Latin1,
        Ascii,
        Utf16,
        Utf16Le,
        Utf16Be,
        Ucs4,
        Ucs4Be,
        Ucs4Le,
    }

    private readonly XmlEncodingDetection _detection;
    private readonly bool _reportNonChar;
    private readonly byte[] _carry = new byte[UnitLength - 1];
    private XmlInputEncoding _encoding;
    private int _carryCount;
    private int _skip;
    private long _fed;

    private XmlInputDecoder(XmlEncodingDetection detection, bool reportNonChar)
    {
        _detection = detection;
        _reportNonChar = reportNonChar;
        Restart(detection.Encoding);
    }

    /// <summary>Gets the encoding the decoder currently uses.</summary>
    internal XmlInputEncoding Encoding => _encoding;

    /// <summary>Creates a decoder for a detection result.</summary>
    /// <param name="detection">The detection result.</param>
    /// <param name="decoder">The decoder, or <see langword="null"/> when the encoding is not supported.</param>
    /// <param name="error"><c>XML4001</c> at offset zero when the encoding is detected but not supported.</param>
    /// <param name="reportNonChar">
    /// Whether a scalar value that is not a <c>Char</c> (production 2) is <c>XML0002</c> at its byte offset. Leave it off when
    /// the parser reads the characters, which reports it at the character, as for character input (#712); set it where no
    /// parser checks the characters, such as an external entity.
    /// </param>
    /// <returns><see langword="true"/> when a decoder was created.</returns>
    internal static bool TryCreate(
        XmlEncodingDetection detection,
        out XmlInputDecoder? decoder,
        out XmlDecodingError error,
        bool reportNonChar = false
    )
    {
        if (
            detection.Encoding
            is XmlInputEncoding.Ucs4Order2143
                or XmlInputEncoding.Ucs4Order3412
                or XmlInputEncoding.Ebcdic
        )
        {
            decoder = null;
            error = new XmlDecodingError(XmlErrorCodes.XML4001, 0);
            return false;
        }

        decoder = new XmlInputDecoder(detection, reportNonChar);
        error = default;
        return true;
    }

    /// <summary>Decodes the next chunk and appends the characters to <paramref name="output"/>.</summary>
    /// <param name="bytes">The next bytes of the input, from the first byte on.</param>
    /// <param name="isFinal">Whether this is the last chunk; an incomplete sequence is then an error.</param>
    /// <param name="output">The character buffer. On an error it holds the characters decoded before the offending sequence.</param>
    /// <param name="error">The error when the method returns <see langword="false"/>.</param>
    /// <returns><see langword="true"/> when the chunk decoded without error.</returns>
    internal bool TryDecode(
        ReadOnlySpan<byte> bytes,
        bool isFinal,
        PooledBuffer<char> output,
        out XmlDecodingError error
    )
    {
        ArgumentNullException.ThrowIfNull(output);

        error = default;
        var baseOffset = _fed;
        _fed += bytes.Length;

        // Never more chars than carried plus new bytes: one char per single byte, two chars per sequence of at least two bytes.
        output.EnsureCapacity(_carryCount + bytes.Length);
        var free = output.GetFreeSpan();
        var written = 0;
        var pos = Math.Min(_skip, bytes.Length);
        _skip -= pos;

        if (
            _carryCount > 0
            && pos < bytes.Length
            && !TryCompleteCarry(bytes, ref pos, baseOffset, free, ref written, out error)
        )
        {
            output.Advance(written);
            return false;
        }

        // Where a byte below 0x80 is one character (UTF-8, ISO-8859-1, US-ASCII) runs are widened at once; on .NET 8 and later the
        // runtime transcoders also take multi-byte runs. They stop in front of the first sequence they cannot take, so the code
        // point loop stays the only code that reports errors and fills the carry. With _reportNonChar the new characters are
        // checked, and on a hit the step is undone and the rest of the chunk is decoded one code point at a time.
        var bulk = _encoding is XmlInputEncoding.Utf8 or XmlInputEncoding.Iso88591 or XmlInputEncoding.UsAscii;
        while (pos < bytes.Length)
        {
            if (bulk)
            {
                var (read, wrote) = DecodeRun(_encoding, bytes.Slice(pos), free.Slice(written));
                if (_reportNonChar && XmlCharClass.IndexOfNonChar(free.Slice(written, wrote)) >= 0)
                {
                    bulk = false;
                    continue;
                }

                pos += read;
                written += wrote;
                if (pos >= bytes.Length)
                {
                    break;
                }
            }

            var status = XmlCodePointReader.Read(_encoding, bytes.Slice(pos), out var consumed, out var codePoint);
            if (status == XmlCodePointReader.ReadStatus.Ok && !IsReportedNonChar(codePoint))
            {
                written += Write(codePoint, free.Slice(written));
                pos += consumed;
            }
            else if (status == XmlCodePointReader.ReadStatus.NeedMoreData && !isFinal)
            {
                bytes.Slice(pos).CopyTo(_carry);
                _carryCount = bytes.Length - pos;
                break;
            }
            else
            {
                var code = status == XmlCodePointReader.ReadStatus.Ok ? XmlErrorCodes.XML0002 : XmlErrorCodes.XML4003;
                error = new XmlDecodingError(code, baseOffset + pos);
                output.Advance(written);
                return false;
            }
        }

        output.Advance(written);
        if (isFinal && _carryCount > 0)
        {
            error = new XmlDecodingError(XmlErrorCodes.XML4003, baseOffset + bytes.Length - _carryCount);
            return false;
        }

        return true;
    }

    // Decodes the leading well-formed run of the source into the destination, which holds at least as many characters as there
    // are bytes. Returns the bytes consumed and the characters written.
    private static (int Read, int Wrote) DecodeRun(
        XmlInputEncoding encoding,
        ReadOnlySpan<byte> source,
        Span<char> destination
    )
    {
#if NET8_0_OR_GREATER
        switch (encoding)
        {
            case XmlInputEncoding.Utf8:
                _ = System.Text.Unicode.Utf8.ToUtf16(
                    source,
                    destination,
                    out var read,
                    out var wrote,
                    replaceInvalidSequences: false,
                    isFinalBlock: false
                );
                return (read, wrote);
            case XmlInputEncoding.Iso88591:
                var n = System.Text.Encoding.Latin1.GetChars(source, destination);
                return (n, n);
        }

        _ = System.Text.Ascii.ToUtf16(source, destination, out var ascii);
        return (ascii, ascii);
#else
        var length = Math.Min(source.Length, destination.Length);
        var i = 0;
        while (i < length && source[i] < 0x80)
        {
            destination[i] = (char)source[i];
            i++;
        }

        return (i, i);
#endif
    }

    private bool IsReportedNonChar(int codePoint) => _reportNonChar && !XmlCharClass.IsChar(codePoint);

    // Completes the sequence left over from the previous chunk with the first new bytes.
    private bool TryCompleteCarry(
        ReadOnlySpan<byte> bytes,
        ref int pos,
        long baseOffset,
        Span<char> free,
        ref int written,
        out XmlDecodingError error
    )
    {
        error = default;
        Span<byte> unit = stackalloc byte[UnitLength];
        var carried = _carryCount;
        _carry.AsSpan(0, carried).CopyTo(unit);
        var take = Math.Min(UnitLength - carried, bytes.Length - pos);
        bytes.Slice(pos, take).CopyTo(unit.Slice(carried));

        // The carried bytes alone were incomplete, so a sequence that completes consumes at least all of them.
        var status = XmlCodePointReader.Read(
            _encoding,
            unit.Slice(0, carried + take),
            out var consumed,
            out var codePoint
        );
        switch (status)
        {
            case XmlCodePointReader.ReadStatus.Invalid:
                error = new XmlDecodingError(XmlErrorCodes.XML4003, baseOffset + pos - carried);
                return false;
            case XmlCodePointReader.ReadStatus.NeedMoreData:
                unit.Slice(0, carried + take).CopyTo(_carry);
                _carryCount = carried + take;
                pos += take;
                return true;
            default:
                if (IsReportedNonChar(codePoint))
                {
                    error = new XmlDecodingError(XmlErrorCodes.XML0002, baseOffset + pos - carried);
                    return false;
                }

                written += Write(codePoint, free.Slice(written));
                pos += consumed - carried;
                _carryCount = 0;
                return true;
        }
    }

    /// <summary>Confirms or switches the encoding once the declared name is known.</summary>
    /// <param name="declaredName">The <c>encoding</c> pseudo-attribute value.</param>
    /// <param name="error"><c>XML4001</c> for an unknown or unsupported name, <c>XML4002</c> when the name contradicts the detected family or the byte order mark.</param>
    /// <returns><see langword="true"/> when the declaration is consistent with the input.</returns>
    /// <remarks>
    /// A switch to another decoder resets it: the caller restarts decoding at byte zero with the same bytes, because
    /// the declaration itself was read from bytes decoded as ASCII-compatible characters.
    /// Limitation: declared names are an exact whitelist (ASCII case-insensitive): <c>UTF-8</c>, <c>ISO-8859-1</c>,
    /// <c>US-ASCII</c>, <c>UTF-16</c>, <c>UTF-16LE</c>, <c>UTF-16BE</c>, <c>ISO-10646-UCS-4</c>, <c>UCS-4</c>,
    /// <c>UTF-32</c>, <c>UTF-32BE</c> and <c>UTF-32LE</c>. IANA aliases such as <c>UTF8</c>, <c>ASCII</c>, <c>latin1</c>
    /// or <c>ISO_8859-1</c> are <c>XML4001</c>.
    /// </remarks>
    internal bool TryConfirmEncoding(ReadOnlySpan<char> declaredName, out XmlDecodingError error)
    {
        error = default;
        var declared = Resolve(declaredName);
        if (declared == Declared.Unknown)
        {
            error = new XmlDecodingError(XmlErrorCodes.XML4001, 0);
            return false;
        }

        if (!TryTarget(declared, out var target))
        {
            error = new XmlDecodingError(XmlErrorCodes.XML4002, 0);
            return false;
        }

        if (target != _encoding)
        {
            Restart(target);
        }

        return true;
    }

    // The encoding the declared name selects, or false when it contradicts the detected family or the byte order mark.
    private bool TryTarget(Declared declared, out XmlInputEncoding target)
    {
        var detected = _detection.Encoding;
        var bom = _detection.HasByteOrderMark;
        var isUtf16 = detected is XmlInputEncoding.Utf16LittleEndian or XmlInputEncoding.Utf16BigEndian;
        var isUcs4 = detected is XmlInputEncoding.Ucs4Order1234 or XmlInputEncoding.Ucs4Order4321;
        target = declared switch
        {
            Declared.Utf8 => XmlInputEncoding.Utf8,
            Declared.Latin1 => XmlInputEncoding.Iso88591,
            Declared.Ascii => XmlInputEncoding.UsAscii,
            Declared.Utf16Le => XmlInputEncoding.Utf16LittleEndian,
            Declared.Utf16Be => XmlInputEncoding.Utf16BigEndian,
            Declared.Ucs4Be => XmlInputEncoding.Ucs4Order1234,
            Declared.Ucs4Le => XmlInputEncoding.Ucs4Order4321,
            _ => detected,
        };

        return declared switch
        {
            Declared.Utf8 => detected == XmlInputEncoding.Utf8,
            Declared.Latin1 or Declared.Ascii => detected == XmlInputEncoding.Utf8 && !bom,
            // Section 4.3.3: UTF-16 requires a byte order mark; the sniffed pattern alone is not enough.
            Declared.Utf16 => isUtf16 && bom,
            Declared.Ucs4 => isUcs4,
            _ => detected == target,
        };
    }

    private static Declared Resolve(ReadOnlySpan<char> name) =>
        Is(name, "UTF-8") ? Declared.Utf8
        : Is(name, "ISO-8859-1") ? Declared.Latin1
        : Is(name, "US-ASCII") ? Declared.Ascii
        : Is(name, "UTF-16") ? Declared.Utf16
        : Is(name, "UTF-16LE") ? Declared.Utf16Le
        : Is(name, "UTF-16BE") ? Declared.Utf16Be
        : Is(name, "ISO-10646-UCS-4") || Is(name, "UCS-4") || Is(name, "UTF-32") ? Declared.Ucs4
        : Is(name, "UTF-32BE") ? Declared.Ucs4Be
        : Is(name, "UTF-32LE") ? Declared.Ucs4Le
        : Declared.Unknown;

    private void Restart(XmlInputEncoding encoding)
    {
        _encoding = encoding;
        _carryCount = 0;
        _skip = _detection.BomLength;
        _fed = 0;
    }

    private static int Write(int codePoint, Span<char> destination)
    {
        if (codePoint < 0x10000)
        {
            destination[0] = (char)codePoint;
            return 1;
        }

        var offset = codePoint - 0x10000;
        destination[0] = (char)(0xD800 + (offset >> 10));
        destination[1] = (char)(0xDC00 + (offset & 0x3FF));
        return 2;
    }

    // ASCII-only case folding, identical on every target framework: the names hold letters, digits, and hyphens.
    private static bool Is(ReadOnlySpan<char> value, string name)
    {
        if (value.Length != name.Length)
        {
            return false;
        }

        for (var i = 0; i < name.Length; i++)
        {
            var expected = name[i];
            var c = value[i];
            if (c != expected && !(expected is >= 'A' and <= 'Z' && (c | 0x20) == (expected | 0x20)))
            {
                return false;
            }
        }

        return true;
    }
}
