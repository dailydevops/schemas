namespace NetEvolve.Schemas.Xml.Regex.Matching;

using System;
using System.Collections.Generic;
using System.Threading;
using NetEvolve.Schemas.Xml.Regex.Contracts;

// The deterministic fast path: the automaton states are built from the program on demand, and the transitions on ASCII
// characters are cached. An input outside ASCII, or a cache that is full, hands the match to the simulation. The XSD dialect
// matches the whole input; the XPath dialect searches, so every state also holds the threads of a match that starts at the
// next position, and a state that holds the end of the program answers at once.
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
        public readonly bool AcceptsAtEnd;
        public readonly DfaState?[] Next = new DfaState?[DfaAlphabet];

        public DfaState(int[] counters, bool accepting, bool acceptsAtEnd)
        {
            Counters = counters;
            Accepting = accepting;
            AcceptsAtEnd = acceptsAtEnd;
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

        public DfaState? Intern(int[] counters, bool accepting, bool acceptsAtEnd, bool start)
        {
            // The start state of a search is the only one at the start of the input, which the closure after an end anchor sees.
            var key = (start ? "^" : string.Empty) + string.Join(",", counters);
            if (_states.TryGetValue(key, out var existing))
            {
                return existing;
            }

            if (_states.Count >= MaxDfaStates)
            {
                _full = true;
                return null;
            }

            var created = new DfaState(counters, accepting, acceptsAtEnd);
            _states.Add(key, created);
            return created;
        }

        public void SetStart(DfaState start) => _start = start;
    }

    private readonly Dfa? _dfa;

    // Whether the boolean match can run on the automaton.
    internal bool IsDeterministic => _dfa is not null;

    private bool IsSearch => _dialect == RegexDialect.XPath;

    // A program runs on the automaton when no instruction needs more than the position of the thread: no capture, loop or back
    // reference instruction, and no anchor of the multi-line mode, which looks at the neighbors of the position.
    private static bool CanRunDeterministically(Instruction[] program)
    {
        foreach (var instruction in program)
        {
            switch (instruction.Opcode)
            {
                case Opcode.Save:
                case Opcode.Mark:
                case Opcode.Progress:
                case Opcode.BackRef:
                    return false;
                case Opcode.AssertStart when instruction.A != 0:
                case Opcode.AssertEnd when instruction.A != 0:
                    return false;
                default:
                    break;
            }
        }

        return true;
    }

    // The answer of the deterministic path, or null when the simulation must decide.
    private RegexMatchResult? MatchDeterministic(ReadOnlySpan<char> input)
    {
        var dfa = _dfa;
        if (dfa is null)
        {
            return null;
        }

        var state = dfa.Start ?? BuildStart(dfa);
        if (IsSearch && state.Accepting)
        {
            return RegexMatchResult.Match;
        }

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

            if (IsSearch && next.Accepting)
            {
                return RegexMatchResult.Match;
            }

            state = next;
        }

        return state.AcceptsAtEnd ? RegexMatchResult.Match : RegexMatchResult.NoMatch;
    }

    // The first state always fits: the cache is empty.
    private DfaState BuildStart(Dfa dfa)
    {
        lock (dfa.Gate)
        {
            var start = Intern(dfa, Closure([0], true, false), true)!;
            dfa.SetStart(start);
            return start;
        }
    }

    private DfaState? BuildTransition(Dfa dfa, DfaState from, char c)
    {
        // A full cache still serves the cached transitions; only a missing one is left to the simulation.
        if (dfa.IsFull)
        {
            return null;
        }

        lock (dfa.Gate)
        {
            var seeds = new List<int>();
            if (IsSearch)
            {
                // A match may also start at the next position.
                seeds.Add(0);
            }

            foreach (var pc in from.Counters)
            {
                if (Accepts(_program[pc], c))
                {
                    seeds.Add(pc + 1);
                }
            }

            var target = Intern(dfa, Closure(seeds, false, false), false);
            if (target is not null)
            {
                // The release write publishes the target state (its counters) before readers can see the slot; readers
                // load the slot without a lock and only dereference what they read, which the memory model orders.
                Volatile.Write(ref from.Next[c], target);
            }

            return target;
        }
    }

    private DfaState? Intern(Dfa dfa, int[] counters, bool start)
    {
        var accepting = false;
        foreach (var pc in counters)
        {
            accepting |= _program[pc].Opcode == Opcode.Match;
        }

        // At the end of the input an end anchor holds: the threads that wait for it move on.
        var acceptsAtEnd = accepting;
        if (IsSearch && !accepting)
        {
            foreach (var pc in Closure(counters, start, true))
            {
                acceptsAtEnd |= _program[pc].Opcode == Opcode.Match;
            }
        }

        return dfa.Intern(counters, accepting, acceptsAtEnd, start && IsSearch);
    }

    // The sorted counters of the instructions that consume a character, wait for the end of the input, or end the program,
    // reachable from the seeds through jumps and splits. A start anchor holds only at the start of the input; an end anchor
    // waits in the state unless the input ends here.
    private int[] Closure(IReadOnlyList<int> seeds, bool atStart, bool atEnd)
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
                case Opcode.AssertStart when atStart:
                case Opcode.AssertEnd when atEnd:
                    stack.Push(pc + 1);
                    break;
                case Opcode.AssertStart:
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
