namespace NetEvolve.Schemas.Xml.Regex.Matching;

using System;
using System.Buffers;
using NetEvolve.Schemas.Xml.Regex.Contracts;
using NetEvolve.Schemas.Xml.Text.Unicode;

// The search of the XPath dialect.
public sealed partial class RegexProgram
{
    // The backtracking stack holds three integers per entry; it starts with room for this many entries and doubles.
    private const int InitialStackEntries = 256;

    // The kinds of backtracking stack entry: an alternative to try (the program counter and the position), or the old value of
    // a slot to restore (the slot and the value).
    private const int BranchEntry = 0;
    private const int RestoreEntry = 1;

    // The state of the leftmost-first simulation: the program counters in the order of the pattern, each with the index its
    // thread started at.
    private ref struct ThreadList
    {
        public readonly Span<int> Dense;
        public readonly Span<int> Starts;
        private readonly Span<int> _sparse;
        public int Count;

        public ThreadList(Span<int> dense, Span<int> sparse, Span<int> starts)
        {
            Dense = dense;
            _sparse = sparse;
            Starts = starts;
            Count = 0;
        }

        public void Clear() => Count = 0;

        public bool Add(int pc, int start)
        {
            var slot = _sparse[pc];
            if ((uint)slot < (uint)Count && Dense[slot] == pc)
            {
                return false;
            }

            _sparse[pc] = Count;
            Starts[Count] = start;
            Dense[Count++] = pc;
            return true;
        }
    }

    private RegexMatchResult Search(
        ReadOnlySpan<char> input,
        int startIndex,
        RegexStepLimits limits,
        out int index,
        out int length,
        out long steps
    )
    {
        var effective = limits ?? RegexStepLimits.Default;
        if (_backtracking)
        {
            return Backtrack(input, startIndex, effective.MaxBacktrackSteps, out index, out length, out steps);
        }

        var size = _program.Length;
        if (size <= StackProgramSize)
        {
            Span<int> stack = stackalloc int[(8 * StackProgramSize) + 2];
            return Simulate(input, startIndex, effective.MaxNfaSteps, stack, out index, out length, out steps);
        }

        var rented = ArrayPool<int>.Shared.Rent((8 * size) + 2);
        try
        {
            return Simulate(input, startIndex, effective.MaxNfaSteps, rented, out index, out length, out steps);
        }
        finally
        {
            ArrayPool<int>.Shared.Return(rented);
        }
    }

    // Pike's simulation with the threads in priority order: a thread that reaches the end of the program cuts every thread of
    // lower priority, later starts come last, so the result is the leftmost match that the order of the pattern prefers.
    private RegexMatchResult Simulate(
        ReadOnlySpan<char> input,
        int startIndex,
        long maxSteps,
        Span<int> buffer,
        out int index,
        out int length,
        out long steps
    )
    {
        var size = _program.Length;
        var current = new ThreadList(buffer.Slice(0, size), buffer.Slice(size, size), buffer.Slice(2 * size, size));
        var next = new ThreadList(
            buffer.Slice(3 * size, size),
            buffer.Slice(4 * size, size),
            buffer.Slice(5 * size, size)
        );
        var pending = buffer.Slice(6 * size, (2 * size) + 2);
        steps = 0;
        index = -1;
        length = 0;
        var matched = false;
        var position = startIndex;
        while (true)
        {
            if (!matched && !AddThread(ref current, pending, 0, position, input, position, maxSteps, ref steps))
            {
                index = -1;
                return RegexMatchResult.Aborted;
            }

            var following = position;
            var codePoint = position < input.Length ? NextCodePoint(input, ref following) : -1;
            next.Clear();
            for (var i = 0; i < current.Count; i++)
            {
                var pc = current.Dense[i];
                if (_program[pc].Opcode == Opcode.Match)
                {
                    matched = true;
                    index = current.Starts[i];
                    length = position - index;
                    break;
                }

                if (
                    codePoint >= 0
                    && Accepts(_program[pc], codePoint)
                    && !AddThread(ref next, pending, pc + 1, current.Starts[i], input, following, maxSteps, ref steps)
                )
                {
                    index = -1;
                    return RegexMatchResult.Aborted;
                }
            }

            if (position >= input.Length || (matched && next.Count == 0))
            {
                return matched ? RegexMatchResult.Match : RegexMatchResult.NoMatch;
            }

            var swap = current;
            current = next;
            next = swap;
            position = following;
        }
    }

    // Adds a thread and everything it reaches without consuming a character, depth first so that the preferred branch of a split
    // comes first in the list; an anchor that does not hold at the position ends the thread.
    private bool AddThread(
        ref ThreadList list,
        Span<int> pending,
        int pc,
        int start,
        ReadOnlySpan<char> input,
        int position,
        long maxSteps,
        ref long steps
    )
    {
        var top = 0;
        pending[top++] = pc;
        while (top > 0)
        {
            pc = pending[--top];
            if (!list.Add(pc, start))
            {
                continue;
            }

            if (steps >= maxSteps)
            {
                return false;
            }

            steps++;
            var instruction = _program[pc];
            switch (instruction.Opcode)
            {
                case Opcode.Split:
                    pending[top++] = instruction.B;
                    pending[top++] = instruction.A;
                    break;
                case Opcode.Jump:
                    pending[top++] = instruction.A;
                    break;
                case Opcode.AssertStart when StartHolds(input, position, instruction.A != 0):
                case Opcode.AssertEnd when EndHolds(input, position, instruction.A != 0):
                    pending[top++] = pc + 1;
                    break;
                default:
                    break;
            }
        }

        return true;
    }

    // ^: the start of the input, or in the multi-line mode the position after a newline unless that newline ends the input.
    private static bool StartHolds(ReadOnlySpan<char> input, int position, bool multiLine) =>
        position == 0 || (multiLine && position < input.Length && input[position - 1] == '\n');

    // $: the end of the input, or in the multi-line mode the position before a newline; the end of an input that ends with a
    // newline is not a line end in the multi-line mode.
    private static bool EndHolds(ReadOnlySpan<char> input, int position, bool multiLine) =>
        position == input.Length
            ? !multiLine || input.Length == 0 || input[input.Length - 1] != '\n'
            : multiLine && input[position] == '\n';

    // The machine for patterns with a back reference: it tries the paths of the pattern in the order of preference, one start
    // position after the other, and takes back the captures of a path that failed.
    private RegexMatchResult Backtrack(
        ReadOnlySpan<char> input,
        int startIndex,
        long maxSteps,
        out int index,
        out int length,
        out long steps
    )
    {
        steps = 0;
        index = -1;
        length = 0;
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
                    index = begin;
                    length = end - begin;
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

    private RegexMatchResult TryAt(
        ReadOnlySpan<char> input,
        int begin,
        long maxSteps,
        ref long steps,
        int[] slots,
        ref int[] stack,
        out int end
    )
    {
        end = -1;
        var top = 0;
        var pc = 0;
        var position = begin;
        while (true)
        {
            if (steps >= maxSteps)
            {
                return RegexMatchResult.Aborted;
            }

            steps++;
            var instruction = _program[pc];
            var succeeded = true;
            switch (instruction.Opcode)
            {
                case Opcode.Match:
                    end = position;
                    return RegexMatchResult.Match;
                case Opcode.Split:
                    Push(ref stack, ref top, BranchEntry, instruction.B, position);
                    pc = instruction.A;
                    continue;
                case Opcode.Jump:
                    pc = instruction.A;
                    continue;
                case Opcode.Save:
                case Opcode.Mark:
                    var slot = instruction.Opcode == Opcode.Save ? instruction.A : _captureSlots + instruction.A;
                    Push(ref stack, ref top, RestoreEntry, slot, slots[slot]);
                    slots[slot] = position;
                    break;
                case Opcode.Progress:
                    // An iteration that matched the empty string leaves the loop instead of repeating forever.
                    pc += slots[_captureSlots + instruction.A] == position ? 1 : 0;
                    break;
                case Opcode.AssertStart:
                case Opcode.AssertEnd:
                case Opcode.BackRef:
                    succeeded = Check(input, ref position, instruction, slots);
                    break;
                default:
                    succeeded = Consume(input, ref position, instruction);
                    break;
            }

            if (succeeded)
            {
                pc++;
                continue;
            }

            if (!Resume(ref top, stack, slots, out pc, out position))
            {
                return RegexMatchResult.NoMatch;
            }
        }
    }

    // The instructions that test the position or the text without taking a character from the pattern.
    private static bool Check(ReadOnlySpan<char> input, ref int position, in Instruction instruction, int[] slots) =>
        instruction.Opcode switch
        {
            Opcode.AssertStart => StartHolds(input, position, instruction.A != 0),
            Opcode.AssertEnd => EndHolds(input, position, instruction.A != 0),
            _ => MatchBackReference(input, ref position, instruction, slots),
        };

    private bool Consume(ReadOnlySpan<char> input, ref int position, in Instruction instruction)
    {
        if (position >= input.Length)
        {
            return false;
        }

        var following = position;
        if (!Accepts(instruction, NextCodePoint(input, ref following)))
        {
            return false;
        }

        position = following;
        return true;
    }

    // Back to the latest alternative, undoing the captures made since; false when there is none.
    private static bool Resume(ref int top, int[] stack, int[] slots, out int pc, out int position)
    {
        while (top > 0)
        {
            top -= 3;
            if (stack[top] == RestoreEntry)
            {
                slots[stack[top + 1]] = stack[top + 2];
                continue;
            }

            pc = stack[top + 1];
            position = stack[top + 2];
            return true;
        }

        pc = 0;
        position = 0;
        return false;
    }

    private static void Push(ref int[] stack, ref int top, int kind, int x, int y)
    {
        if (top + 3 > stack.Length)
        {
            var bigger = ArrayPool<int>.Shared.Rent(stack.Length * 2);
            Array.Copy(stack, bigger, top);
            ArrayPool<int>.Shared.Return(stack);
            stack = bigger;
        }

        stack[top++] = kind;
        stack[top++] = x;
        stack[top++] = y;
    }

    // A back reference to a group that took no part in the match matches the empty string.
    private static bool MatchBackReference(
        ReadOnlySpan<char> input,
        ref int position,
        in Instruction instruction,
        int[] slots
    )
    {
        var from = slots[2 * instruction.A];
        var to = slots[(2 * instruction.A) + 1];
        if (from < 0 || to < 0)
        {
            return true;
        }

        var length = to - from;
        if (instruction.B == 0)
        {
            if (
                position + length > input.Length
                || !input.Slice(from, length).SequenceEqual(input.Slice(position, length))
            )
            {
                return false;
            }

            position += length;
            return true;
        }

        // Case-blind: every code point of the captured text must be a case variant of the one at the same place.
        var captured = from;
        var current = position;
        while (captured < to)
        {
            if (current >= input.Length)
            {
                return false;
            }

            var expected = NextCodePoint(input, ref captured);
            var actual = NextCodePoint(input, ref current);
            if (expected != actual && UnicodeTables.SimpleCaseFold(expected) != UnicodeTables.SimpleCaseFold(actual))
            {
                return false;
            }
        }

        position = current;
        return true;
    }
}
