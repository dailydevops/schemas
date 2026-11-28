namespace NetEvolve.Schemas.Xsd.ContentModels.Determinism;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Xsd.ContentModels.Automaton;

/// <summary>
/// Explores the states of a counting automaton with every counter abstracted to its class (design #146 DP-8) and collects
/// the pairs of distinct positions that compete for one element from one state. A state is a source and the classes of the
/// counters that are still read; the number of states depends on the nesting of counters, never on their bounds.
/// </summary>
/// <remarks>
/// <para>
/// Limitations: the exploration stops at <see cref="MaxStates"/> states, an internal, non-configurable ceiling, and
/// when a source keeps more than <see cref="MaxLiveCounters"/> counters alive; the caller reports it. The equivalence of
/// the class abstraction with the definition on the unfolded model is argued in the design and pinned by a
/// differential test, not proven. A counter that is written before it is read again is dropped from the state, so counters
/// of finished siblings do not multiply the states.
/// </para>
/// </remarks>
internal sealed class RegionAnalysis
{
    /// <summary>
    /// The ceiling of explored states per content model. It is at least the positions the compiler accepts times the classes
    /// of one counter, so a flat model at the position ceiling is never rejected by it.
    /// </summary>
    internal const int MaxStates = 1 << 20;

    /// <summary>The ceiling of counters that one source reads again later; a class takes three bits of the state key.</summary>
    internal const int MaxLiveCounters = 15;

    private const int RegionBits = 3;
    private const int SourceShift = RegionBits * MaxLiveCounters;
    private const long RegionMask = (1L << RegionBits) - 1;

    private readonly CountingAutomaton _model;
    private readonly ParticleOverlap _overlap;
    private readonly CounterRegions?[] _regions;
    private readonly int[][] _live;
    private readonly bool _tooManyLiveCounters;
    private readonly HashSet<long> _visited = [];
    private readonly Queue<long> _queue = new();
    private readonly int[] _stamp;
    private readonly List<int> _targets = [];
    private readonly List<long> _named = [];
    private readonly HashSet<long> _pairSet = [];
    private int _stateStamp;
    private bool _ceilingExceeded;

    internal RegionAnalysis(CountingAutomaton model, ParticleOverlap overlap)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(overlap);

        _model = model;
        _overlap = overlap;
        _regions = ScanCounters(model);
        _live = ComputeLiveCounters(model, out _tooManyLiveCounters);

        _stamp = new int[model.PositionCount];
        for (var i = 0; i < _stamp.Length; i++)
        {
            _stamp[i] = -1;
        }
    }

    /// <summary>Gets the competing pairs, each once, as <c>first &lt;&lt; 32 | second</c> with the smaller position first.</summary>
    internal List<long> Pairs { get; } = [];

    /// <summary>Explores every reachable state.</summary>
    /// <returns><see langword="false"/> when a ceiling stopped the exploration before it was complete.</returns>
    internal bool Explore()
    {
        if (_tooManyLiveCounters)
        {
            return false;
        }

        _ = _visited.Add(0);
        _queue.Enqueue(0);
        while (_queue.Count > 0 && !_ceilingExceeded)
        {
            var key = _queue.Dequeue();
            Process((int)(key >> SourceShift), key & ((1L << SourceShift) - 1));
        }

        return !_ceilingExceeded;
    }

    /// <summary>Reads the bounds of every counter from the guards of the transitions.</summary>
    private static CounterRegions?[] ScanCounters(CountingAutomaton model)
    {
        var slots = model.CounterSlots;
        var min = new long[slots];
        var limit = new long[slots];
        var cap = new long[slots];
        var known = new bool[slots];
        for (var s = 0; s < slots; s++)
        {
            min[s] = 1;
            limit[s] = AutomatonParticle.Unbounded;
            cap[s] = 1;
        }

        for (var t = 0; t < model.TransitionCount; t++)
        {
            var transition = model.GetTransition(t);
            for (var a = transition.ActionStart; a < transition.ActionStart + transition.ActionCount; a++)
            {
                var action = model.GetAction(a);
                known[action.Slot] = true;
                if (action.Kind == AutomatonActionKind.Exit)
                {
                    min[action.Slot] = action.Limit;
                    cap[action.Slot] = Math.Max(cap[action.Slot], action.Limit);
                }
                else if (action.Kind == AutomatonActionKind.Iterate)
                {
                    limit[action.Slot] = action.Limit;
                    cap[action.Slot] = Math.Max(cap[action.Slot], action.Cap);
                }
            }
        }

        var regions = new CounterRegions?[slots];
        for (var s = 0; s < slots; s++)
        {
            regions[s] = known[s] ? new CounterRegions(min[s], limit[s], cap[s]) : null;
        }

        return regions;
    }

    /// <summary>
    /// For every source, the counters that are read again before they are written, as a sorted list: a counter is live at a
    /// source when a guard of one of its transitions reads it, or when the target keeps it live and the transition does not
    /// reset it. A list that outgrows <see cref="MaxLiveCounters"/> ends the computation.
    /// </summary>
    private static int[][] ComputeLiveCounters(CountingAutomaton model, out bool tooMany)
    {
        var live = new int[model.PositionCount + 1][];
        for (var i = 0; i < live.Length; i++)
        {
            live[i] = [];
        }

        var scratch = new List<int>();
        bool changed;
        do
        {
            changed = false;
            for (var s = live.Length - 1; s >= 0; s--)
            {
                for (var t = model.SourceStart(s); t < model.SourceEnd(s); t++)
                {
                    LiveBefore(model, model.GetTransition(t), live, scratch);
                    if (scratch.Count > MaxLiveCounters)
                    {
                        tooMany = true;
                        return live;
                    }

                    if (!IsSubset(scratch, live[s]))
                    {
                        live[s] = Union(live[s], scratch);
                        changed = true;
                    }
                }
            }
        } while (changed);

        tooMany = false;
        return live;
    }

    /// <summary>Gets the counters live before a transition: its guards, and what the target keeps live and it does not reset.</summary>
    private static void LiveBefore(
        CountingAutomaton model,
        AutomatonTransition transition,
        int[][] live,
        List<int> result
    )
    {
        result.Clear();
        var end = transition.ActionStart + transition.ActionCount;
        foreach (var slot in live[transition.Target + 1])
        {
            var reset = false;
            for (var a = transition.ActionStart; a < end && !reset; a++)
            {
                var action = model.GetAction(a);
                reset = action.Kind == AutomatonActionKind.Enter && action.Slot == slot;
            }

            if (!reset)
            {
                result.Add(slot);
            }
        }

        for (var a = transition.ActionStart; a < end; a++)
        {
            var action = model.GetAction(a);
            if (action.Kind != AutomatonActionKind.Enter && !result.Contains(action.Slot))
            {
                result.Add(action.Slot);
            }
        }

        result.Sort();
    }

    private static bool IsSubset(List<int> subset, int[] set)
    {
        foreach (var slot in subset)
        {
            if (Array.BinarySearch(set, slot) < 0)
            {
                return false;
            }
        }

        return true;
    }

    private static int[] Union(int[] set, List<int> other)
    {
        var merged = new List<int>(set);
        foreach (var slot in other)
        {
            if (Array.BinarySearch(set, slot) < 0)
            {
                merged.Add(slot);
            }
        }

        merged.Sort();
        return merged.ToArray();
    }

    private void Process(int source, long classes)
    {
        var liveSlots = _live[source];
        _targets.Clear();
        _stateStamp++;
        for (var t = _model.SourceStart(source); t < _model.SourceEnd(source); t++)
        {
            var transition = _model.GetTransition(t);
            if (!IsEnabled(transition, liveSlots, classes))
            {
                continue;
            }

            if (_stamp[transition.Target] != _stateStamp)
            {
                _stamp[transition.Target] = _stateStamp;
                _targets.Add(transition.Target);
            }

            Enqueue(transition, liveSlots, classes);
            if (_ceilingExceeded)
            {
                return;
            }
        }

        Compete();
    }

    private bool IsEnabled(AutomatonTransition transition, int[] liveSlots, long classes)
    {
        for (var a = transition.ActionStart; a < transition.ActionStart + transition.ActionCount; a++)
        {
            var action = _model.GetAction(a);
            if (action.Kind == AutomatonActionKind.Enter)
            {
                continue;
            }

            var regions = _regions[action.Slot]!;
            var region = ClassOf(liveSlots, classes, action.Slot);
            var passes =
                action.Kind == AutomatonActionKind.Exit
                    ? regions.Low(region) >= action.Limit
                    : regions.High(region) < action.Limit;
            if (!passes)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Adds the states a transition leads to: a reset counter is at one, an iterated one moves to the classes of <c>c + 1</c>.</summary>
    private void Enqueue(AutomatonTransition transition, int[] liveSlots, long classes)
    {
        var target = transition.Target + 1;
        var targetSlots = _live[target];
        long next = 0;
        var iterateAt = -1;
        var first = 0;
        var last = 0;
        for (var j = 0; j < targetSlots.Length; j++)
        {
            var slot = targetSlots[j];
            var action = Find(transition, slot);
            long region;
            if (action is { Kind: AutomatonActionKind.Enter })
            {
                region = 0;
            }
            else if (action is { Kind: AutomatonActionKind.Iterate })
            {
                iterateAt = j;
                _regions[slot]!.Successors(ClassOf(liveSlots, classes, slot), out first, out last);
                region = 0;
            }
            else
            {
                region = ClassOf(liveSlots, classes, slot);
            }

            next |= region << (RegionBits * j);
        }

        if (iterateAt < 0)
        {
            Add(target, next);
            return;
        }

        for (var region = first; region <= last && !_ceilingExceeded; region++)
        {
            Add(target, next | ((long)region << (RegionBits * iterateAt)));
        }
    }

    private void Add(int source, long classes)
    {
        var key = ((long)source << SourceShift) | classes;
        if (!_visited.Add(key))
        {
            return;
        }

        if (_visited.Count > MaxStates)
        {
            _ceilingExceeded = true;
            return;
        }

        _queue.Enqueue(key);
    }

    private AutomatonAction? Find(AutomatonTransition transition, int slot)
    {
        for (var a = transition.ActionStart; a < transition.ActionStart + transition.ActionCount; a++)
        {
            var action = _model.GetAction(a);
            if (action.Slot == slot)
            {
                return action;
            }
        }

        return null;
    }

    /// <summary>Gets the class of a counter in a state; a counter the state does not keep is at the first class.</summary>
    private static int ClassOf(int[] liveSlots, long classes, int slot)
    {
        for (var i = 0; i < liveSlots.Length; i++)
        {
            if (liveSlots[i] == slot)
            {
                return (int)((classes >> (RegionBits * i)) & RegionMask);
            }
        }

        return 0;
    }

    /// <summary>Records every pair of distinct enabled targets that can match the same element.</summary>
    private void Compete()
    {
        _named.Clear();
        foreach (var target in _targets)
        {
            if (_overlap.IsWildcard(target))
            {
                continue;
            }

            foreach (var id in _model.NamesOf(target))
            {
                _named.Add(((long)id << 32) | (uint)target);
            }
        }

        // Targets that share a schema name id, in runs of the sorted ids.
        _named.Sort();
        for (var i = 0; i < _named.Count; i++)
        {
            for (var j = i + 1; j < _named.Count && (_named[j] >> 32) == (_named[i] >> 32); j++)
            {
                Record((int)(uint)_named[i], (int)(uint)_named[j]);
            }
        }

        foreach (var wildcard in _targets)
        {
            if (!_overlap.IsWildcard(wildcard))
            {
                continue;
            }

            foreach (var other in _targets)
            {
                if (other != wildcard && Competes(wildcard, other))
                {
                    Record(wildcard, other);
                }
            }
        }
    }

    private bool Competes(int wildcard, int other) =>
        _overlap.IsWildcard(other)
            ? _overlap.WildcardsOverlap(wildcard, other)
            : _overlap.WildcardAllowsElement(wildcard, other);

    private void Record(int first, int second)
    {
        var low = Math.Min(first, second);
        var high = Math.Max(first, second);
        var pair = ((long)low << 32) | (uint)high;
        if (_pairSet.Add(pair))
        {
            Pairs.Add(pair);
        }
    }
}
