namespace NetEvolve.Schemas.Xml.Input.Sequence;

using System.Buffers;
using NetEvolve.Schemas.Xml.Contracts;

/// <summary>Feeds the segments of a <see cref="ReadOnlySequence{T}"/> of characters to the parser; no decoding is involved.</summary>
internal sealed class XmlCharSequenceFeeder : XmlSequenceFeeder
{
    private XmlSegmentCursor<char> _cursor;
    private bool _exhausted;

    internal XmlCharSequenceFeeder(ReadOnlySequence<char> input, XmlReaderOptions options, ArrayPool<char>? charPool)
        : base(options, charPool) => _cursor = new XmlSegmentCursor<char>(input);

    private protected override void Start() { }

    private protected override bool Fill(int wanted)
    {
        var characters = Characters;
        while (!_exhausted && characters.Length < wanted)
        {
            if (_cursor.TryRead(wanted - characters.Length, out var piece))
            {
                characters.Append(piece.Span);
            }
            else
            {
                _exhausted = true;
            }
        }

        return _exhausted;
    }
}
