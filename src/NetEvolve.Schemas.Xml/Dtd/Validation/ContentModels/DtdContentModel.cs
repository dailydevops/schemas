namespace NetEvolve.Schemas.Xml.Dtd.Validation.ContentModels;

using System;
using NetEvolve.Arguments;

/// <summary>
/// An element content model compiled once into a Glushkov position automaton (XML 1.0, Appendix E): one position per
/// name in the model, the follow positions of every position, and the positions that may end the content. Position zero
/// is the start; its follow positions are the first positions of the model.
/// </summary>
/// <remarks>
/// Matching is a set simulation over the positions, so the accepted language does not depend on whether the model is
/// deterministic. The model is immutable; the caller owns the scratch memory that <see cref="Step"/> uses.
/// </remarks>
internal sealed class DtdContentModel
{
    /// <summary>The position before the first child.</summary>
    internal const int StartPosition = 0;

    private readonly int[] _names;
    private readonly int[] _offsets;
    private readonly int[] _targets;
    private readonly bool[] _isLast;

    /// <summary>Initializes a new instance of the <see cref="DtdContentModel"/> class.</summary>
    /// <param name="names">The name symbol of every position; index zero is the start and unused.</param>
    /// <param name="offsets">The start of the follow positions of every position in <paramref name="targets"/>, with one more entry at the end.</param>
    /// <param name="targets">The follow positions of all positions, without duplicates per position.</param>
    /// <param name="isLast">Whether content may end after each position; index zero is set when empty content matches.</param>
    /// <param name="ambiguousName">The symbol of a name that two follow positions share, <c>-1</c> when the model is deterministic.</param>
    internal DtdContentModel(int[] names, int[] offsets, int[] targets, bool[] isLast, int ambiguousName)
    {
        ArgumentNullException.ThrowIfNull(names);
        ArgumentNullException.ThrowIfNull(offsets);
        ArgumentNullException.ThrowIfNull(targets);
        ArgumentNullException.ThrowIfNull(isLast);

        _names = names;
        _offsets = offsets;
        _targets = targets;
        _isLast = isLast;
        AmbiguousName = ambiguousName;
    }

    /// <summary>Gets the number of positions, the start excluded.</summary>
    internal int PositionCount => _names.Length - 1;

    /// <summary>Gets the symbol of a name that two follow positions share (Appendix E), <c>-1</c> when the model is deterministic.</summary>
    internal int AmbiguousName { get; }

    /// <summary>Gets a value indicating whether the model is deterministic (Appendix E).</summary>
    internal bool IsDeterministic => AmbiguousName < 0;

    /// <summary>Computes the positions reached from <paramref name="current"/> by a child with the given name.</summary>
    /// <param name="current">The current positions.</param>
    /// <param name="name">The name symbol of the child.</param>
    /// <param name="next">Receives the reached positions; at least <see cref="PositionCount"/> plus one long.</param>
    /// <param name="mark">Scratch memory, all zero on entry and on return; at least <see cref="PositionCount"/> plus one long.</param>
    /// <returns>The number of reached positions; zero when the child does not match.</returns>
    internal int Step(ReadOnlySpan<int> current, int name, Span<int> next, byte[] mark)
    {
        ArgumentNullException.ThrowIfNull(mark);
        ArgumentOutOfRangeException.ThrowIfLessThan(next.Length, _names.Length);
        ArgumentOutOfRangeException.ThrowIfLessThan(mark.Length, _names.Length);

        var count = 0;
        foreach (var position in current)
        {
            var end = _offsets[position + 1];
            for (var i = _offsets[position]; i < end; i++)
            {
                var target = _targets[i];
                if (_names[target] == name && mark[target] == 0)
                {
                    mark[target] = 1;
                    next[count++] = target;
                }
            }
        }

        for (var i = 0; i < count; i++)
        {
            mark[next[i]] = 0;
        }

        return count;
    }

    /// <summary>Gets a value indicating whether the content may end at the given positions.</summary>
    /// <param name="current">The current positions.</param>
    /// <returns><see langword="true"/> when one of the positions may end the content.</returns>
    internal bool IsAccepting(ReadOnlySpan<int> current)
    {
        foreach (var position in current)
        {
            if (_isLast[position])
            {
                return true;
            }
        }

        return false;
    }
}
