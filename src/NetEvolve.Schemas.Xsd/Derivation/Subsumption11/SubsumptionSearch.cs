namespace NetEvolve.Schemas.Xsd.Derivation.Subsumption11;

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
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
/// does not. Both models are read with the priority that Unique Particle Attribution (Structures 1.1, 3.8.6.4) gives an element
/// declaration: a move on an element declaration is taken before a move on a wildcard, which only gets the values of the
/// counters for which no element declaration takes the name. Among the moves of one kind the base takes the first in position
/// order, so that a base that is not deterministic (<c>cos-nonambig</c>) is read as its first move, and the restriction takes
/// all of them.
/// </para>
/// <para>
/// A zone is a convex set, so the zones of one pair of positions are merged by their hull once there are more than
/// <see cref="WidenAfter"/> of them. The hull is exact when the zones are the points of a loop that counters run in step (each
/// step adds one to some of them): the widened zone then holds exactly the values that the loop reaches before a counter
/// stops it. Otherwise it is an over-approximation, which keeps a restriction that is valid from being rejected, but a
/// violation that is found from such a zone might be spurious, so it is not reported as one: the check has no verdict
/// (<see cref="SubsumptionVerdict.Undecided"/>). The search also stops at a work limit and at a ceiling of the memory of its zones.
/// </para>
/// <para>
/// Limitations: the values of counters that are not run in step, and more than <see cref="WidenAfter"/> per pair of positions,
/// are not decided; the restriction as a language is read with the priority of an element declaration, so a sequence that
/// the base could take only through a wildcard where an element declaration of the same name has priority is not accepted.
/// </para>
/// </remarks>
internal sealed class SubsumptionSearch
{
    /// <summary>The ceiling of counters of both models together; the cost of a zone grows with the cube of it.</summary>
    internal const int MaxVariables = 24;

    /// <summary>The number of different zones of one pair of positions that are kept before their hull is widened.</summary>
    internal const int WidenAfter = 64;

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
        ArgumentNullException.ThrowIfNull(restriction);
        ArgumentNullException.ThrowIfNull(baseSide);
        ArgumentNullException.ThrowIfNull(symbols);
        ArgumentNullException.ThrowIfNull(correspond);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxWork, 1);

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
        Add(0, 0, CounterZone.Origin(_caps.Length), true);
        while (_queue.Count > 0)
        {
            var work = _queue.Dequeue();
            var violation = Expand(work);

            // A violation that was found from a zone that is more than the reachable values might not be one.
            if (violation.IsViolation && !work.Exact)
            {
                return (ParticleViolation.None, true);
            }

            if (violation.IsViolation || _exceeded)
            {
                return (violation, _exceeded);
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

    private void Enqueue(int restrictionSource, int baseSource, CounterZone zone, bool exact)
    {
        var size = _caps.Length + 1L;
        _cells += size * size;
        _exceeded |= _cells > MaxCells;
        _queue.Enqueue(new Item(restrictionSource, baseSource, zone, exact));
    }

    // A zone is exact when it holds the reachable values and no others.
    private void Add(int restrictionSource, int baseSource, CounterZone zone, bool exact)
    {
        var key = ((long)restrictionSource << 32) | (uint)baseSource;
        if (!_pairs.TryGetValue(key, out var pair))
        {
            pair = new PairZones();
            _pairs.Add(key, pair);
        }

        pair.Exact &= exact;
        if (pair.Widened is { } widened)
        {
            if (!zone.IsSubsetOf(widened))
            {
                // The widened zone of a progression holds all its values; anything else it gets is an over-approximation.
                pair.Exact = false;
                pair.Widened = widened.Widen(zone, _caps);
                Enqueue(restrictionSource, baseSource, pair.Widened, false);
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

        // A zone that holds the ones kept before replaces them.
        _ = pair.Zones.RemoveAll(known => known.IsSubsetOf(zone));
        pair.Zones.Add(zone);
        if (pair.Zones.Count <= WidenAfter)
        {
            Enqueue(restrictionSource, baseSource, zone, exact);
            return;
        }

        var hull = pair.Zones[0];
        for (var i = 1; i < pair.Zones.Count; i++)
        {
            hull = hull.Hull(pair.Zones[i]);
        }

        // The values of a loop that two counters run in step are points on a line, and the hull of a line is exact; the
        // widened zone then reaches the largest values at once.
        if (NextPoint(pair.Zones) is { } next)
        {
            hull = hull.Widen(next, _caps);
        }
        else
        {
            pair.Exact = false;
        }

        pair.Zones.Clear();
        pair.Widened = hull;
        Enqueue(restrictionSource, baseSource, hull, pair.Exact);
    }

    // The point after the last of zones that are points of one line: each step adds zero or one to every counter.
    internal static CounterZone? NextPoint(IReadOnlyList<CounterZone> zones)
    {
        ArgumentNullException.ThrowIfNull(zones);

        long[]? previous = null;
        long[]? step = null;
        foreach (var zone in zones)
        {
            if (!zone.TryPoint(out var point))
            {
                return null;
            }

            if (previous is not null)
            {
                var difference = new long[point.Length];
                for (var v = 0; v < point.Length; v++)
                {
                    difference[v] = point[v] - previous[v];
                    if (difference[v] is not (0 or 1))
                    {
                        return null;
                    }
                }

                if (step is not null && !difference.AsSpan().SequenceEqual(step))
                {
                    return null;
                }

                step = difference;
            }

            previous = point;
        }

        if (step is null)
        {
            return null;
        }

        var next = new long[previous!.Length];
        for (var v = 0; v < next.Length; v++)
        {
            next[v] = previous[v] + step[v];
        }

        return CounterZone.Point(next);
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
            violation = ExpandSymbol(work, moves.BySymbol[symbol], baseList);
            if (violation.IsViolation || _exceeded)
            {
                return violation;
            }
        }

        return ParticleViolation.None;
    }

    // The moves of the restriction on one name. The restriction takes its element declarations before its wildcards, as the
    // base does: a wildcard only gets the values of the counters for which no element declaration of the restriction can take
    // the name.
    private ParticleViolation ExpandSymbol(Item work, List<int> list, List<int>? baseList)
    {
        var automaton = _restriction.Automaton!;
        var pieces = new List<CounterZone> { work.Zone };
        for (var tier = 0; tier < 2 && pieces.Count > 0; tier++)
        {
            foreach (var t in list)
            {
                var move = automaton.GetTransition(t);
                if (automaton.IsWildcard(move.Target) != (tier == 1))
                {
                    continue;
                }

                var atoms = AtomsOf(_restriction, 0, move.ActionStart, move.ActionCount);
                foreach (var piece in pieces)
                {
                    if (Meet(piece, atoms) is not { } guard)
                    {
                        continue;
                    }

                    if (Spend(1))
                    {
                        return ParticleViolation.None;
                    }

                    var violation = Step(work, guard, move, baseList);
                    if (violation.IsViolation)
                    {
                        return violation;
                    }
                }
            }

            if (tier == 0)
            {
                pieces = WithoutElements(pieces, list);
            }
        }

        return ParticleViolation.None;
    }

    // The values of the counters for which no element move of the list is enabled.
    private List<CounterZone> WithoutElements(List<CounterZone> pieces, List<int> list)
    {
        var automaton = _restriction.Automaton!;
        foreach (var t in list)
        {
            var move = automaton.GetTransition(t);
            if (automaton.IsWildcard(move.Target))
            {
                continue;
            }

            var atoms = AtomsOf(_restriction, 0, move.ActionStart, move.ActionCount);
            var outside = new List<CounterZone>();
            foreach (var piece in pieces)
            {
                outside.AddRange(Outside(piece, atoms));
            }

            pieces = outside;
        }

        return pieces;
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

    // One move of the restriction, for the values of the counters in the guard, against the moves of the base for the same name.
    private ParticleViolation Step(Item work, CounterZone guard, AutomatonTransition move, List<int>? baseList)
    {
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

                        Advance(covered, move, baseMove, work.Exact);
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

    private void Advance(CounterZone zone, AutomatonTransition move, AutomatonTransition baseMove, bool exact)
    {
        foreach (var afterRestriction in Apply(zone, _restriction, 0, move.ActionStart, move.ActionCount))
        {
            foreach (var next in Apply(afterRestriction, _base, _offset, baseMove.ActionStart, baseMove.ActionCount))
            {
                Add(move.Target + 1, baseMove.Target + 1, next, exact);
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
        internal Item(int restriction, int baseSource, CounterZone zone, bool exact)
        {
            Restriction = restriction;
            Base = baseSource;
            Zone = zone;
            Exact = exact;
        }

        internal int Restriction { get; }

        internal int Base { get; }

        internal CounterZone Zone { get; }

        internal bool Exact { get; }
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

        internal bool Exact { get; set; } = true;
    }
}
