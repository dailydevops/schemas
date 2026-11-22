namespace NetEvolve.Schemas.Xml.Regex.Matching;

using System;
using System.Buffers;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Regex.Contracts;
using NetEvolve.Schemas.Xml.Regex.Parsing;

// The capture bounds that the operations of DT-57 (Regex/Operations) need. They live in the folder of that work package, as a
// part of the compiled program, because they read its private instruction array; the search of the program stays as it is.
public sealed partial class RegexProgram
{
    // The program size times the slots of one thread caps the state of the capturing automaton, in integers per thread list.
    private const int MaxCaptureStateSize = 2_000_000;

    // True for a program compiled with capture instructions for the automaton, see TryCompileForOperations.
    private bool _recordsCaptures;

    // Compiles a tree for the operations: like TryCompile, but a pattern with capture groups and without back references also
    // gets the capture instructions, which only the backtracking machine would otherwise have.
    internal static RegexError TryCompileForOperations(RegexTree tree, out RegexProgram? program)
    {
        Argument.ThrowIfNull(tree);

        program = null;
        if (tree.Dialect != RegexDialect.XPath)
        {
            return RegexError.ConstructNotAllowed;
        }

        var nodes = tree.Nodes;
        if (CountGroups(nodes) == 0 || HasBackReference(nodes))
        {
            return TryCompile(tree, out program);
        }

        var emitter = new Emitter(nodes, true, tree.Options, true);
        emitter.Emit(tree.Root, 0);
        _ = emitter.Add(Opcode.Match, 0, 0);
        if (emitter.Error != RegexError.None)
        {
            return emitter.Error;
        }

        var instructions = emitter.ToArray();
        var captureSlots = 2 * (emitter.MaxGroup + 1);
        if ((long)instructions.Length * captureSlots > MaxCaptureStateSize)
        {
            return RegexError.LimitExceeded;
        }

        var sets = new RegexCharacterSet[tree.SetCount];
        for (var i = 0; i < sets.Length; i++)
        {
            sets[i] = tree.GetSet(i);
        }

        program = new RegexProgram(
            instructions,
            sets,
            tree.Dialect,
            false,
            captureSlots,
            captureSlots + emitter.LoopCount
        )
        {
            _recordsCaptures = true,
        };
        return RegexError.None;
    }

    // The highest group number of the capturing groups of a tree; groups are numbered without gaps.
    internal static int CountGroups(ReadOnlySpan<RegexNode> nodes)
    {
        var count = 0;
        for (var i = 0; i < nodes.Length; i++)
        {
            if (nodes[i].Kind == RegexNodeKind.Group && nodes[i].Value > count)
            {
                count = nodes[i].Value;
            }
        }

        return count;
    }

    // Finds the first match and the bounds of its capture groups: groups holds the start and the end position of group 0 (the
    // whole match), 1, 2, and so on, -1 for a group that took no part in the match. The program must come from
    // TryCompileForOperations and groups must have room for the groups of the pattern.
    internal RegexMatchResult FindGroups(
        ReadOnlySpan<char> input,
        int startIndex,
        RegexStepLimits? limits,
        Span<int> groups
    )
    {
        Argument.ThrowIfLessThan(startIndex, 0);
        Argument.ThrowIfGreaterThan(startIndex, input.Length);
        Argument.ThrowIfLessThan(groups.Length, _captureSlots);

        var effective = limits ?? RegexStepLimits.Default;
        groups.Fill(-1);
        if (_backtracking)
        {
            return BacktrackGroups(input, startIndex, effective.MaxBacktrackSteps, groups);
        }

        if (_recordsCaptures)
        {
            return SimulateGroups(input, startIndex, effective.MaxNfaSteps, groups);
        }

        var result = Search(input, startIndex, effective, out var index, out var length, out _);
        if (result == RegexMatchResult.Match)
        {
            groups[0] = index;
            groups[1] = index + length;
        }

        return result;
    }

    // The backtracking machine of Backtrack, keeping the slots of the match that was found.
    private RegexMatchResult BacktrackGroups(ReadOnlySpan<char> input, int startIndex, long maxSteps, Span<int> groups)
    {
        long steps = 0;
        var slots = ArrayPool<int>.Shared.Rent(_slotCount);
        var stack = ArrayPool<int>.Shared.Rent(3 * InitialStackEntries);
        try
        {
            for (var begin = startIndex; ; )
            {
                slots.AsSpan(0, _slotCount).Fill(-1);
                var result = TryAt(input, begin, maxSteps, ref steps, slots, ref stack, out var end);
                if (result == RegexMatchResult.Match)
                {
                    groups[0] = begin;
                    groups[1] = end;
                    for (var i = 2; i < _captureSlots; i++)
                    {
                        groups[i] = slots[i];
                    }

                    return result;
                }

                if (result == RegexMatchResult.Aborted || begin >= input.Length)
                {
                    return result;
                }

                _ = NextCodePoint(input, ref begin);
            }
        }
        finally
        {
            ArrayPool<int>.Shared.Return(slots);
            ArrayPool<int>.Shared.Return(stack);
        }
    }

    // The state of the capturing simulation: the thread lists with the capture slots of every thread, the slots of the thread being
    // followed, and the stack of the depth-first expansion.
    private ref struct CaptureThreads
    {
        public ThreadList Current;
        public ThreadList Next;
        public Span<int> CurrentCaptures;
        public Span<int> NextCaptures;
        public readonly Span<int> Work;
        public readonly Span<int> Pending;
        public long Steps;

        public CaptureThreads(Span<int> buffer, int size, int width)
        {
            Current = new ThreadList(buffer.Slice(0, size), buffer.Slice(size, size), buffer.Slice(2 * size, size));
            Next = new ThreadList(
                buffer.Slice(3 * size, size),
                buffer.Slice(4 * size, size),
                buffer.Slice(5 * size, size)
            );
            CurrentCaptures = buffer.Slice(6 * size, size * width);
            NextCaptures = buffer.Slice((6 * size) + (size * width), size * width);
            var pendingSize = PendingSize(size);
            Pending = buffer.Slice((6 * size) + (2 * size * width), pendingSize);
            Work = buffer.Slice((6 * size) + (2 * size * width) + pendingSize, width);
            Steps = 0;
        }

        public void Swap()
        {
            var list = Current;
            Current = Next;
            Next = list;
            var captures = CurrentCaptures;
            CurrentCaptures = NextCaptures;
            NextCaptures = captures;
        }
    }

    // Each state is expanded once per call, and it pushes at most two entries of three integers.
    private static int PendingSize(int size) => 3 * ((2 * size) + 2);

    private RegexMatchResult SimulateGroups(ReadOnlySpan<char> input, int startIndex, long maxSteps, Span<int> groups)
    {
        var size = _program.Length;
        var width = groups.Length;
        var rented = ArrayPool<int>.Shared.Rent((6 * size) + (2 * size * width) + PendingSize(size) + width);
        try
        {
            return SimulateGroups(input, startIndex, maxSteps, new CaptureThreads(rented, size, width), groups);
        }
        finally
        {
            ArrayPool<int>.Shared.Return(rented);
        }
    }

    // The simulation of Simulate with the capture slots of every thread next to it.
    private RegexMatchResult SimulateGroups(
        ReadOnlySpan<char> input,
        int startIndex,
        long maxSteps,
        CaptureThreads threads,
        Span<int> groups
    )
    {
        var matched = false;
        var position = startIndex;
        while (true)
        {
            if (!matched)
            {
                threads.Work.Fill(-1);
                threads.Work[0] = position;
                if (!AddThreadCaptures(ref threads, false, 0, input, position, maxSteps))
                {
                    return RegexMatchResult.Aborted;
                }
            }

            var following = position;
            var codePoint = position < input.Length ? NextCodePoint(input, ref following) : -1;
            threads.Next.Clear();
            if (!StepThreads(ref threads, input, position, following, codePoint, maxSteps, groups, ref matched))
            {
                return RegexMatchResult.Aborted;
            }

            if (position >= input.Length || (matched && threads.Next.Count == 0))
            {
                return matched ? RegexMatchResult.Match : RegexMatchResult.NoMatch;
            }

            threads.Swap();
            position = following;
        }
    }

    // Moves every thread that accepts the code point to its successor; the first thread at the end of the program is the match
    // so far and cuts every thread of lower priority.
    private bool StepThreads(
        ref CaptureThreads threads,
        ReadOnlySpan<char> input,
        int position,
        int following,
        int codePoint,
        long maxSteps,
        Span<int> groups,
        ref bool matched
    )
    {
        var width = groups.Length;
        for (var i = 0; i < threads.Current.Count; i++)
        {
            var pc = threads.Current.Dense[i];
            var captures = threads.CurrentCaptures.Slice(i * width, width);
            if (_program[pc].Opcode == Opcode.Match)
            {
                matched = true;
                captures.CopyTo(groups);
                groups[1] = position;
                break;
            }

            if (codePoint >= 0 && Accepts(_program[pc], codePoint))
            {
                captures.CopyTo(threads.Work);
                if (!AddThreadCaptures(ref threads, true, pc + 1, input, following, maxSteps))
                {
                    return false;
                }
            }
        }

        return true;
    }

    // AddThread with the capture slots: Work holds the slots of the thread and is changed along the path and taken back after it,
    // the state that consumes a character or ends the program keeps a copy of the slots at its position in the list.
    private bool AddThreadCaptures(
        ref CaptureThreads threads,
        bool intoNext,
        int pc,
        ReadOnlySpan<char> input,
        int position,
        long maxSteps
    )
    {
        ref var list = ref (intoNext ? ref threads.Next : ref threads.Current);
        var captures = intoNext ? threads.NextCaptures : threads.CurrentCaptures;
        var work = threads.Work;
        var pending = threads.Pending;
        var top = 0;
        PushPending(pending, ref top, BranchEntry, pc, 0);
        while (top > 0)
        {
            top -= 3;
            if (pending[top] == RestoreEntry)
            {
                work[pending[top + 1]] = pending[top + 2];
                continue;
            }

            pc = pending[top + 1];
            if (!list.Add(pc, work[0]))
            {
                continue;
            }

            if (threads.Steps >= maxSteps)
            {
                return false;
            }

            threads.Steps++;
            var instruction = _program[pc];
            switch (instruction.Opcode)
            {
                case Opcode.Split:
                    PushPending(pending, ref top, BranchEntry, instruction.B, 0);
                    PushPending(pending, ref top, BranchEntry, instruction.A, 0);
                    break;
                case Opcode.Jump:
                    PushPending(pending, ref top, BranchEntry, instruction.A, 0);
                    break;
                case Opcode.Save:
                    PushPending(pending, ref top, RestoreEntry, instruction.A, work[instruction.A]);
                    work[instruction.A] = position;
                    PushPending(pending, ref top, BranchEntry, pc + 1, 0);
                    break;
                case Opcode.Mark:
                case Opcode.Progress:
                    PushPending(pending, ref top, BranchEntry, pc + 1, 0);
                    break;
                case Opcode.AssertStart when StartHolds(input, position, instruction.A != 0):
                case Opcode.AssertEnd when EndHolds(input, position, instruction.A != 0):
                    PushPending(pending, ref top, BranchEntry, pc + 1, 0);
                    break;
                default:
                    work.CopyTo(captures.Slice((list.Count - 1) * work.Length, work.Length));
                    break;
            }
        }

        return true;
    }

    private static void PushPending(Span<int> pending, ref int top, int kind, int x, int y)
    {
        pending[top++] = kind;
        pending[top++] = x;
        pending[top++] = y;
    }
}
