namespace NetEvolve.Schemas.Xml.Tests.Unit.Input.Decoding;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using NetEvolve.Schemas.Xml.Buffers;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Input.Decoding;
using static DecodingTestHelper;

internal sealed class XmlInputDecoderTests
{
    private const string Sample = "<?xml version=\"1.0\"?><a>Aé€😀\r\n\t</a>";

    public static IEnumerable<Func<(string Name, byte[] Bytes)>> SampleDocuments()
    {
        yield return () => ("utf8", Encoding.UTF8.GetBytes(Sample));
        yield return () => ("utf8-bom", Concat(Hex("EF BB BF"), Encoding.UTF8.GetBytes(Sample)));
        yield return () => ("utf16le-bom", Concat(Hex("FF FE"), Encoding.Unicode.GetBytes(Sample)));
        yield return () => ("utf16be-bom", Concat(Hex("FE FF"), Encoding.BigEndianUnicode.GetBytes(Sample)));
        yield return () => ("utf16le", Encoding.Unicode.GetBytes(Sample));
        yield return () => ("utf16be", Encoding.BigEndianUnicode.GetBytes(Sample));
        yield return () => ("ucs4be", new UTF32Encoding(true, false).GetBytes(Sample));
        yield return () => ("ucs4le", new UTF32Encoding(false, false).GetBytes(Sample));
        yield return () => ("ucs4be-bom", Concat(Hex("00 00 FE FF"), new UTF32Encoding(true, false).GetBytes(Sample)));
        yield return () => ("ucs4le-bom", Concat(Hex("FF FE 00 00"), new UTF32Encoding(false, false).GetBytes(Sample)));
    }

    private static byte[] Concat(byte[] first, byte[] second)
    {
        var all = new byte[first.Length + second.Length];
        first.CopyTo(all, 0);
        second.CopyTo(all, first.Length);
        return all;
    }

    [Test]
    [MethodDataSource(nameof(SampleDocuments))]
    public async Task Decode_SampleDocument_ProducesTheCharacters(string name, byte[] bytes)
    {
        var (text, error) = Decode(bytes);

        await Assert.That(error).IsNull().Because(name);
        await Assert.That(text).IsEqualTo(Sample).Because(name);
    }

    [Test]
    [MethodDataSource(nameof(SampleDocuments))]
    public async Task Decode_SplitAtEveryByteOffset_EqualsUnsplitInput(string name, byte[] bytes)
    {
        for (var split = 0; split <= bytes.Length; split++)
        {
            var (text, error) = Decode(bytes, split);

            await Assert.That(error).IsNull().Because($"{name} @ {split}");
            await Assert.That(text).IsEqualTo(Sample).Because($"{name} @ {split}");
        }
    }

    [Test]
    [MethodDataSource(nameof(SampleDocuments))]
    public async Task Decode_OneByteChunks_EqualsUnsplitInput(string name, byte[] bytes)
    {
        var splits = new int[bytes.Length - 1];
        for (var i = 0; i < splits.Length; i++)
        {
            splits[i] = i + 1;
        }

        var (text, error) = Decode(bytes, splits);

        await Assert.That(error).IsNull().Because(name);
        await Assert.That(text).IsEqualTo(Sample).Because(name);
    }

    // Malformed UTF-8: code XML4003 and the offset of the first byte of the offending sequence, at every split.
    [Test]
    [Arguments("61 62 80", 2L)] // stray continuation byte
    [Arguments("61 C0 80", 1L)] // overlong two-byte form
    [Arguments("C1 BF", 0L)] // overlong two-byte form
    [Arguments("61 E0 80 80", 1L)] // overlong three-byte form
    [Arguments("F0 80 80 80", 0L)] // overlong four-byte form
    [Arguments("61 62 ED A0 80", 2L)] // encoded surrogate
    [Arguments("F4 90 80 80", 0L)] // above U+10FFFF
    [Arguments("F5 80 80 80", 0L)] // invalid lead byte
    [Arguments("FF", 0L)] // invalid byte
    [Arguments("61 E2 82 41", 1L)] // missing continuation byte
    [Arguments("61 62 63 E2 82", 3L)] // truncated at the end
    [Arguments("61 EF BF BE", 1L)] // U+FFFE is not a Char
    [Arguments("61 01", 1L)] // U+0001 is not a Char
    public Task Decode_MalformedUtf8_YieldsMalformedCodeAndOffset(string hex, long offset) =>
        AssertMalformed(Hex(hex), offset);

    // Malformed UTF-16 and UCS-4.
    [Test]
    [Arguments("FF FE 3D D8", 2L)] // little endian: high surrogate at the end
    [Arguments("FF FE 3D D8 41 00", 2L)] // high surrogate followed by a non-surrogate
    [Arguments("FF FE 3D D8 3D D8", 2L)] // high surrogate followed by a high surrogate
    [Arguments("FF FE 41 00 00 DE", 4L)] // lone low surrogate
    [Arguments("FF FE 41", 2L)] // odd byte count
    [Arguments("FF FE FE FF", 2L)] // U+FFFE
    [Arguments("FE FF D8 3D", 2L)] // big endian: high surrogate at the end
    [Arguments("FE FF D8 3D 00 41", 2L)] // high surrogate followed by a non-surrogate
    [Arguments("FE FF 00 41 DE 00", 4L)] // lone low surrogate
    [Arguments("FE FF 00 41 00", 4L)] // odd byte count
    [Arguments("00 00 FE FF 00 11 00 00", 4L)] // UCS-4 above U+10FFFF
    [Arguments("00 00 FE FF 00 00 D8 00", 4L)] // UCS-4 surrogate
    [Arguments("00 00 FE FF 00 00 00", 4L)] // UCS-4 truncated
    public Task Decode_MalformedUtf16AndUcs4_YieldsMalformedCodeAndOffset(string hex, long offset) =>
        AssertMalformed(Hex(hex), offset);

    private static async Task AssertMalformed(byte[] bytes, long offset)
    {
        for (var split = 0; split <= bytes.Length; split++)
        {
            var (_, error) = split == 0 || split == bytes.Length ? Decode(bytes) : Decode(bytes, split);

            await Assert.That(error).IsNotNull().Because($"split {split}");
            await Assert.That(error!.Value.Code).IsEqualTo(XmlErrorCodes.XML4003);
            await Assert.That(error.Value.ByteOffset).IsEqualTo(offset).Because($"split {split}");
        }
    }

    [Test]
    [Arguments("61 80 62")]
    [Arguments("61 C0 80 62")]
    [Arguments("FF FE 41 00 00 DE 42 00")]
    [Arguments("FE FF 00 41 D8 00 00 42")]
    public async Task Decode_InvalidInput_NeverProducesReplacementCharacter(string hex)
    {
        var (text, error) = Decode(Hex(hex));

        await Assert.That(error).IsNotNull();
        await Assert.That(text.Contains('�', StringComparison.Ordinal)).IsFalse();
    }

    [Test]
    public async Task Decode_ReplacementCharacterInInput_IsKept()
    {
        var (text, error) = Decode(Hex("EF BF BD"));

        await Assert.That(error).IsNull();
        await Assert.That(text).IsEqualTo("�");
    }

    [Test]
    public async Task Decode_BomIsConsumedAndOffsetsCountIt()
    {
        var (text, error) = Decode(Hex("EF BB BF 41 80"));

        await Assert.That(text).IsEqualTo("A");
        await Assert.That(error!.Value.ByteOffset).IsEqualTo(4L);
    }

    [Test]
    public async Task ConfirmEncoding_Iso88591_DecodesEveryByteToItsCodePoint()
    {
        var bytes = Hex("3C 3F 78 6D 6C 20 E9 FF 80 9F");
        XmlInputDecoder.TryCreate(Detect(bytes), out var decoder, out _);
        var confirmed = decoder!.TryConfirmEncoding("iso-8859-1".AsSpan(), out var confirmError);
        using var output = new PooledBuffer<char>();

        var ok = decoder.TryDecode(bytes, true, output, out _);

        await Assert.That(confirmed).IsTrue();
        await Assert.That(confirmError.Code).IsNull();
        await Assert.That(decoder.Encoding).IsEqualTo(XmlInputEncoding.Iso88591);
        await Assert.That(ok).IsTrue();
        await Assert.That(output.WrittenSpan.ToString()).IsEqualTo("<?xml éÿ\u0080\u009F");
    }

    [Test]
    public async Task ConfirmEncoding_UsAscii_DecodesAscii_AndRejectsHighBytes()
    {
        var bytes = Hex("41 42 80 43");
        XmlInputDecoder.TryCreate(Detect(bytes), out var decoder, out _);
        decoder!.TryConfirmEncoding("US-ASCII".AsSpan(), out _);
        using var output = new PooledBuffer<char>();

        var ok = decoder.TryDecode(bytes, true, output, out var error);

        await Assert.That(ok).IsFalse();
        await Assert.That(error.Code).IsEqualTo(XmlErrorCodes.XML4003);
        await Assert.That(error.ByteOffset).IsEqualTo(2L);
        await Assert.That(output.WrittenSpan.ToString()).IsEqualTo("AB");
    }

    [Test]
    [Arguments("EF BB BF 41", "UTF-16")] // BOM contradicts the declaration
    [Arguments("FF FE 41 00", "UTF-16BE")]
    [Arguments("FE FF 00 41", "UTF-8")]
    [Arguments("FF FE 41 00", "ISO-8859-1")]
    [Arguments("EF BB BF 41", "US-ASCII")]
    [Arguments("EF BB BF 41", "ISO-8859-1")]
    [Arguments("3C 3F 78 6D", "UTF-16")] // declared 16-bit encoding on 8-bit input
    [Arguments("3C 3F 78 6D", "UTF-16LE")]
    [Arguments("3C 3F 78 6D", "UTF-16BE")]
    [Arguments("3C 3F 78 6D", "ISO-10646-UCS-4")]
    [Arguments("3C 00 3F 00", "UTF-16")] // UTF-16 without a byte order mark
    [Arguments("3C 00 3F 00", "UTF-16BE")]
    [Arguments("00 3C 00 3F", "UTF-16LE")]
    [Arguments("3C 00 3F 00", "UTF-8")]
    public async Task ConfirmEncoding_Contradiction_YieldsMismatchCode(string hex, string declared)
    {
        XmlInputDecoder.TryCreate(Detect(Hex(hex)), out var decoder, out _);

        var confirmed = decoder!.TryConfirmEncoding(declared.AsSpan(), out var error);

        await Assert.That(confirmed).IsFalse();
        await Assert.That(error.Code).IsEqualTo(XmlErrorCodes.XML4002);
    }

    [Test]
    [Arguments("3C 3F 78 6D", "utf-8", "Utf8")]
    [Arguments("EF BB BF 41", "UTF-8", "Utf8")]
    [Arguments("3C 3F 78 6D", "ISO-8859-1", "Iso88591")]
    [Arguments("3C 3F 78 6D", "Us-Ascii", "UsAscii")]
    [Arguments("FF FE 41 00", "UTF-16", "Utf16LittleEndian")]
    [Arguments("FE FF 00 41", "utf-16be", "Utf16BigEndian")]
    [Arguments("3C 00 3F 00", "UTF-16LE", "Utf16LittleEndian")]
    [Arguments("00 3C 00 3F", "UTF-16BE", "Utf16BigEndian")]
    [Arguments("00 00 00 3C", "ISO-10646-UCS-4", "Ucs4Order1234")]
    [Arguments("3C 00 00 00", "UTF-32LE", "Ucs4Order4321")]
    public async Task ConfirmEncoding_Matching_Succeeds(string hex, string declared, string expected)
    {
        XmlInputDecoder.TryCreate(Detect(Hex(hex)), out var decoder, out _);

        var confirmed = decoder!.TryConfirmEncoding(declared.AsSpan(), out var error);

        await Assert.That(confirmed).IsTrue();
        await Assert.That(error.Code).IsNull();
        await Assert.That(decoder.Encoding.ToString()).IsEqualTo(expected);
    }

    [Test]
    [Arguments("EBCDIC")]
    [Arguments("windows-1252")]
    [Arguments("UTF-7")]
    [Arguments("")]
    public async Task ConfirmEncoding_UnknownName_YieldsUnsupportedEncodingCode(string declared)
    {
        XmlInputDecoder.TryCreate(Detect(Hex("3C 3F 78 6D")), out var decoder, out _);

        var confirmed = decoder!.TryConfirmEncoding(declared.AsSpan(), out var error);

        await Assert.That(confirmed).IsFalse();
        await Assert.That(error.Code).IsEqualTo(XmlErrorCodes.XML4001);
    }

    [Test]
    public async Task ConfirmEncoding_Switch_ResetsStateSoTheCallerCanRestartAtByteZero()
    {
        var bytes = Hex("3C 61 3E E9");
        XmlInputDecoder.TryCreate(Detect(bytes), out var decoder, out _);
        using var first = new PooledBuffer<char>();
        decoder!.TryDecode(bytes.AsSpan(0, 4), false, first, out _);

        decoder.TryConfirmEncoding("ISO-8859-1".AsSpan(), out _);
        using var second = new PooledBuffer<char>();
        var ok = decoder.TryDecode(bytes, true, second, out _);

        await Assert.That(ok).IsTrue();
        await Assert.That(second.WrittenSpan.ToString()).IsEqualTo("<a>é");
    }

    [Test]
    public async Task TryDecode_NullOutput_Throws()
    {
        XmlInputDecoder.TryCreate(Detect(Hex("3C")), out var decoder, out _);

        await Assert.That(() => decoder!.TryDecode(default, true, null!, out _)).Throws<ArgumentNullException>();
    }

    [Test]
    public async Task Folder_UsesNeitherLinqNorSystemXml_AndWritesToPooledBuffers(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Schemas.slnx")))
        {
            dir = dir.Parent;
        }

        var folder = Path.Combine(dir!.FullName, "src", "NetEvolve.Schemas.Xml", "Input", "Decoding");
        var files = Directory.GetFiles(folder, "*.cs");
        var all = new StringBuilder();

        await Assert.That(files.Length).IsGreaterThan(0);
        foreach (var file in files)
        {
            var text = await File.ReadAllTextAsync(file, cancellationToken);
            all.Append(text);
            await Assert.That(text.Contains("System.Linq", StringComparison.Ordinal)).IsFalse().Because(file);
            await Assert.That(text.Contains("System.Xml", StringComparison.Ordinal)).IsFalse().Because(file);
        }

        await Assert.That(all.ToString().Contains("PooledBuffer<char>", StringComparison.Ordinal)).IsTrue();
    }
}
