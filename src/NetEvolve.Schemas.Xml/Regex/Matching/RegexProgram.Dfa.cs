namespace NetEvolve.Schemas.Xml.Regex.Matching;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Xml.Regex.Contracts;

// The deterministic fast path of the XSD dialect: the automaton states are built from the program on demand, and the
// transitions on ASCII characters are cached. An input outside ASCII, or a cache that is full, hands the match to the simulation.
public sealed partial class RegexProgram
{
    // Characters below this value have a cached transition.
    private const int DfaAlphabet = 128;

    // The number of automaton states one program keeps; the next state is not built and the simulation takes over.
    private const int MaxDfaStates = 256;

    // One state: the sorted program counters of the threads that wait for a character or have reached the end. The transition
    // slots are written at most once each, with the final reference, so concurrent readers see null or the target.
    private sealed class DfaState
    {
        public readonly int[] Counters;
        public readonly bool Accepting;
        public readonly DfaState?[] Next = new DfaState?[DfaAlphabet];

        public DfaState(int[] counters, bool accepting)
        {
            Counters = counters;
            Accepting = accepting;
        }

        public bool IsDead => Counters.Length == 0;
    }

    // The states of a program; only the building of a state takes the lock, matching never does.
    private sealed class Dfa
    {
        private readonly object _gate = new();
        private readonly Dictionary<string, DfaState> _states = [];
        private volatile DfaState? _start;
        private volatile bool _full;

        public bool IsFull => _full;

        public DfaState? Start => _start;

        public object Gate => _gate;

        public DfaState? Intern(int[] counters, bool accepting)
        {
            var key = string.Join(",", counters);
            if (_states.TryGetValue(key, out var existing))
            {
                return existing;
            }

            if (_states.Count >= MaxDfaStates)
            {
                _full = true;
                return null;
            }

            var created = new DfaState(counters, accepting);
            _states.Add(key, created);
            return created;
        }

        public void SetStart(DfaState start) => _start = start;
    }

    private readonly Dfa? _dfa;

    // The answer of the deterministic path, or null when the simulation must decide.
    private RegexMatchResult? MatchDeterministic(ReadOnlySpan<char> input)
    {
        var dfa = _dfa;
        if (dfa is null || dfa.IsFull)
        {
            return null;
        }

        var state = dfa.Start ?? BuildStart(dfa);

        for (var i = 0; i < input.Length; i++)
        {
            var c = input[i];
            if (c >= DfaAlphabet)
            {
                return null;
            }

            var next = state.Next[c] ?? BuildTransition(dfa, state, c);
            if (next is null)
            {
                return null;
            }

            if (next.IsDead)
            {
                return RegexMatchResult.NoMatch;
            }

            state = next;
        }

        return state.Accepting ? RegexMatchResult.Match : RegexMatchResult.NoMatch;
    }

    // The first state always fits: the cache is empty.
    private DfaState BuildStart(Dfa dfa)
    {
        lock (dfa.Gate)
        {
            var start = Intern(dfa, Closure([0]))!;
            dfa.SetStart(start);
            return start;
        }
    }

    private DfaState? BuildTransition(Dfa dfa, DfaState from, char c)
    {
        lock (dfa.Gate)
        {
            var seeds = new List<int>();
            foreach (var pc in from.Counters)
            {
                if (Accepts(_program[pc], c))
                {
                    seeds.Add(pc + 1);
                }
            }

            var target = Intern(dfa, Closure(seeds));
            if (target is not null)
            {
                from.Next[c] = target;
            }

            return target;
        }
    }

    private DfaState? Intern(Dfa dfa, int[] counters)
    {
        var accepting = false;
        foreach (var pc in counters)
        {
            accepting |= _program[pc].Opcode == Opcode.Match;
        }

        return dfa.Intern(counters, accepting);
    }

    // The sorted counters of the instructions that consume a character or end the program, reachable from the seeds through
    // jumps and splits.
    private int[] Closure(IReadOnlyList<int> seeds)
    {
        var seen = new bool[_program.Length];
        var stack = new Stack<int>(seeds);
        var result = new List<int>();
        while (stack.Count > 0)
        {
            var pc = stack.Pop();
            if (seen[pc])
            {
                continue;
            }

            seen[pc] = true;
            var instruction = _program[pc];
            switch (instruction.Opcode)
            {
                case Opcode.Split:
                    stack.Push(instruction.A);
                    stack.Push(instruction.B);
                    break;
                case Opcode.Jump:
                    stack.Push(instruction.A);
                    break;
                default:
                    result.Add(pc);
                    break;
            }
        }

        result.Sort();
        return [.. result];
    }
}
