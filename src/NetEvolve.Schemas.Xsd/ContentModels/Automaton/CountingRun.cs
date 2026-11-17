namespace NetEvolve.Schemas.Xsd.ContentModels.Automaton;

using System;
using NetEvolve.Arguments;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.Contracts;

/// <summary>
/// The run state of one element frame over a <see cref="CountingAutomaton"/>: a bounded set of configurations, each a
/// position and its counters (design #146 DP-8 sub-decision A1). Names stay deterministic for a valid model; only the
/// counters are nondeterministic, as in <c>(a{2,3}){2}</c>.
/// </summary>
/// <remarks>
/// <para>
/// Limitations: the set holds at most <see cref="MaxConfigurations"/> configurations, an internal, non-configurable
/// ceiling over the counted nesting. A step that would exceed it is rejected and sets <see cref="CeilingExceeded"/>;
/// the validator (XSD-13) reports it. The seam <see cref="IContentModelMatcher"/> carries no counters
/// (<see cref="ContentModelState"/> is a position and an offset), so this run is the matcher until the validator
/// decides between this overload and a change of the seam (design #146 DP-7 gap 2). The overloads taking an
/// <see cref="XPathQName"/> reach wildcards; the one with only the id never matches a wildcard.
/// </para>
/// <para>
/// A step allocates nothing once the buffers have grown to the working set; growth is amortized and capped.
/// </para>
/// </remarks>
internal sealed class CountingRun
{
    /// <summary>The ceiling of live configurations per frame.</summary>
    internal const int MaxConfigurations = 1024;

    private const int InitialConfigurations = 8;

    private readonly CountingAutomaton _automaton;
    private readonly int _slots;
    private int[] _positions = new int[InitialConfigurations];
    private long[] _counters;
    private int[] _nextPositions = new int[InitialConfigurations];
    private long[] _nextCounters;
    private int _count;

    internal CountingRun(CountingAutomaton automaton)
    {
        Argument.ThrowIfNull(automaton);

        _automaton = automaton;
        _slots = automaton.CounterSlots;
        _counters = new long[InitialConfigurations * _slots];
        _nextCounters = new long[InitialConfigurations * _slots];
        Reset();
    }

    /// <summary>Gets a value indicating whether a step was rejected because the configurations exceeded the ceiling.</summary>
    public bool CeilingExceeded { get; private set; }

    /// <summary>Returns to the state before the first child.</summary>
    public void Reset()
    {
        _count = 1;
        _positions[0] = -1;
        Array.Clear(_counters, 0, _slots);
        CeilingExceeded = false;
    }

    /// <summary>Tries to accept a child that is known by its schema name id only; wildcards never match.</summary>
    /// <param name="schemaNameId">The bound schema name id, <c>-1</c> when unknown.</param>
    /// <param name="matched">The matched particle when accepted.</param>
    /// <returns><see langword="true"/> when the child is accepted.</returns>
    public bool TryStep(int schemaNameId, out MatchedParticle matched) => TryStep(schemaNameId, default, out matched);

    /// <summary>Tries to accept a child element. A failed step leaves the run unchanged.</summary>
    /// <param name="schemaNameId">The bound schema name id, <c>-1</c> when the name is unknown to the schema.</param>
    /// <param name="name">The expanded name, used for wildcards.</param>
    /// <param name="matched">The matched particle when accepted.</param>
    /// <returns><see langword="true"/> when the child is accepted.</returns>
    public bool TryStep(int schemaNameId, XPathQName name, out MatchedParticle matched)
    {
        matched = default;
        if (CeilingExceeded || !Expand(schemaNameId, name, out var next, out var chosen))
        {
            return false;
        }

        // One particle attributes the child (element priority over a wildcard); configurations of others are dropped.
        var kept = 0;
        for (var i = 0; i < next; i++)
        {
            if (_nextPositions[i] != chosen)
            {
                continue;
            }

            if (kept != i)
            {
                _nextPositions[kept] = chosen;
                Array.Copy(_nextCounters, i * _slots, _nextCounters, kept * _slots, _slots);
            }

            kept++;
        }

        (_positions, _nextPositions) = (_nextPositions, _positions);
        (_counters, _nextCounters) = (_nextCounters, _counters);
        _count = kept;
        matched = new MatchedParticle(chosen, _automaton.IsWildcard(chosen));
        return true;
    }

    /// <summary>Determines whether the content may end now.</summary>
    /// <returns><see langword="true"/> when some configuration is final.</returns>
    public bool CanEnd()
    {
        for (var i = 0; i < _count; i++)
        {
            if (
                _automaton.IsFinalSource(_positions[i] + 1, out var start, out var count)
                && Passes(start, count, _counters, i * _slots)
            )
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Collects the schema name ids a child may have next, as many as fit.</summary>
    /// <param name="destination">The bounded destination.</param>
    /// <param name="wildcardExpected">Whether a wildcard particle may match next.</param>
    /// <returns>The number of ids written, without duplicates.</returns>
    public int CollectExpected(Span<int> destination, out bool wildcardExpected)
    {
        wildcardExpected = false;
        var written = 0;
        for (var i = 0; i < _count; i++)
        {
            var source = _positions[i] + 1;
            var end = _automaton.SourceEnd(source);
            for (var t = _automaton.SourceStart(source); t < end; t++)
            {
                var transition = _automaton.GetTransition(t);
                if (!Passes(transition.ActionStart, transition.ActionCount, _counters, i * _slots))
                {
                    continue;
                }

                if (_automaton.IsWildcard(transition.Target))
                {
                    wildcardExpected = true;
                    continue;
                }

                foreach (var id in _automaton.NamesOf(transition.Target))
                {
                    if (written == destination.Length)
                    {
                        return written;
                    }

                    if (!Contains(destination, written, id))
                    {
                        destination[written++] = id;
                    }
                }
            }
        }

        return written;
    }

    /// <summary>Fills the next buffer with every configuration the name leads to, including the other particles.</summary>
    private bool Expand(int schemaNameId, XPathQName name, out int next, out int chosen)
    {
        next = 0;
        chosen = -1;
        var chosenIsWildcard = false;
        for (var i = 0; i < _count; i++)
        {
            var source = _positions[i] + 1;
            var end = _automaton.SourceEnd(source);
            for (var t = _automaton.SourceStart(source); t < end; t++)
            {
                var transition = _automaton.GetTransition(t);
                if (!_automaton.Matches(transition.Target, schemaNameId, name))
                {
                    continue;
                }

                if (next == _nextPositions.Length && !GrowNext())
                {
                    CeilingExceeded = true;
                    return false;
                }

                var dst = next * _slots;
                Array.Copy(_counters, i * _slots, _nextCounters, dst, _slots);
                if (
                    !Passes(transition.ActionStart, transition.ActionCount, _counters, i * _slots)
                    || ApplyAndFindDuplicate(transition, i * _slots, dst, next)
                )
                {
                    continue;
                }

                _nextPositions[next++] = transition.Target;
                var isWildcard = _automaton.IsWildcard(transition.Target);
                if (chosen < 0 || (chosenIsWildcard && !isWildcard))
                {
                    chosen = transition.Target;
                    chosenIsWildcard = isWildcard;
                }
            }
        }

        return chosen >= 0;
    }

    private static bool Contains(ReadOnlySpan<int> values, int count, int id)
    {
        for (var i = 0; i < count; i++)
        {
            if (values[i] == id)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Runs the guards of a path against one configuration's counters without changing them.</summary>
    private bool Passes(int actionStart, int actionCount, long[] counters, int offset)
    {
        // The slots of one path are distinct, so every guard may read the source counters.
        for (var a = actionStart; a < actionStart + actionCount; a++)
        {
            var action = _automaton.GetAction(a);
            var passes = action.Kind switch
            {
                AutomatonActionKind.Exit => counters[offset + action.Slot] >= action.Limit,
                AutomatonActionKind.Iterate => counters[offset + action.Slot] < action.Limit,
                _ => true,
            };
            if (!passes)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Writes the counters after the path into the next buffer and tells whether that configuration is already there.</summary>
    /// <remarks>
    /// ponytail: linear duplicate scan, O(n^2) in the configurations; a hash set if profiling asks for it.
    /// </remarks>
    private bool ApplyAndFindDuplicate(AutomatonTransition transition, int srcOffset, int dstOffset, int count)
    {
        for (var a = transition.ActionStart; a < transition.ActionStart + transition.ActionCount; a++)
        {
            var action = _automaton.GetAction(a);
            if (action.Kind == AutomatonActionKind.Iterate)
            {
                _nextCounters[dstOffset + action.Slot] = Math.Min(_counters[srcOffset + action.Slot] + 1, action.Cap);
            }
            else if (action.Kind == AutomatonActionKind.Enter)
            {
                _nextCounters[dstOffset + action.Slot] = 1;
            }
        }

        for (var j = 0; j < count; j++)
        {
            if (_nextPositions[j] != transition.Target)
            {
                continue;
            }

            var same = true;
            for (var s = 0; s < _slots; s++)
            {
                if (_nextCounters[j * _slots + s] != _nextCounters[dstOffset + s])
                {
                    same = false;
                    break;
                }
            }

            if (same)
            {
                return true;
            }
        }

        return false;
    }

    private bool GrowNext()
    {
        if (_nextPositions.Length >= MaxConfigurations)
        {
            return false;
        }

        var capacity = Math.Min(_nextPositions.Length * 2, MaxConfigurations);
        Array.Resize(ref _nextPositions, capacity);
        Array.Resize(ref _nextCounters, capacity * _slots);
        return true;
    }
}
