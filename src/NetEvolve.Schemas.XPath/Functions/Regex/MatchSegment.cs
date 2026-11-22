namespace NetEvolve.Schemas.XPath.Functions.Regex;

using System;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Regex.Operations;

/// <summary>One segment of an input that a pattern divides: a match with the bounds of its capture groups, or a part between matches.</summary>
/// <remarks>The segment is a view of the enumerator that produced it and of the input span; it is valid until the enumerator advances.</remarks>
internal readonly ref struct MatchSegment
{
    private readonly ReadOnlySpan<char> _input;
    private readonly RegexMatch _match;

    internal MatchSegment(MatchSegmentKind kind, ReadOnlySpan<char> input, int index, int length, RegexMatch match)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(index, 0);
        ArgumentOutOfRangeException.ThrowIfLessThan(length, 0);

        Kind = kind;
        _input = input;
        Index = index;
        Length = length;
        _match = match;
    }

    /// <summary>Gets the kind of the segment.</summary>
    public MatchSegmentKind Kind { get; }

    /// <summary>Gets the index of the segment in the input.</summary>
    public int Index { get; }

    /// <summary>Gets the length of the segment.</summary>
    public int Length { get; }

    /// <summary>Gets the text of the segment, a view of the input.</summary>
    public ReadOnlySpan<char> Text => _input.Slice(Index, Length);

    /// <summary>Gets the number of capture groups of the pattern, not counting group 0; zero for a non-matching segment.</summary>
    public int GroupCount => Kind == MatchSegmentKind.Match ? _match.GroupCount : 0;

    /// <summary>Gets the bounds of a capture group; groups are numbered by their opening parenthesis, group 0 is the whole match.</summary>
    /// <param name="number">The group number, 0 to <see cref="GroupCount"/>.</param>
    /// <param name="range">The bounds in the input, <see langword="default"/> when the group took no part in the match.</param>
    /// <returns><see langword="true"/> when the group took part in the match, also with the empty string; always <see langword="false"/> for a non-matching segment.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="number"/> is negative or above <see cref="GroupCount"/>.</exception>
    public bool TryGetGroup(int number, out TextRange range)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(number, 0);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(number, GroupCount);

        if (Kind != MatchSegmentKind.Match)
        {
            range = default;
            return false;
        }

        return _match.TryGetGroup(number, out range);
    }

    /// <summary>Gets the text of a capture group.</summary>
    /// <param name="number">The group number, 0 to <see cref="GroupCount"/>.</param>
    /// <returns>The text, empty for a group that took no part in the match.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="number"/> is negative or above <see cref="GroupCount"/>.</exception>
    public ReadOnlySpan<char> GetGroupText(int number) =>
        TryGetGroup(number, out var range) ? range.Slice(_input) : default;
}
