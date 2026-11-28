namespace NetEvolve.Schemas.Xml.Tests.Unit.Input.Decoding;

using System;
using System.Collections.Generic;
using System.Text;
using NetEvolve.Schemas.Xml.Buffers;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Input.Decoding;
using NetEvolve.Schemas.Xml.Text.CharClasses;
using static DecodingTestHelper;

// Differential tests: the decoder, however the bytes are chunked, equals a plain one-code-point-at-a-time reference.
internal sealed class XmlInputDecoderBulkTests
{
    private const string Text = "<?xml version=\"1.0\"?><a>plain ascii run, Aé€😀 ünïcödé ÿ 日本語 𝄞 end</a>";

    public static IEnumerable<Func<(string Name, byte[] Bytes, bool Latin1)>> Inputs()
    {
        var utf8 = Encoding.UTF8.GetBytes(Text);
        yield return () => ("utf8", utf8, false);
        yield return () => ("latin1", Hex("3C 61 3E 61 62 E9 FF 80 A0 63 3C 2F 61 3E"), true);
        yield return () => ("utf8-overlong", Concat(utf8, Hex("C0 80 61")), false);
        yield return () => ("utf8-surrogate", Concat(utf8, Hex("ED A0 80 61")), false);
        yield return () => ("utf8-above-max", Concat(utf8, Hex("F4 90 80 80 61")), false);
        yield return () => ("utf8-lone-continuation", Concat(utf8, Hex("80 61")), false);
        yield return () => ("utf8-truncated-tail", Concat(utf8, Hex("E2 82")), false);
        yield return () => ("utf8-control", Concat(utf8, Hex("01 61 EF BF BE 61")), false);
        yield return () =>
            ("utf8-control-late", Concat(Encoding.UTF8.GetBytes("é€€€€€€€€€€€€€€€€€€€"), Hex("0B 61")), false);
    }

    private static byte[] Concat(byte[] first, byte[] second)
    {
        var all = new byte[first.Length + second.Length];
        first.CopyTo(all, 0);
        second.CopyTo(all, first.Length);
        return all;
    }

    private static (string Text, XmlDecodingError? Error) Reference(
        byte[] bytes,
        XmlInputEncoding encoding,
        int skip,
        bool reportNonChar
    )
    {
        var sb = new StringBuilder();
        var pos = skip;
        while (pos < bytes.Length)
        {
            var status = XmlCodePointReader.Read(encoding, bytes.AsSpan(pos), out var consumed, out var cp);
            if (status != XmlCodePointReader.ReadStatus.Ok)
            {
                return (sb.ToString(), new XmlDecodingError(XmlErrorCodes.XML4003, pos));
            }

            if (reportNonChar && !XmlCharClass.IsChar(cp))
            {
                return (sb.ToString(), new XmlDecodingError(XmlErrorCodes.XML0002, pos));
            }

            _ = sb.Append(char.ConvertFromUtf32(cp));
            pos += consumed;
        }

        return (sb.ToString(), null);
    }

    private static (string Text, XmlDecodingError? Error) Run(
        byte[] bytes,
        XmlInputEncoding encoding,
        int chunk,
        bool reportNonChar
    )
    {
        var detection = new XmlEncodingDetection(encoding, 0);
        _ = XmlInputDecoder.TryCreate(detection, out var decoder, out _, reportNonChar);
        using var output = new PooledBuffer<char>(2);
        var start = 0;
        while (start < bytes.Length)
        {
            var length = Math.Min(chunk, bytes.Length - start);
            var last = start + length == bytes.Length;
            if (!decoder!.TryDecode(bytes.AsSpan(start, length), last, output, out var error))
            {
                return (output.WrittenSpan.ToString(), error);
            }

            start += length;
        }

        return (output.WrittenSpan.ToString(), null);
    }

    [Test]
    [MethodDataSource(nameof(Inputs))]
    public async Task Decode_AnyChunking_EqualsTheCodePointReference(string name, byte[] bytes, bool latin1)
    {
        var encoding = latin1 ? XmlInputEncoding.Iso88591 : XmlInputEncoding.Utf8;
        foreach (var reportNonChar in new[] { false, true })
        {
            var expected = Reference(bytes, encoding, 0, reportNonChar);
            foreach (var chunk in new[] { 1, 2, 3, 7, 64, bytes.Length })
            {
                var actual = Run(bytes, encoding, chunk, reportNonChar);
                var because = $"{name} chunk {chunk} reportNonChar {reportNonChar}";
                await Assert.That(actual.Text).IsEqualTo(expected.Text).Because(because);
                await Assert.That(actual.Error?.Code).IsEqualTo(expected.Error?.Code).Because(because);
                await Assert.That(actual.Error?.ByteOffset).IsEqualTo(expected.Error?.ByteOffset).Because(because);
            }
        }
    }
}
