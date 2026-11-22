namespace NetEvolve.Schemas.XPath.Functions.Regex;

using System;
using System.Runtime.InteropServices;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Regex.Contracts;
using NetEvolve.Schemas.Xml.Regex.Operations;

/// <summary>
/// Divides an input at the matches of a pattern into alternating segments, in input order: the part before the first match, each match,
/// the part between two matches, and the part after the last, without a substring allocation. <c>fn:matches</c>, <c>fn:replace</c>, and
/// <c>fn:tokenize</c> read it, and <c>fn:analyze-string</c> and <c>xsl:analyze-string</c> will.
/// </summary>
/// <remarks>
/// <para>
/// A non-matching segment is never empty, so the input of a pattern without a match is one segment and an empty input has none; a
/// consumer that needs the empty parts (<c>fn:tokenize</c>) reads them from the bounds of the matches. Matches are those of
/// <see cref="RegexMatchEnumerator"/>: leftmost, without overlap, and an empty match is a segment of length zero after which the
/// search continues one code point further. It rents one small array from the shared pool: call <see cref="Dispose"/> after the last
/// use, and never use a copy next to the original. When a step ceiling is reached the iteration ends and <see cref="IsAborted"/> is
/// <see langword="true"/>; the segments already returned must then be discarded.
/// </para>
/// <para>Limitation: a capture group keeps the text of the last iteration of its enclosing loop in which it took part (the limitation of <see cref="RegexMatchEnumerator"/>).</para>
/// </remarks>
[StructLayout(LayoutKind.Auto)]
internal ref struct MatchSegmentEnumerator
{
    private RegexMatchEnumerator _matches;
    private readonly ReadOnlySpan<char> _input;
    private MatchSegmentKind _kind;
    private int _index;
    private int _length;
    private int _position;
    private bool _matchPending;
    private bool _finished;

    /// <summary>Initializes a new instance of the <see cref="MatchSegmentEnumerator"/> struct.</summary>
    /// <param name="operations">The operations of the compiled pattern.</param>
    /// <param name="input">The input; it must stay unchanged while the enumerator is in use.</param>
    /// <param name="limits">The step ceilings, <see cref="RegexStepLimits.Default"/> for <see langword="null"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="operations"/> is <see langword="null"/>.</exception>
    public MatchSegmentEnumerator(RegexOperations operations, ReadOnlySpan<char> input, RegexStepLimits? limits = null)
        : this(Matches(operations, input, limits), input) { }

    // The enumerator of the matches arrives as a parameter, so this type uses it and does not own its construction.
    private MatchSegmentEnumerator(RegexMatchEnumerator matches, ReadOnlySpan<char> input)
    {
        _matches = matches;
        _input = input;
        _kind = MatchSegmentKind.NonMatch;
        _index = 0;
        _length = 0;
        _position = 0;
        _matchPending = false;
        _finished = false;
    }

    /// <summary>Gets the current segment; it is valid after <see cref="MoveNext"/> returned <see langword="true"/> and until the next call.</summary>
    public readonly MatchSegment Current =>
        new(_kind, _input, _index, _length, _kind == MatchSegmentKind.Match ? _matches.Current : default);

    /// <summary>Gets a value indicating whether the iteration ended because a step ceiling was reached.</summary>
    public readonly bool IsAborted => _matches.IsAborted;

    /// <summary>Advances to the next segment.</summary>
    /// <returns><see langword="false"/> when no segment is left, or a step ceiling was reached.</returns>
    public bool MoveNext()
    {
        if (_matchPending)
        {
            _matchPending = false;
            SetMatch();
            return true;
        }

        if (_finished)
        {
            return false;
        }

        if (_matches.MoveNext())
        {
            var match = _matches.Current;
            if (match.Index > _position)
            {
                // The part before the match comes first; the match itself is returned by the next call.
                Set(MatchSegmentKind.NonMatch, _position, match.Index - _position);
                _position = match.Index;
                _matchPending = true;
                return true;
            }

            SetMatch();
            return true;
        }

        _finished = true;
        if (_matches.IsAborted || _position >= _input.Length)
        {
            return false;
        }

        Set(MatchSegmentKind.NonMatch, _position, _input.Length - _position);
        _position = _input.Length;
        return true;
    }

    /// <summary>Returns the rented array to the pool and ends the iteration; calling it again has no effect.</summary>
    public void Dispose()
    {
        _matches.Dispose();
        _finished = true;
        _matchPending = false;
    }

    private static RegexMatchEnumerator Matches(
        RegexOperations operations,
        ReadOnlySpan<char> input,
        RegexStepLimits? limits
    )
    {
        Argument.ThrowIfNull(operations);

        return operations.EnumerateMatches(input, limits);
    }

    private void SetMatch()
    {
        var match = _matches.Current;
        Set(MatchSegmentKind.Match, match.Index, match.Length);
        _position = match.Index + match.Length;
    }

    private void Set(MatchSegmentKind kind, int index, int length)
    {
        _kind = kind;
        _index = index;
        _length = length;
    }
}
