namespace NetEvolve.Schemas.Xsd.Derivation.Subsumption11;

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using NetEvolve.Arguments;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.ContentModels.Automaton;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Derivation.Particles10;
using NetEvolve.Schemas.Xsd.Elements;

/// <summary>
/// Decides whether every sequence of one counting automaton is accepted by another (Structures 1.1, 3.4.6.4 clause 1): the
/// product of the two automata is explored, and a state of the product is a position of each and a set of values of all counters
/// of both, kept as a <see cref="CounterZone"/>. A counter never takes more than its bounds, so the states do not depend on how
/// large the bounds are.
/// </summary>
/// <remarks>
/// <para>
/// The alphabet is every element name of the two models, every name that a wildcard disallows, and one name of each namespace
/// that occurs, which stands for all the other names of that namespace. A move of the restriction on a name is checked against
/// the base: where the guards of the moves of the base do not cover the zone, the restriction accepts a sequence that the base
/// does not. The base is read as deterministic, with the priority of XSD 1.1: a move on an element declaration is taken
/// before a move on a wildcard, and among equal ones the first in position order, so that a base that is not deterministic
/// (<c>cos-nonambig</c>) is read as the first move; the restriction is read as all its moves.
/// </para>
/// <para>
/// Limitations: a zone is a convex set, so after <see cref="WidenAfter"/> different zones of one pair of positions their hull is
/// widened; this is exact for the loops of one counter that two models run in step, and an over-approximation otherwise, where a
/// restriction can be reported that is valid. The search stops at a work limit and then has no verdict.
/// </para>
/// </remarks>
internal sealed class SubsumptionSearch
{
    /// <summary>The ceiling of counters of both models together; the cost of a zone grows with the cube of it.</summary>
    internal const int MaxVariables = 24;

    /// <summary>The number of different zones of one pair of positions that are kept before their hull is widened.</summary>
    internal const int WidenAfter = 8;

    /// <summary>The ceiling of the bounds that all zones of one check hold together, which limits its memory.</summary>
    internal const long MaxCells = 1L << 23;

    private static readonly Moves NoMoves = new([], new Dictionary<int, List<int>>());

    private readonly SubsumptionSide _restriction;
    private readonly SubsumptionSide _base;
    private readonly XPathQName[] _symbols;
    private readonly Func<int, int, ParticleViolation> _correspond;
    private readonly int _maxWork;
    private readonly int _offset;
    private readonly long[] _caps;
    private readonly Dictionary<long, PairZones> _pairs = [];
    private readonly Queue<Item> _queue = new();
    private readonly Dictionary<int, Moves> _restrictionMoves = [];
    private readonly Dictionary<int, Moves> _baseMoves = [];
    private readonly Dictionary<long, ParticleViolation> _verdicts = [];
    private int _work;
    private long _cells;
    private bool _exceeded;

    internal SubsumptionSearch(
        SubsumptionSide restriction,
        SubsumptionSide baseSide,
        XPathQName[] symbols,
        Func<int, int, ParticleViolation> correspond,
        int maxWork
    )
    {
        Argument.ThrowIfNull(restriction);
        Argument.ThrowIfNull(baseSide);
        Argument.ThrowIfNull(symbols);
        Argument.ThrowIfNull(correspond);
        Argument.ThrowIfLessThan(maxWork, 1);

        _restriction = restriction;
        _base = baseSide;
        _symbols = symbols;
        _correspond = correspond;
        _maxWork = maxWork;
        _offset = restriction.Caps.Length;
        _caps = new long[_offset + baseSide.Caps.Length];
        restriction.Caps.CopyTo(_caps, 0);
        baseSide.Caps.CopyTo(_caps, _offset);
    }

    /// <summary>Explores the product.</summary>
    /// <returns>The first violation in the order of the exploration, none when the restriction is accepted, and whether the work limit was hit.</returns>
    internal (ParticleViolation Violation, bool Exceeded) Run()
    {
        Add(0, 0, CounterZone.Origin(_caps.Length));
        while (_queue.Count > 0)
        {
            var work = _queue.Dequeue();
            var violation = Expand(work);
            if (violation.IsViolation || _exceeded)
            {
                return (violation, _exceeded && !violation.IsViolation);
            }
        }

        return (ParticleViolation.None, false);
    }

    /// <summary>Gets the units of work done so far.</summary>
    internal int Work => _work;

    private static CounterZone? Meet(CounterZone zone, Atom[] atoms)
    {
        var current = (CounterZone?)zone;
        for (var i = 0; i < atoms.Length && current is not null; i++)
        {
            if (atoms[i].Low > 0)
            {
                current = current.AtLeast(atoms[i].Variable, atoms[i].Low);
            }

            if (current is not null && atoms[i].High != CounterZone.Infinity)
            {
                current = current.AtMost(atoms[i].Variable, atoms[i].High);
            }
        }

        return current;
    }

    // The parts of a zone that fail at least one atom, as disjoint zones.
    private static List<CounterZone> Outside(CounterZone zone, Atom[] atoms)
    {
        var pieces = new List<CounterZone>();
        var rest = (CounterZone?)zone;
        for (var i = 0; i < atoms.Length && rest is not null; i++)
        {
            var atom = atoms[i];
            if (atom.Low > 0 && rest.AtMost(atom.Variable, atom.Low - 1) is { } below)
            {
                pieces.Add(below);
            }

            if (atom.High != CounterZone.Infinity && rest.AtLeast(atom.Variable, atom.High + 1) is { } above)
            {
                pieces.Add(above);
            }

            rest = Meet(rest, [atom]);
        }

        return pieces;
    }

    private static string Describe(IXsdComponent term) =>
        term is ElementDeclaration element ? "the element '" + element.Name.LocalName + "'" : "a wildcard";

    private static Atom[] AtomsOf(SubsumptionSide side, int offset, int start, int count)
    {
        var atoms = new List<Atom>();
        var automaton = side.Automaton!;
        for (var a = start; a < start + count; a++)
        {
            var action = automaton.GetAction(a);
            if (action.Kind == AutomatonActionKind.Exit)
            {
                atoms.Add(new Atom(offset + side.VariableOfSlot[action.Slot], action.Limit, CounterZone.Infinity));
            }
            else if (action.Kind == AutomatonActionKind.Iterate && action.Limit != AutomatonParticle.Unbounded)
            {
                atoms.Add(new Atom(offset + side.VariableOfSlot[action.Slot], 0, action.Limit - 1));
            }
        }

        return atoms.ToArray();
    }

    private static List<CounterZone> Apply(CounterZone zone, SubsumptionSide side, int offset, int start, int count)
    {
        var zones = new List<CounterZone> { zone };
        var automaton = side.Automaton!;
        for (var a = start; a < start + count; a++)
        {
            var action = automaton.GetAction(a);
            var variable = side.VariableOfSlot[action.Slot];
            if (variable == 0 || action.Kind == AutomatonActionKind.Exit)
            {
                continue;
            }

            variable += offset;
            var next = new List<CounterZone>(zones.Count);
            foreach (var current in zones)
            {
                if (action.Kind == AutomatonActionKind.Enter)
                {
                    next.Add(current.Assign(variable, 1));
                }
                else if (action.Limit != AutomatonParticle.Unbounded)
                {
                    next.Add(current.Increment(variable));
                }
                else
                {
                    // An unbounded counter stops at its minimum, so its values stay few: below it counts, from it on it stays.
                    if (current.AtMost(variable, action.Cap - 1) is { } counting)
                    {
                        next.Add(counting.Increment(variable));
                    }

                    if (current.AtLeast(variable, action.Cap) is { } saturated)
                    {
                        next.Add(saturated.Assign(variable, action.Cap));
                    }
                }
            }

            zones = next;
        }

        return zones;
    }

    private Moves MovesOf(Dictionary<int, Moves> cache, SubsumptionSide side, int source)
    {
        if (cache.TryGetValue(source, out var cached))
        {
            return cached;
        }

        var automaton = side.Automaton!;
        var bySymbol = new Dictionary<int, List<int>>();
        for (var t = automaton.SourceStart(source); t < automaton.SourceEnd(source); t++)
        {
            var target = automaton.GetTransition(t).Target;
            if (automaton.IsWildcard(target))
            {
                // A wildcard is asked about every symbol, which is charged to the work limit.
                if (Spend((_symbols.Length >> 6) + 1))
                {
                    return NoMoves;
                }

                var wildcard = (IWildcard)side.Terms[target];
                for (var s = 0; s < _symbols.Length; s++)
                {
                    if (wildcard.Allows(_symbols[s]))
                    {
                        Append(bySymbol, s, t);
                    }
                }
            }
            else
            {
                foreach (var id in automaton.NamesOf(target))
                {
                    Append(bySymbol, id, t);
                }
            }
        }

        var symbols = new int[bySymbol.Count];
        bySymbol.Keys.CopyTo(symbols, 0);
        Array.Sort(symbols);
        var moves = new Moves(symbols, bySymbol);
        cache.Add(source, moves);
        return moves;
    }

    private static void Append(Dictionary<int, List<int>> bySymbol, int symbol, int transition)
    {
        if (!bySymbol.TryGetValue(symbol, out var list))
        {
            list = [];
            bySymbol.Add(symbol, list);
        }

        list.Add(transition);
    }

    private bool Spend(int units)
    {
        _work += units;
        _exceeded |= _work > _maxWork;
        return _exceeded;
    }

    private void Enqueue(int restrictionSource, int baseSource, CounterZone zone)
    {
        var size = _caps.Length + 1L;
        _cells += size * size;
        _exceeded |= _cells > MaxCells;
        _queue.Enqueue(new Item(restrictionSource, baseSource, zone));
    }

    private void Add(int restrictionSource, int baseSource, CounterZone zone)
    {
        var key = ((long)restrictionSource << 32) | (uint)baseSource;
        if (!_pairs.TryGetValue(key, out var pair))
        {
            pair = new PairZones();
            _pairs.Add(key, pair);
        }

        if (pair.Widened is { } widened)
        {
            if (!zone.IsSubsetOf(widened))
            {
                pair.Widened = widened.Widen(zone, _caps);
                Enqueue(restrictionSource, baseSource, pair.Widened);
            }

            return;
        }

        foreach (var known in pair.Zones)
        {
            if (zone.IsSubsetOf(known))
            {
                return;
            }
        }

        pair.Zones.Add(zone);
        if (pair.Zones.Count <= WidenAfter)
        {
            Enqueue(restrictionSource, baseSource, zone);
            return;
        }

        var hull = pair.Zones[0];
        for (var i = 1; i < pair.Zones.Count; i++)
        {
            hull = hull.Hull(pair.Zones[i]);
        }

        pair.Zones.Clear();
        pair.Widened = hull;
        Enqueue(restrictionSource, baseSource, hull);
    }

    private ParticleViolation Expand(Item work)
    {
        _ = Spend(1);
        var violation = CheckEnd(work);
        if (violation.IsViolation)
        {
            return violation;
        }

        var moves = MovesOf(_restrictionMoves, _restriction, work.Restriction);
        var baseMoves = MovesOf(_baseMoves, _base, work.Base);
        if (_exceeded)
        {
            return ParticleViolation.None;
        }

        foreach (var symbol in moves.Symbols)
        {
            _ = baseMoves.BySymbol.TryGetValue(symbol, out var baseList);
            foreach (var t in moves.BySymbol[symbol])
            {
                if (Spend(1))
                {
                    return ParticleViolation.None;
                }

                violation = Step(work, t, baseList);
                if (violation.IsViolation)
                {
                    return violation;
                }
            }
        }

        return ParticleViolation.None;
    }

    // The restriction may end here: the base must be able to end for every value of the counters.
    private ParticleViolation CheckEnd(Item work)
    {
        if (!_restriction.Automaton!.IsFinalSource(work.Restriction, out var start, out var count))
        {
            return ParticleViolation.None;
        }

        var ends = Meet(work.Zone, AtomsOf(_restriction, 0, start, count));
        if (ends is null)
        {
            return ParticleViolation.None;
        }

        if (_base.Automaton!.IsFinalSource(work.Base, out var baseStart, out var baseCount))
        {
            if (Outside(ends, AtomsOf(_base, _offset, baseStart, baseCount)).Count == 0)
            {
                return ParticleViolation.None;
            }
        }

        var at = work.Restriction == 0 ? _restriction.Root : _restriction.Particles[work.Restriction - 1];
        return Fail(
            "cos-content-act-restrict.1",
            at,
            "The content of the restriction can end "
                + (work.Restriction == 0 ? "here" : "after " + Describe(_restriction.Terms[work.Restriction - 1]))
                + ", where the base content model requires more content."
        );
    }

    // One move of the restriction against the moves of the base for the same name.
    private ParticleViolation Step(Item work, int transition, List<int>? baseList)
    {
        var automaton = _restriction.Automaton!;
        var move = automaton.GetTransition(transition);
        var guard = Meet(work.Zone, AtomsOf(_restriction, 0, move.ActionStart, move.ActionCount));
        if (guard is null)
        {
            return ParticleViolation.None;
        }

        var remaining = new List<CounterZone> { guard };

        // An element declaration of the base is taken before a wildcard, and then the first move in position order.
        for (var tier = 0; tier < 2 && baseList is not null && remaining.Count > 0; tier++)
        {
            foreach (var t in baseList)
            {
                var baseMove = _base.Automaton!.GetTransition(t);
                if (_base.Automaton.IsWildcard(baseMove.Target) != (tier == 1))
                {
                    continue;
                }

                var atoms = AtomsOf(_base, _offset, baseMove.ActionStart, baseMove.ActionCount);
                var outside = new List<CounterZone>();
                foreach (var piece in remaining)
                {
                    if (Meet(piece, atoms) is { } covered)
                    {
                        var violation = Correspond(move.Target, baseMove.Target);
                        if (violation.IsViolation)
                        {
                            return violation;
                        }

                        Advance(covered, move, baseMove);
                    }

                    outside.AddRange(Outside(piece, atoms));
                }

                remaining = outside;
            }
        }

        return remaining.Count == 0
            ? ParticleViolation.None
            : Fail(
                "cos-content-act-restrict.1",
                _restriction.Particles[move.Target],
                "The restriction accepts "
                    + Describe(_restriction.Terms[move.Target])
                    + " where the base content model does not."
            );
    }

    private void Advance(CounterZone zone, AutomatonTransition move, AutomatonTransition baseMove)
    {
        foreach (var afterRestriction in Apply(zone, _restriction, 0, move.ActionStart, move.ActionCount))
        {
            foreach (var next in Apply(afterRestriction, _base, _offset, baseMove.ActionStart, baseMove.ActionCount))
            {
                Add(move.Target + 1, baseMove.Target + 1, next);
            }
        }
    }

    // The element declarations of two positions correspond, checked once for the pair.
    private ParticleViolation Correspond(int restrictionPosition, int basePosition)
    {
        var key = ((long)restrictionPosition << 32) | (uint)basePosition;
        if (!_verdicts.TryGetValue(key, out var violation))
        {
            violation = _correspond(restrictionPosition, basePosition);
            _verdicts.Add(key, violation);
        }

        return violation;
    }

    private static ParticleViolation Fail(string clause, Particle at, string message) =>
        new(XsdRuleCodes.CosContentActRestrict, clause, message, at.Location);

    [StructLayout(LayoutKind.Auto)]
    private readonly struct Atom
    {
        internal Atom(int variable, long low, long high)
        {
            Variable = variable;
            Low = low;
            High = high;
        }

        internal int Variable { get; }

        internal long Low { get; }

        internal long High { get; }
    }

    [StructLayout(LayoutKind.Auto)]
    private readonly struct Item
    {
        internal Item(int restriction, int baseSource, CounterZone zone)
        {
            Restriction = restriction;
            Base = baseSource;
            Zone = zone;
        }

        internal int Restriction { get; }

        internal int Base { get; }

        internal CounterZone Zone { get; }
    }

    private sealed class Moves
    {
        internal Moves(int[] symbols, Dictionary<int, List<int>> bySymbol)
        {
            Symbols = symbols;
            BySymbol = bySymbol;
        }

        internal int[] Symbols { get; }

        internal Dictionary<int, List<int>> BySymbol { get; }
    }

    private sealed class PairZones
    {
        internal List<CounterZone> Zones { get; } = [];

        internal CounterZone? Widened { get; set; }
    }
}
