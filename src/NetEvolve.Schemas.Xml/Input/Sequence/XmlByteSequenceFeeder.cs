namespace NetEvolve.Schemas.Xml.Input.Sequence;

using System;
using System.Buffers;
using NetEvolve.Schemas.Xml.Buffers;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Input.Decoding;
using NetEvolve.Schemas.Xml.Reader.Declaration;

/// <summary>
/// Feeds the segments of a <see cref="ReadOnlySequence{T}"/> of bytes to the parser: the resumable decoder keeps a multi-byte
/// sequence that a segment edge splits, and the pieces are decoded one after the other.
/// </summary>
/// <remarks>
/// The encoding follows XML 1.0 Appendix F and the declaration like <c>XmlPipelineReader</c>: the declaration is read from a probe
/// decoding of the head of the sequence, bounded by <c>4 x MaxXmlDeclarationLength + 4</c> bytes, and decoding then starts again at
/// the first byte with the confirmed encoding. A sequence can be read twice, so the head is only copied, never kept.
/// </remarks>
internal sealed class XmlByteSequenceFeeder : XmlSequenceFeeder
{
    private const long MaximumHead = 1L << 30;

    private readonly ReadOnlySequence<byte> _input;
    private readonly ArrayPool<byte>? _bytePool;
    private XmlSegmentCursor<byte> _cursor;
    private XmlInputDecoder? _decoder;
    private bool _exhausted;

    internal XmlByteSequenceFeeder(
        ReadOnlySequence<byte> input,
        XmlReaderOptions options,
        ArrayPool<byte>? bytePool,
        ArrayPool<char>? charPool
    )
        : base(options, charPool)
    {
        _input = input;
        _bytePool = bytePool;
        _cursor = new XmlSegmentCursor<byte>(input);
    }

    private protected override void Start()
    {
        var headLength = Math.Min(Math.Min(_input.Length, 4L * Limits.MaxXmlDeclarationLength + 4), MaximumHead);
        var ended = headLength == _input.Length;
        using var head = new PooledBuffer<byte>((int)headLength, _bytePool);
        head.EnsureCapacity((int)headLength);
        _input.Slice(0, headLength).CopyTo(head.GetFreeSpan());
        head.Advance((int)headLength);

        // Fewer than four bytes are only possible when the head is the whole input.
        _ = XmlEncodingDetector.TryDetect(head.WrittenSpan, ended, out var detection);
        if (!XmlInputDecoder.TryCreate(detection, out var probe, out var error))
        {
            Fail(error);
            return;
        }

        var characters = Characters;
        _ = probe!.TryDecode(head.WrittenSpan, ended, characters, out _);
        var declared = XmlDeclarationParser.Parse(characters.WrittenSpan, XmlDeclarationKind.Xml);
        _ = XmlInputDecoder.TryCreate(detection, out var decoder, out _);
        if (
            declared.IsValid
            && declared.Encoding.Length > 0
            && !decoder!.TryConfirmEncoding(declared.Encoding, out error)
        )
        {
            Fail(error);
            characters.Clear();
            return;
        }

        characters.Clear();
        _decoder = decoder;
    }

    private protected override bool Fill(int wanted)
    {
        var decoder = _decoder;
        if (decoder is null || _exhausted)
        {
            return true;
        }

        var characters = Characters;
        while (characters.Length < wanted)
        {
            var more = _cursor.TryRead(wanted - characters.Length, out var piece);
            _exhausted = !more;
            if (!decoder.TryDecode(more ? piece.Span : default, _exhausted, characters, out var error))
            {
                Fail(error);
                return true;
            }

            if (_exhausted)
            {
                return true;
            }
        }

        return false;
    }
}
