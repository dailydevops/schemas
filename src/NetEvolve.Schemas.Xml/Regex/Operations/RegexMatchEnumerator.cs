namespace NetEvolve.Schemas.Xml.Regex.Operations;

using System;
using System.Buffers;
using System.Runtime.InteropServices;
using NetEvolve.Schemas.Xml.Regex.Contracts;

/// <summary>Enumerates the non-overlapping matches of a pattern in an input, leftmost first, without allocating per match.</summary>
/// <remarks>
/// It rents one small array from the shared pool: use it in a <see langword="foreach"/> loop or call <see cref="Dispose"/> after the last
/// use. A copy shares the rented array, so dispose exactly one of the copies (a <see langword="foreach"/> loop disposes its own copy). After a match of the empty string the search continues one code
/// point further, so the enumeration always ends. When a step ceiling is reached the enumeration ends and <see cref="IsAborted"/>
/// is <see langword="true"/>. Limitation: a capture group keeps the text of the last iteration of its enclosing loop in which it
/// took part.
/// </remarks>
[StructLayout(LayoutKind.Auto)]
public ref struct RegexMatchEnumerator
{
    private readonly RegexOperations? _operations;
    private readonly ReadOnlySpan<char> _input;
    private readonly RegexStepLimits? _limits;
    private readonly int _width;
    private int[]? _groups;
    private int _next;
    private bool _aborted;
    private bool _finished;

    internal RegexMatchEnumerator(RegexOperations operations, ReadOnlySpan<char> input, RegexStepLimits? limits)
    {
        ArgumentNullException.ThrowIfNull(operations);

        _operations = operations;
        _input = input;
        _limits = limits;
        _width = 2 * (operations.GroupCount + 1);
        _groups = ArrayPool<int>.Shared.Rent(_width);
        _next = 0;
        _aborted = false;
        _finished = false;
    }

    /// <summary>Gets the current match; it is valid after <see cref="MoveNext"/> returned <see langword="true"/> and until the next call.</summary>
    public readonly RegexMatch Current => new(_input, new ReadOnlySpan<int>(_groups, 0, _width));

    /// <summary>Gets a value indicating whether the enumeration ended because a step ceiling was reached.</summary>
    public readonly bool IsAborted => _aborted;

    /// <summary>Returns this enumerator, so that it can be used by <see langword="foreach"/>.</summary>
    /// <returns>This enumerator.</returns>
    public readonly RegexMatchEnumerator GetEnumerator() => this;

    /// <summary>Advances to the next match.</summary>
    /// <returns><see langword="false"/> when no match is left, or a step ceiling was reached.</returns>
    public bool MoveNext()
    {
        if (_finished || _groups is null)
        {
            return false;
        }

        if (_next > _input.Length)
        {
            _finished = true;
            return false;
        }

        var groups = new Span<int>(_groups, 0, _width);
        var result = _operations!.Program.FindGroups(_input, _next, _limits, groups);
        if (result != RegexMatchResult.Match)
        {
            _aborted = result == RegexMatchResult.Aborted;
            _finished = true;
            return false;
        }

        _next = groups[1] + (groups[1] == groups[0] ? Step(_input, groups[1]) : 0);
        return true;
    }

    /// <summary>Returns the rented array to the pool; calling it again has no effect.</summary>
    public void Dispose()
    {
        var groups = _groups;
        if (groups is null)
        {
            return;
        }

        _groups = null;
        ArrayPool<int>.Shared.Return(groups);
    }

    // One code point from the index; past the end of the input one position, which ends the enumeration.
    private static int Step(ReadOnlySpan<char> input, int index) =>
        index + 1 < input.Length && char.IsHighSurrogate(input[index]) && char.IsLowSurrogate(input[index + 1]) ? 2 : 1;
}
