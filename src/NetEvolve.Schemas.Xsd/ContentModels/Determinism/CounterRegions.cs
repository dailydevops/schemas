namespace NetEvolve.Schemas.Xsd.ContentModels.Determinism;

using System;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xsd.ContentModels.Automaton;

/// <summary>
/// The classes of values one counter takes, cut at the points where a guard changes its answer (design #146 DP-8): a count
/// below <c>min - 1</c>, exactly <c>min - 1</c>, from <c>min</c> up to <c>max - 2</c>, exactly <c>max - 1</c>, and exactly
/// <c>max</c>. A class that is empty for the bounds is left out, so small bounds collapse to single values and huge bounds
/// keep at most five classes. An unbounded counter saturates at its cap, so its classes end there.
/// </summary>
internal sealed class CounterRegions
{
    /// <summary>The largest number of classes of one counter.</summary>
    internal const int MaxCount = 5;

    private readonly long[] _low;
    private readonly long[] _high;
    private readonly long _cap;

    /// <summary>Creates the classes of one counter.</summary>
    /// <param name="min">The minimum the exit guard needs, one when there is none.</param>
    /// <param name="limit">The maximum the iterate guard needs, <see cref="AutomatonParticle.Unbounded"/> when unbounded.</param>
    /// <param name="cap">The largest value the counter takes, at least one.</param>
    internal CounterRegions(long min, long limit, long cap)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(cap, 1L);

        _cap = cap;

        // The start of every class; a cut point that falls outside one to cap, or repeats, makes no class.
        long[] cuts = [1, min - 1, min, limit - 1, limit];
        Array.Sort(cuts);
        var starts = new long[MaxCount];
        var count = 0;
        foreach (var cut in cuts)
        {
            if (cut >= 1 && cut <= cap && (count == 0 || starts[count - 1] != cut))
            {
                starts[count++] = cut;
            }
        }

        _low = new long[count];
        _high = new long[count];
        for (var i = 0; i < count; i++)
        {
            _low[i] = starts[i];
            _high[i] = i + 1 < count ? starts[i + 1] - 1 : cap;
        }
    }

    /// <summary>Gets the number of classes.</summary>
    internal int Count => _low.Length;

    /// <summary>Gets the smallest value of a class.</summary>
    /// <param name="region">The class.</param>
    /// <returns>The value.</returns>
    internal long Low(int region) => _low[region];

    /// <summary>Gets the largest value of a class.</summary>
    /// <param name="region">The class.</param>
    /// <returns>The value.</returns>
    internal long High(int region) => _high[region];

    /// <summary>Gets the class of a value.</summary>
    /// <param name="value">The value, at least one; a value above the cap is the cap.</param>
    /// <returns>The class.</returns>
    internal int IndexOf(long value)
    {
        var last = _low.Length - 1;
        for (var i = 0; i < last; i++)
        {
            if (value <= _high[i])
            {
                return i;
            }
        }

        return last;
    }

    /// <summary>Gets the classes an iteration reaches from one class: those with a value of <c>c + 1</c>, saturating at the cap.</summary>
    /// <param name="region">The class before the iteration.</param>
    /// <param name="first">The first class reached.</param>
    /// <param name="last">The last class reached.</param>
    internal void Successors(int region, out int first, out int last)
    {
        first = IndexOf(Math.Min(_low[region] + 1, _cap));
        last = IndexOf(Math.Min(_high[region] + 1, _cap));
    }
}
