namespace NetEvolve.Schemas.Xml.Datatypes.Binary;

using System;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;

/// <summary>The lexical scan and the decoding of <c>hexBinary</c> and <c>base64Binary</c>, on spans.</summary>
internal static class BinaryLexical
{
#if !NETSTANDARD2_0
    // Below this length the call into the runtime decoder costs more than the loop.
    private const int RuntimeDecoderLength = 32;
#endif

    /// <summary>Checks a lexical form and counts its octets.</summary>
    /// <param name="lexical">The lexical form; whitespace is not handled here, only the single spaces that base64Binary allows.</param>
    /// <param name="kind">The lexical space.</param>
    /// <param name="octets">The number of octets of the value.</param>
    /// <param name="offset">The offset of the error.</param>
    /// <returns><see cref="DatatypeError.None"/> when the form is valid.</returns>
    internal static DatatypeError Scan(ReadOnlySpan<char> lexical, BinaryKind kind, out int octets, out int offset) =>
        kind == BinaryKind.HexBinary
            ? ScanHex(lexical, out octets, out offset)
            : ScanBase64(lexical, out octets, out offset);

    /// <summary>Decodes a form that passed <see cref="Scan"/>.</summary>
    internal static byte[] Decode(ReadOnlySpan<char> lexical, BinaryKind kind, int octets)
    {
        var result = new byte[octets];
        if (kind == BinaryKind.HexBinary)
        {
            for (var i = 0; i < octets; i++)
            {
                result[i] = (byte)((Hex(lexical[2 * i]) << 4) | Hex(lexical[(2 * i) + 1]));
            }

            return result;
        }

#if !NETSTANDARD2_0
        // A form without spaces is plain base64 that Scan has already validated; the runtime decodes it vectorized.
        if (
            lexical.Length >= RuntimeDecoderLength
            && lexical.IndexOf(' ') < 0
            && Convert.TryFromBase64Chars(lexical, result, out var decoded)
            && decoded == octets
        )
        {
            return result;
        }
#endif

        var written = 0;
        var accumulator = 0;
        var bits = 0;
        for (var i = 0; i < lexical.Length; i++)
        {
            var index = Base64Index(lexical[i]);
            if (index < 0)
            {
                continue; // a space or a padding sign
            }

            accumulator = (accumulator << 6) | index;
            bits += 6;
            if (bits >= 8)
            {
                bits -= 8;
                result[written++] = (byte)(accumulator >> bits);
                accumulator &= (1 << bits) - 1;
            }
        }

        return result;
    }

    internal static string ToHex(ReadOnlySpan<byte> octets)
    {
        var chars = new char[octets.Length * 2];
        for (var i = 0; i < octets.Length; i++)
        {
            chars[2 * i] = HexDigit(octets[i] >> 4);
            chars[(2 * i) + 1] = HexDigit(octets[i] & 0xF);
        }

        return new string(chars);
    }

    private static DatatypeError ScanHex(ReadOnlySpan<char> lexical, out int octets, out int offset)
    {
        octets = 0;
        offset = 0;
        for (var i = 0; i < lexical.Length; i++)
        {
            if (Hex(lexical[i]) < 0)
            {
                offset = i;
                return DatatypeError.InvalidCharacter;
            }
        }

        if ((lexical.Length & 1) != 0)
        {
            offset = lexical.Length;
            return DatatypeError.InvalidLexicalForm;
        }

        octets = lexical.Length / 2;
        return DatatypeError.None;
    }

    // XSD 1.1: Base64Binary ::= ((B64S B64S B64S B64S)* ((B64S B64S B64S B64S) | (B64S B64S B64S B64) | (B64S B64S B16S '=') | (B64S B04S '=' #x20? '='))) ?
    // with B64S ::= B64 #x20?: a single space may follow each character, and the padding must carry zero bits.
    private static DatatypeError ScanBase64(ReadOnlySpan<char> lexical, out int octets, out int offset)
    {
        octets = 0;
        offset = 0;

        // The leading run of alphabet characters needs none of the space and padding rules.
        var run = 0;
        while (run < lexical.Length && Base64Index(lexical[run]) >= 0)
        {
            run++;
        }

        var data = run;
        var padding = 0;
        var lastDataOffset = run > 0 ? run - 1 : 0;
        var lastWasSpace = false;
        for (var i = run; i < lexical.Length; i++)
        {
            var c = lexical[i];
            if (c == ' ')
            {
                if (i == 0 || lastWasSpace)
                {
                    offset = i;
                    return DatatypeError.InvalidLexicalForm;
                }

                lastWasSpace = true;
                continue;
            }

            lastWasSpace = false;
            if (c == '=')
            {
                if (!PaddingAllowed(data, padding))
                {
                    offset = i;
                    return DatatypeError.InvalidLexicalForm;
                }

                padding++;
                continue;
            }

            var index = Base64Index(c);
            if (index < 0)
            {
                offset = i;
                return DatatypeError.InvalidCharacter;
            }

            if (padding > 0)
            {
                offset = i;
                return DatatypeError.InvalidLexicalForm;
            }

            data++;
            lastDataOffset = i;
        }

        var error = CheckBase64Tail(lexical, data, padding, lastDataOffset, out offset);
        if (error == DatatypeError.None)
        {
            octets = ((data + padding) / 4 * 3) - padding;
        }

        return error;
    }

    // The first sign needs two or three data characters in the group, the second exactly two.
    private static bool PaddingAllowed(int data, int padding) =>
        padding == 0 ? data % 4 >= 2 : padding == 1 && data % 4 == 2;

    private static DatatypeError CheckBase64Tail(
        ReadOnlySpan<char> lexical,
        int data,
        int padding,
        int lastDataOffset,
        out int offset
    )
    {
        offset = lexical.Length;
        var tail = data % 4;
        if ((lexical.Length > 0 && lexical[lexical.Length - 1] == ' ') || (tail == 0 ? 0 : 4 - tail) != padding)
        {
            return DatatypeError.InvalidLexicalForm;
        }

        // The unused low bits of the last character must be zero: B16 for one padding sign, B04 for two.
        if (padding > 0 && Base64Index(lexical[lastDataOffset]) % (padding == 1 ? 4 : 16) != 0)
        {
            offset = lastDataOffset;
            return DatatypeError.InvalidLexicalForm;
        }

        return DatatypeError.None;
    }

    // The Base64 alphabet: A-Z, a-z, 0-9, '+', '/'; -1 for every other character.
    private static int Base64Index(char c) => c < Base64Indexes.Length ? Base64Indexes[c] : -1;

    private static readonly sbyte[] Base64Indexes = CreateBase64Indexes();

    private static sbyte[] CreateBase64Indexes()
    {
        var table = new sbyte[128];
        for (var i = 0; i < table.Length; i++)
        {
            table[i] = -1;
        }

        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/";
        for (var i = 0; i < alphabet.Length; i++)
        {
            table[alphabet[i]] = (sbyte)i;
        }

        return table;
    }

    private static int Hex(char c) =>
        c >= '0' && c <= '9' ? c - '0'
        : c >= 'A' && c <= 'F' ? c - 'A' + 10
        : c >= 'a' && c <= 'f' ? c - 'a' + 10
        : -1;

    private static char HexDigit(int value) => (char)(value < 10 ? '0' + value : 'A' + value - 10);
}
