namespace NetEvolve.Schemas.Xml.Regex.Matching;

using System;
using System.Buffers;
using NetEvolve.Schemas.Xml.Regex.Contracts;
using NetEvolve.Schemas.Xml.Regex.XPathDialect;
using NetEvolve.Schemas.Xml.Text.Unicode;

public sealed partial class RegexProgram
{
    // Programs up to this size keep their search state on the stack.
    private const int StackSearchSize = 128;

    // One backtrack entry is three ints: a branch (pc, position, 0) or a capture to restore (slot, old value, 1).
    private const int EntrySize = 3;

    /// <summary>Searches an input for the leftmost match; the XPath dialect only, in the XSD dialect the whole input is the match.</summary>
    /// <param name="input">The input.</param>
    /// <param name="limits">The step ceilings, <see cref="RegexStepLimits.Default"/> for <see langword="null"/>.</param>
    /// <param name="index">The UTF-16 index of the match, 0 when there is none.</param>
    /// <param name="length">The UTF-16 length of the match, 0 when there is none.</param>
    /// <returns>The outcome; <see cref="RegexMatchResult.Aborted"/> when the ceiling was reached.</returns>
    public RegexMatchResult Search(ReadOnlySpan<char> input, RegexStepLimits limits, out int index, out int length)
    {
        if (_dialect == RegexDialect.XPath)
        {
            return Find(input, limits, out index, out length, out _);
        }

        var result = Match(input, limits, out _);
        index = 0;
        length = result == RegexMatchResult.Match ? input.Length : 0;
        return result;
    }

    private RegexMatchResult Find(
        ReadOnlySpan<char> input,
        RegexStepLimits limits,
        out int index,
        out int length,
        out long steps
    )
    {
        limits ??= RegexStepLimits.Default;
        if (_hasBackReference)
        {
            return Backtrack(input, limits.MaxBacktrackSteps, out index, out length, out steps);
        }

        var size = _program.Length;
        var need = (8 * size) + 2;
        if (size <= StackSearchSize)
        {
            Span<int> stack = stackalloc int[(8 * StackSearchSize) + 2];
            return Pike(input, limits.MaxNfaSteps, stack, out index, out length, out steps);
        }

        var rented = ArrayPool<int>.Shared.Rent(need);
        try
        {
            return Pike(input, limits.MaxNfaSteps, rented, out index, out length, out steps);
        }
        finally
        {
            ArrayPool<int>.Shared.Return(rented);
        }
    }

    // Pike simulation in priority order: a new thread starts at every position behind all older ones, and the first thread
    // to reach the end of the program fixes the match and cuts every thread of lower priority.
    private RegexMatchResult Pike(
        ReadOnlySpan<char> input,
        long maxSteps,
        Span<int> buffer,
        out int index,
        out int length,
        out long steps
    )
    {
        var size = _program.Length;
        var current = new StateSet(buffer.Slice(0, size), buffer.Slice(size, size), buffer.Slice(2 * size, size));
        var next = new StateSet(
            buffer.Slice(3 * size, size),
            buffer.Slice(4 * size, size),
            buffer.Slice(5 * size, size)
        );
        var pending = buffer.Slice(6 * size, (2 * size) + 2);
        steps = 0;
        index = 0;
        length = 0;
        var matched = false;
        var position = 0;
        while (true)
        {
            if (!matched && !AddThread(ref current, pending, 0, position, position, input, maxSteps, ref steps))
            {
                return RegexMatchResult.Aborted;
            }

            var after = position;
            var codePoint = position < input.Length ? NextCodePoint(input, ref after) : -1;
            next.Clear();
            for (var i = 0; i < current.Count; i++)
            {
                var pc = current.Dense[i];
                var instruction = _program[pc];
                if (instruction.Opcode == Opcode.Match)
                {
                    index = current.Starts[i];
                    length = position - index;
                    matched = true;
                    break;
                }

                if (
                    codePoint >= 0
                    && Accepts(instruction, codePoint)
                    && !AddThread(ref next, pending, pc + 1, current.Starts[i], after, input, maxSteps, ref steps)
                )
                {
                    return RegexMatchResult.Aborted;
                }
            }

            var swap = current;
            current = next;
            next = swap;
            if (position >= input.Length || (matched && current.Count == 0))
            {
                return matched ? RegexMatchResult.Match : RegexMatchResult.NoMatch;
            }

            position = after;
        }
    }

    // Follows the jumps, splits, assertions, and slot instructions from a state in priority order; one step is one state
    // entering the list.
    private bool AddThread(
        ref StateSet list,
        Span<int> pending,
        int pc0,
        int start,
        int position,
        ReadOnlySpan<char> input,
        long maxSteps,
        ref long steps
    )
    {
        var top = 0;
        pending[top++] = pc0;
        while (top > 0)
        {
            var pc = pending[--top];
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
                case Opcode.Jump:
                    pending[top++] = instruction.A;
                    break;
                case Opcode.Split:
                    pending[top++] = instruction.B;
                    pending[top++] = instruction.A;
                    break;
                case Opcode.Save or Opcode.Progress:
                    pending[top++] = pc + 1;
                    break;
                case Opcode.AnchorStart when AtStart(input, position):
                case Opcode.AnchorEnd when AtEnd(input, position):
                    pending[top++] = pc + 1;
                    break;
                default:
                    break;
            }
        }

        return true;
    }

    private bool AtStart(ReadOnlySpan<char> input, int position) =>
        position == 0 || ((_flags & RegexFlagSet.MultiLine) != RegexFlagSet.None && input[position - 1] == '\n');

    private bool AtEnd(ReadOnlySpan<char> input, int position) =>
        position == input.Length || ((_flags & RegexFlagSet.MultiLine) != RegexFlagSet.None && input[position] == '\n');

    // The backtracking machine for a pattern with back references; the first start position with a match wins.
    private RegexMatchResult Backtrack(
        ReadOnlySpan<char> input,
        long maxSteps,
        out int index,
        out int length,
        out long steps
    )
    {
        steps = 0;
        index = 0;
        length = 0;
        var slots = ArrayPool<int>.Shared.Rent(_slotCount);
        var stack = ArrayPool<int>.Shared.Rent(64 * EntrySize);
        try
        {
            for (var start = 0; ; )
            {
                for (var i = 0; i < _slotCount; i++)
                {
                    slots[i] = -1;
                }

                var result = RunFrom(input, start, slots, ref stack, maxSteps, ref steps, out var end);
                if (result != RegexMatchResult.NoMatch)
                {
                    index = start;
                    length = result == RegexMatchResult.Match ? end - start : 0;
                    return result;
                }

                if (start >= input.Length)
                {
                    return RegexMatchResult.NoMatch;
                }

                _ = NextCodePoint(input, ref start);
            }
        }
        finally
        {
            ArrayPool<int>.Shared.Return(slots);
            ArrayPool<int>.Shared.Return(stack);
        }
    }

    private RegexMatchResult RunFrom(
        ReadOnlySpan<char> input,
        int start,
        int[] slots,
        ref int[] stack,
        long maxSteps,
        ref long steps,
        out int end
    )
    {
        end = 0;
        var top = 0;
        var pc = 0;
        var position = start;
        while (true)
        {
            if (steps >= maxSteps)
            {
                return RegexMatchResult.Aborted;
            }

            steps++;
            var instruction = _program[pc];
            var failed = false;
            switch (instruction.Opcode)
            {
                case Opcode.Char or Opcode.Set or Opcode.Any:
                    var after = position;
                    if (position < input.Length && Accepts(instruction, NextCodePoint(input, ref after)))
                    {
                        position = after;
                        pc++;
                    }
                    else
                    {
                        failed = true;
                    }

                    break;
                case Opcode.Split:
                    Push(ref stack, ref top, instruction.B, position, 0);
                    pc = instruction.A;
                    break;
                case Opcode.Jump:
                    pc = instruction.A;
                    break;
                case Opcode.Save:
                    Push(ref stack, ref top, instruction.A, slots[instruction.A], 1);
                    slots[instruction.A] = position;
                    pc++;
                    break;
                case Opcode.Progress or Opcode.AnchorStart or Opcode.AnchorEnd or Opcode.BackRef:
                    failed = !TryAssert(instruction, input, slots, ref position);
                    pc++;
                    break;
                default:
                    end = position;
                    return RegexMatchResult.Match;
            }

            if (failed && !Unwind(slots, stack, ref top, out pc, out position))
            {
                return RegexMatchResult.NoMatch;
            }
        }
    }

    // The instructions that consume nothing and only succeed or fail; a back reference also advances the position.
    private bool TryAssert(Instruction instruction, ReadOnlySpan<char> input, int[] slots, ref int position) =>
        instruction.Opcode switch
        {
            Opcode.Progress => slots[instruction.A] != position,
            Opcode.AnchorStart => AtStart(input, position),
            Opcode.AnchorEnd => AtEnd(input, position),
            _ => MatchBackReference(input, slots[2 * instruction.A], slots[(2 * instruction.A) + 1], ref position),
        };

    // Pops entries, restoring captures, up to the latest branch; false when none is left.
    private static bool Unwind(int[] slots, int[] stack, ref int top, out int pc, out int position)
    {
        pc = 0;
        position = 0;
        while (top > 0)
        {
            top -= EntrySize;
            if (stack[top + 2] == 1)
            {
                slots[stack[top]] = stack[top + 1];
                continue;
            }

            pc = stack[top];
            position = stack[top + 1];
            return true;
        }

        return false;
    }

    private static void Push(ref int[] stack, ref int top, int a, int b, int kind)
    {
        if (top + EntrySize > stack.Length)
        {
            var larger = ArrayPool<int>.Shared.Rent(stack.Length * 2);
            Array.Copy(stack, larger, top);
            ArrayPool<int>.Shared.Return(stack);
            stack = larger;
        }

        stack[top] = a;
        stack[top + 1] = b;
        stack[top + 2] = kind;
        top += EntrySize;
    }

    // A group that did not take part matches the empty string.
    private bool MatchBackReference(ReadOnlySpan<char> input, int first, int last, ref int position)
    {
        if (first < 0 || last < first)
        {
            return true;
        }

        var captured = first;
        var cursor = position;
        while (captured < last)
        {
            if (cursor >= input.Length)
            {
                return false;
            }

            var expected = NextCodePoint(input, ref captured);
            var actual = NextCodePoint(input, ref cursor);
            if (
                expected != actual
                && (
                    (_flags & RegexFlagSet.IgnoreCase) == RegexFlagSet.None
                    || UnicodeTables.SimpleCaseFold(expected) != UnicodeTables.SimpleCaseFold(actual)
                )
            )
            {
                return false;
            }
        }

        position = cursor;
        return true;
    }
}
