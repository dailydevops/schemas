namespace NetEvolve.Schemas.Xsd.ContentModels.Automaton;

using System;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.Contracts;

/// <summary>
/// The immutable position automaton of one content model with counters instead of copied states (design #146 DP-7).
/// Its size is one entry per position, transition, and counted node, never per occurrence.
/// </summary>
/// <remarks>
/// Index 0 of the per-source tables is the start state; position <c>p</c> is at index <c>p + 1</c>.
/// </remarks>
internal sealed class CountingAutomaton : ICompiledContentModel
{
    private readonly int[][] _names;
    private readonly IWildcard?[] _wildcards;
    private readonly int[] _sourceStart;
    private readonly AutomatonTransition[] _transitions;
    private readonly AutomatonAction[] _actions;
    private readonly int[] _finalStart;
    private readonly int[] _finalCount;
    private readonly long[] _slotMinimums;

    // Counter-free automata only: per source, the (name id, target) pairs and the wildcard targets in transition order.
    private readonly int[] _lookupStart = [];
    private readonly int[] _lookupNames = [];
    private readonly int[] _lookupTargets = [];
    private readonly int[] _wildStart = [];
    private readonly int[] _wildTargets = [];

    internal CountingAutomaton(
        int[][] names,
        IWildcard?[] wildcards,
        int[] sourceStart,
        AutomatonTransition[] transitions,
        AutomatonAction[] actions,
        int[] finalStart,
        int[] finalCount,
        int counterSlots,
        long[] slotMinimums
    )
    {
        _names = names;
        _wildcards = wildcards;
        _sourceStart = sourceStart;
        _transitions = transitions;
        _actions = actions;
        _finalStart = finalStart;
        _finalCount = finalCount;
        CounterSlots = counterSlots;
        _slotMinimums = slotMinimums;

        if (counterSlots == 0)
        {
            BuildLookup(out _lookupStart, out _lookupNames, out _lookupTargets, out _wildStart, out _wildTargets);
        }
    }

    /// <inheritdoc />
    public int PositionCount => _names.Length;

    /// <inheritdoc />
    public int CounterSlots { get; }

    /// <summary>Gets the number of transitions.</summary>
    public int TransitionCount => _transitions.Length;

    /// <summary>Creates the mutable run state for one element frame.</summary>
    /// <returns>The run.</returns>
    /// <remarks>
    /// The run owns its configuration buffers; pooling runs per session is left to the validator (XSD-13).
    /// </remarks>
    public CountingRun CreateRun() => new(this);

    private void BuildLookup(
        out int[] lookupStart,
        out int[] lookupNames,
        out int[] lookupTargets,
        out int[] wildStart,
        out int[] wildTargets
    )
    {
        var sources = _sourceStart.Length - 1;
        var nameCount = 0;
        var wildCount = 0;
        foreach (var transition in _transitions)
        {
            if (_wildcards[transition.Target] is null)
            {
                nameCount += _names[transition.Target].Length;
            }
            else
            {
                wildCount++;
            }
        }

        lookupStart = new int[sources + 1];
        wildStart = new int[sources + 1];
        lookupNames = new int[nameCount];
        lookupTargets = new int[nameCount];
        wildTargets = new int[wildCount];
        var n = 0;
        var w = 0;
        for (var source = 0; source < sources; source++)
        {
            lookupStart[source] = n;
            wildStart[source] = w;
            for (var t = _sourceStart[source]; t < _sourceStart[source + 1]; t++)
            {
                var target = _transitions[t].Target;
                if (_wildcards[target] is not null)
                {
                    wildTargets[w++] = target;
                    continue;
                }

                foreach (var id in _names[target])
                {
                    lookupNames[n] = id;
                    lookupTargets[n++] = target;
                }
            }
        }

        lookupStart[sources] = n;
        wildStart[sources] = w;
    }

    /// <summary>Finds the first element target of a source, in transition order, that accepts the name id.</summary>
    internal int FirstElementTarget(int sourceIndex, int schemaNameId)
    {
        var start = _lookupStart[sourceIndex];
        var hit = _lookupNames.AsSpan(start, _lookupStart[sourceIndex + 1] - start).IndexOf(schemaNameId);
        return hit < 0 ? -1 : _lookupTargets[start + hit];
    }

    /// <summary>Finds the first wildcard target of a source, in transition order, that allows the name.</summary>
    internal int FirstWildcardTarget(int sourceIndex, in XPathQName name)
    {
        for (var i = _wildStart[sourceIndex]; i < _wildStart[sourceIndex + 1]; i++)
        {
            var target = _wildTargets[i];
            if (_wildcards[target]!.Allows(name))
            {
                return target;
            }
        }

        return -1;
    }

    internal AutomatonTransition GetTransition(int index) => _transitions[index];

    internal int SourceStart(int sourceIndex) => _sourceStart[sourceIndex];

    internal int SourceEnd(int sourceIndex) => _sourceStart[sourceIndex + 1];

    internal AutomatonAction GetAction(int index) => _actions[index];

    /// <summary>Gets the minimum occurrence of the counted node a counter slot belongs to.</summary>
    internal long SlotMinimum(int slot) => _slotMinimums[slot];

    internal bool IsFinalSource(int sourceIndex, out int actionStart, out int actionCount)
    {
        actionStart = _finalStart[sourceIndex];
        actionCount = _finalCount[sourceIndex];
        return actionStart >= 0;
    }

    internal bool IsWildcard(int position) => _wildcards[position] is not null;

    internal int[] NamesOf(int position) => _names[position];

    internal bool Matches(int position, int schemaNameId, in XPathQName name)
    {
        var wildcard = _wildcards[position];
        if (wildcard is null)
        {
            return schemaNameId >= 0 && Array.BinarySearch(_names[position], schemaNameId) >= 0;
        }

        // An empty local name marks "no expanded name supplied"; real names never have one.
        return name.LocalName.Length != 0 && wildcard.Allows(name);
    }
}
