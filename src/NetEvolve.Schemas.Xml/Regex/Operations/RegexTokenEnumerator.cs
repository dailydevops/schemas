namespace NetEvolve.Schemas.Xml.Regex.Operations;

using System.Runtime.InteropServices;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;

/// <summary>Enumerates the tokens of an input: the parts between the matches of a separator pattern, as ranges of the input.</summary>
/// <remarks>
/// A separator at the start of the input gives an empty first token, a separator at the end an empty last token, and adjacent
/// separators an empty token between them. Use it in a <see langword="foreach"/> loop or call <see cref="Dispose"/> after the
/// last use. A copy shares the rented array, so dispose exactly one of the copies (a <see langword="foreach"/> loop disposes its own copy). When a step ceiling is reached the enumeration ends and <see cref="IsAborted"/> is <see langword="true"/>; the
/// tokens already returned must then be discarded.
/// </remarks>
[StructLayout(LayoutKind.Auto)]
public ref struct RegexTokenEnumerator
{
    private RegexMatchEnumerator _matches;
    private readonly int _inputLength;
    private int _position;
    private bool _active;
    private bool _aborted;
    private TextRange _current;

    internal RegexTokenEnumerator(RegexMatchEnumerator matches, int inputLength)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(inputLength, 0);

        _matches = matches;
        _inputLength = inputLength;
        _position = 0;
        _active = true;
        _aborted = false;
        _current = default;
    }

    /// <summary>Gets the current token, a range of the input; it is valid after <see cref="MoveNext"/> returned <see langword="true"/>.</summary>
    public readonly TextRange Current => _current;

    /// <summary>Gets a value indicating whether the enumeration ended because a step ceiling was reached.</summary>
    public readonly bool IsAborted => _aborted;

    /// <summary>Returns this enumerator, so that it can be used by <see langword="foreach"/>.</summary>
    /// <returns>This enumerator.</returns>
    public readonly RegexTokenEnumerator GetEnumerator() => this;

    /// <summary>Advances to the next token.</summary>
    /// <returns><see langword="false"/> when no token is left, or a step ceiling was reached.</returns>
    public bool MoveNext()
    {
        if (!_active)
        {
            return false;
        }

        if (_matches.MoveNext())
        {
            var match = _matches.Current;
            _current = new TextRange(_position, match.Index - _position);
            _position = match.Index + match.Length;
            return true;
        }

        _active = false;
        _aborted = _matches.IsAborted;
        if (_aborted)
        {
            return false;
        }

        _current = new TextRange(_position, _inputLength - _position);
        return true;
    }

    /// <summary>Returns the rented array to the pool; calling it again has no effect.</summary>
    public void Dispose() => _matches.Dispose();
}
