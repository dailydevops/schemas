namespace NetEvolve.Schemas.Xml.Input.Sequence;

using System;
using System.Buffers;
using NetEvolve.Arguments;

/// <summary>Walks the segments of a <see cref="ReadOnlySequence{T}"/> in pieces of bounded length; empty segments are skipped.</summary>
/// <typeparam name="T">The element type.</typeparam>
internal struct XmlSegmentCursor<T>
{
    private readonly ReadOnlySequence<T> _sequence;
    private SequencePosition _next;
    private ReadOnlyMemory<T> _current;
    private int _offset;

    internal XmlSegmentCursor(ReadOnlySequence<T> sequence)
    {
        _sequence = sequence;
        _next = sequence.Start;
        _current = default;
        _offset = 0;
    }

    /// <summary>Takes the next piece of the current segment, or the first piece of the next non-empty one.</summary>
    /// <param name="max">The longest piece, at least one.</param>
    /// <param name="piece">The piece, which lies inside one segment.</param>
    /// <returns><see langword="false"/> when no element is left.</returns>
    internal bool TryRead(int max, out ReadOnlyMemory<T> piece)
    {
        Argument.ThrowIfLessThan(max, 1);

        while (_offset >= _current.Length)
        {
            if (!_sequence.TryGet(ref _next, out _current))
            {
                _current = default;
                piece = default;
                return false;
            }

            _offset = 0;
        }

        var take = Math.Min(max, _current.Length - _offset);
        piece = _current.Slice(_offset, take);
        _offset += take;
        return true;
    }
}
