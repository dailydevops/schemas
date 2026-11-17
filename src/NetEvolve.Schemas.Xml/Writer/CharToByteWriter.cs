namespace NetEvolve.Schemas.Xml.Writer;

using System;
using System.Buffers;
using NetEvolve.Arguments;

/// <summary>
/// Adapts a character buffer writer to a byte buffer writer, as UTF-8 or UTF-16 little endian. A high surrogate at the
/// end of one <see cref="Advance"/> is held back until the next one completes the pair.
/// </summary>
internal sealed class CharToByteWriter : IBufferWriter<char>
{
    private const int ScratchLength = 1024;
    private const char Replacement = '�';

    private readonly IBufferWriter<byte> _bytes;
    private readonly bool _utf16;
    private readonly char[] _scratch = new char[ScratchLength];
    private char _high;

    internal CharToByteWriter(IBufferWriter<byte> bytes, bool utf16)
    {
        Argument.ThrowIfNull(bytes);
        _bytes = bytes;
        _utf16 = utf16;
    }

    internal void WriteBom()
    {
        var span = _bytes.GetSpan(2);
        span[0] = 0xFF;
        span[1] = 0xFE;
        _bytes.Advance(2);
    }

    public Memory<char> GetMemory(int sizeHint = 0) => _scratch;

    public Span<char> GetSpan(int sizeHint = 0) => _scratch;

    public void Advance(int count)
    {
        if (count == 0)
        {
            return;
        }

        var chars = new ReadOnlySpan<char>(_scratch, 0, count);
        if (_utf16)
        {
            EncodeUtf16(chars);
        }
        else
        {
            EncodeUtf8(chars);
        }
    }

    private void EncodeUtf16(ReadOnlySpan<char> chars)
    {
        var dest = _bytes.GetSpan(chars.Length * 2);
        for (var i = 0; i < chars.Length; i++)
        {
            dest[2 * i] = (byte)chars[i];
            dest[(2 * i) + 1] = (byte)(chars[i] >> 8);
        }

        _bytes.Advance(chars.Length * 2);
    }

    private void EncodeUtf8(ReadOnlySpan<char> chars)
    {
        var dest = _bytes.GetSpan((chars.Length * 3) + 3);
        var written = 0;
        foreach (var c in chars)
        {
            var current = c;
            if (_high != 0)
            {
                if (char.IsLowSurrogate(current))
                {
                    written += Utf8(char.ConvertToUtf32(_high, current), dest.Slice(written));
                    _high = '\0';
                    continue;
                }

                written += Utf8(Replacement, dest.Slice(written));
                _high = '\0';
            }

            if (char.IsHighSurrogate(current))
            {
                _high = current;
            }
            else
            {
                written += Utf8(char.IsLowSurrogate(current) ? Replacement : current, dest.Slice(written));
            }
        }

        _bytes.Advance(written);
    }

    private static int Utf8(int codePoint, Span<byte> dest)
    {
        if (codePoint < 0x80)
        {
            dest[0] = (byte)codePoint;
            return 1;
        }

        if (codePoint < 0x800)
        {
            dest[0] = (byte)(0xC0 | (codePoint >> 6));
            dest[1] = (byte)(0x80 | (codePoint & 0x3F));
            return 2;
        }

        if (codePoint < 0x10000)
        {
            dest[0] = (byte)(0xE0 | (codePoint >> 12));
            dest[1] = (byte)(0x80 | ((codePoint >> 6) & 0x3F));
            dest[2] = (byte)(0x80 | (codePoint & 0x3F));
            return 3;
        }

        dest[0] = (byte)(0xF0 | (codePoint >> 18));
        dest[1] = (byte)(0x80 | ((codePoint >> 12) & 0x3F));
        dest[2] = (byte)(0x80 | ((codePoint >> 6) & 0x3F));
        dest[3] = (byte)(0x80 | (codePoint & 0x3F));
        return 4;
    }
}
