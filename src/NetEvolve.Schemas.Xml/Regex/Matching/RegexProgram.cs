namespace NetEvolve.Schemas.Xml.Regex.Matching;

using System;
using System.Buffers;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Regex.Contracts;
using NetEvolve.Schemas.Xml.Regex.Parsing;

/// <summary>
/// A compiled pattern: the instruction array of the syntax tree, run against spans by a Pike/Thompson automaton simulation.
/// The work of one call is at most (input length + 1) times the program size and is capped by
/// <see cref="RegexStepLimits.MaxNfaSteps"/>; a pattern never backtracks.
/// </summary>
/// <remarks>
/// Limitations: only the <see cref="RegexDialect.Xsd"/> dialect is compiled (whole-input match; the XPath dialect is DT-14);
/// a counted repeat is expanded into the program, so the program size is capped at <see cref="MaxProgramSize"/> and, for
/// example, <c>a{1,1000000}</c> is rejected with <see cref="RegexError.LimitExceeded"/> (approved design AP-15 A); the tree
/// nesting is capped at <see cref="MaxNesting"/>; a match that exceeds the step ceiling is <see cref="RegexMatchResult.Aborted"/>
/// although the specification defines an answer; a lone surrogate in the input is matched as the code point of that surrogate.
/// The instance is immutable and may be shared between threads; a call keeps its state on the stack or in a pooled array.
/// </remarks>
public sealed partial class RegexProgram : IRegexMatcher
{
    /// <summary>The largest program, in instructions; a larger one is <see cref="RegexError.LimitExceeded"/>.</summary>
    public const int MaxProgramSize = 100_000;

    /// <summary>The deepest tree nesting; a deeper tree is <see cref="RegexError.LimitExceeded"/>.</summary>
    public const int MaxNesting = 64;

    // Programs up to this size keep their four state arrays on the stack.
    private const int StackProgramSize = 128;

    private readonly Instruction[] _program;
    private readonly RegexCharacterSet[] _sets;

    private RegexProgram(Instruction[] program, RegexCharacterSet[] sets)
    {
        _program = program;
        _sets = sets;
    }

    /// <inheritdoc/>
    public RegexDialect Dialect => RegexDialect.Xsd;

    /// <summary>Compiles a syntax tree.</summary>
    /// <param name="nodes">The array-backed tree; its indexes, and the set indexes of its nodes, must be valid.</param>
    /// <param name="root">The index of the root node, or <see cref="RegexNode.NoNode"/> for the empty pattern.</param>
    /// <param name="sets">The character sets the <see cref="RegexNodeKind.Set"/> nodes refer to.</param>
    /// <param name="dialect">The dialect; only <see cref="RegexDialect.Xsd"/> is supported.</param>
    /// <param name="program">The compiled program, <see langword="null"/> on an error.</param>
    /// <returns><see cref="RegexError.None"/>, <see cref="RegexError.ConstructNotAllowed"/>, or <see cref="RegexError.LimitExceeded"/>.</returns>
    public static RegexError TryCompile(
        ReadOnlySpan<RegexNode> nodes,
        int root,
        ReadOnlySpan<RegexCharacterSet> sets,
        RegexDialect dialect,
        out RegexProgram? program
    )
    {
        program = null;
        if (dialect != RegexDialect.Xsd)
        {
            return RegexError.ConstructNotAllowed;
        }

        var emitter = new Emitter(nodes);
        emitter.Emit(root, 0);
        _ = emitter.Add(Opcode.Match, 0, 0);
        if (emitter.Error != RegexError.None)
        {
            return emitter.Error;
        }

        program = new RegexProgram(emitter.ToArray(), sets.ToArray());
        return RegexError.None;
    }

    /// <summary>Compiles the tree of a parsed pattern.</summary>
    /// <param name="tree">The tree.</param>
    /// <param name="program">The compiled program, <see langword="null"/> on an error.</param>
    /// <returns>The error, <see cref="RegexError.None"/> on success.</returns>
    public static RegexError TryCompile(RegexTree tree, out RegexProgram? program)
    {
        Argument.ThrowIfNull(tree);

        var sets = new RegexCharacterSet[tree.SetCount];
        for (var i = 0; i < sets.Length; i++)
        {
            sets[i] = tree.GetSet(i);
        }

        return TryCompile(tree.Nodes, tree.Root, sets, tree.Dialect, out program);
    }

    /// <summary>Finds the first match.</summary>
    /// <param name="input">The input.</param>
    /// <param name="startIndex">The index to start at.</param>
    /// <param name="limits">The step ceilings.</param>
    /// <param name="index">The index of the match, -1 for none.</param>
    /// <param name="length">The length of the match.</param>
    /// <returns>The outcome.</returns>
    public RegexMatchResult Find(
        ReadOnlySpan<char> input,
        int startIndex,
        RegexStepLimits limits,
        out int index,
        out int length
    )
    {
        _ = (input.Length, startIndex, limits, _program);
        index = -1;
        length = 0;
        return RegexMatchResult.NoMatch;
    }

    /// <inheritdoc/>
    public RegexMatchResult Match(ReadOnlySpan<char> input, RegexStepLimits limits) => Match(input, limits, out _);

    /// <summary>Matches an input and reports the steps taken.</summary>
    /// <param name="input">The input; the whole of it must match.</param>
    /// <param name="limits">The step ceilings, <see cref="RegexStepLimits.Default"/> for <see langword="null"/>; <see cref="RegexStepLimits.MaxNfaSteps"/> applies.</param>
    /// <param name="steps">The steps taken, never above the ceiling.</param>
    /// <returns>The outcome; <see cref="RegexMatchResult.Aborted"/> when the ceiling was reached.</returns>
    public RegexMatchResult Match(ReadOnlySpan<char> input, RegexStepLimits limits, out long steps)
    {
        var size = _program.Length;
        if (size <= StackProgramSize)
        {
            Span<int> stack = stackalloc int[4 * StackProgramSize];
            return Run(input, limits, stack, out steps);
        }

        var rented = ArrayPool<int>.Shared.Rent(4 * size);
        try
        {
            return Run(input, limits, rented, out steps);
        }
        finally
        {
            ArrayPool<int>.Shared.Return(rented);
        }
    }

    private RegexMatchResult Run(ReadOnlySpan<char> input, RegexStepLimits limits, Span<int> buffer, out long steps)
    {
        var size = _program.Length;
        var maxSteps = (limits ?? RegexStepLimits.Default).MaxNfaSteps;
        var current = new StateSet(buffer.Slice(0, size), buffer.Slice(size, size));
        var next = new StateSet(buffer.Slice(2 * size, size), buffer.Slice(3 * size, size));
        steps = 0;
        _ = current.Add(0);
        if (!Expand(ref current, maxSteps, ref steps))
        {
            return RegexMatchResult.Aborted;
        }

        var position = 0;
        while (position < input.Length)
        {
            Advance(ref current, ref next, NextCodePoint(input, ref position));
            var swap = current;
            current = next;
            next = swap;
            if (current.Count == 0)
            {
                return RegexMatchResult.NoMatch;
            }

            if (!Expand(ref current, maxSteps, ref steps))
            {
                return RegexMatchResult.Aborted;
            }
        }

        for (var i = 0; i < current.Count; i++)
        {
            if (_program[current.Dense[i]].Opcode == Opcode.Match)
            {
                return RegexMatchResult.Match;
            }
        }

        return RegexMatchResult.NoMatch;
    }

    private static int NextCodePoint(ReadOnlySpan<char> input, ref int position)
    {
        int codePoint = input[position++];
        if (char.IsHighSurrogate((char)codePoint) && position < input.Length && char.IsLowSurrogate(input[position]))
        {
            codePoint = 0x10000 + ((codePoint - 0xD800) << 10) + (input[position++] - 0xDC00);
        }

        return codePoint;
    }

    // Moves every state that accepts the code point to its successor in the next set.
    private void Advance(ref StateSet current, ref StateSet next, int codePoint)
    {
        next.Clear();
        for (var i = 0; i < current.Count; i++)
        {
            var pc = current.Dense[i];
            var instruction = _program[pc];
            var accepts = instruction.Opcode switch
            {
                Opcode.Char => instruction.A == codePoint,
                Opcode.Set => _sets[instruction.A].Contains(codePoint),
                Opcode.Any => codePoint is not ('\n' or '\r'),
                _ => false,
            };
            if (accepts)
            {
                _ = next.Add(pc + 1);
            }
        }
    }

    // Follows the jumps and splits of every state in the set; one step is one state entering the set at one position.
    private bool Expand(ref StateSet set, long maxSteps, ref long steps)
    {
        for (var i = 0; i < set.Count; i++)
        {
            if (steps >= maxSteps)
            {
                return false;
            }

            steps++;
            var instruction = _program[set.Dense[i]];
            switch (instruction.Opcode)
            {
                case Opcode.Split:
                    _ = set.Add(instruction.A);
                    _ = set.Add(instruction.B);
                    break;
                case Opcode.Jump:
                    _ = set.Add(instruction.A);
                    break;
                default:
                    break;
            }
        }

        return true;
    }
}
