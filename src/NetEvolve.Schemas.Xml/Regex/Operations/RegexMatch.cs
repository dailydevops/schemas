namespace NetEvolve.Schemas.Xml.Regex.Operations;

using System;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;

/// <summary>One match of a pattern in an input: its bounds and the bounds of its capture groups.</summary>
/// <remarks>The match is a view of the enumerator that produced it and of the input span; it is valid until the enumerator advances.</remarks>
public readonly ref struct RegexMatch
{
    private readonly ReadOnlySpan<char> _input;
    private readonly ReadOnlySpan<int> _groups;

    // The groups are the start and the end position of every group, group 0 (the whole match) first, -1 for a group that took no part.
    internal RegexMatch(ReadOnlySpan<char> input, ReadOnlySpan<int> groups)
    {
        _input = input;
        _groups = groups;
    }

    /// <summary>Gets the index of the match in the input.</summary>
    public int Index => _groups[0];

    /// <summary>Gets the length of the match.</summary>
    public int Length => _groups[1] - _groups[0];

    /// <summary>Gets the number of capture groups of the pattern, not counting group 0.</summary>
    public int GroupCount => (_groups.Length / 2) - 1;

    /// <summary>Gets the bounds of a capture group; groups are numbered by their opening parenthesis, group 0 is the whole match.</summary>
    /// <param name="number">The group number, 0 to <see cref="GroupCount"/>.</param>
    /// <param name="range">The bounds in the input, <see langword="default"/> when the group took no part in the match.</param>
    /// <returns><see langword="true"/> when the group took part in the match, also with the empty string.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="number"/> is negative or above <see cref="GroupCount"/>.</exception>
    public bool TryGetGroup(int number, out TextRange range)
    {
        Argument.ThrowIfLessThan(number, 0);
        Argument.ThrowIfGreaterThan(number, GroupCount);

        var start = _groups[2 * number];
        if (start < 0)
        {
            range = default;
            return false;
        }

        range = new TextRange(start, _groups[(2 * number) + 1] - start);
        return true;
    }

    /// <summary>Gets the text of a capture group.</summary>
    /// <param name="number">The group number, 0 to <see cref="GroupCount"/>.</param>
    /// <returns>The text, empty for a group that took no part in the match.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="number"/> is negative or above <see cref="GroupCount"/>.</exception>
    public ReadOnlySpan<char> GetGroupText(int number) =>
        TryGetGroup(number, out var range) ? range.Slice(_input) : default;
}
