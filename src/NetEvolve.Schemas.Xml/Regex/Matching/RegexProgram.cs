namespace NetEvolve.Schemas.Xml.Regex.Matching;

using System;
using System.Buffers;
using NetEvolve.Schemas.Xml.Regex.Contracts;
using NetEvolve.Schemas.Xml.Regex.XPathDialect;

/// <summary>
/// A compiled pattern: the instruction array of the syntax tree, run against spans by a Pike/Thompson automaton simulation.
/// The work of one call is at most (input length + 1) times the program size and is capped by
/// <see cref="RegexStepLimits.MaxNfaSteps"/>; a pattern never backtracks, except an XPath pattern with a back reference,
/// which runs on a backtracking machine capped by <see cref="RegexStepLimits.MaxBacktrackSteps"/>.
/// </summary>
/// <remarks>
/// Limitations: the <see cref="RegexDialect.Xsd"/> dialect matches the whole input, the <see cref="RegexDialect.XPath"/>
/// dialect searches it for the leftmost match in the priority order of the quantifiers and alternatives; the backtracking
/// machine needs memory in proportion to the steps it takes, which the ceiling bounds, and an iteration of an unbounded loop
/// that consumes nothing fails there; the <see cref="RegexFlagSet.IgnoreCase"/> flag is resolved by the parser into the
/// character sets, only a back reference is compared case-insensitively by the matcher;
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
    private readonly RegexDialect _dialect;
    private readonly RegexFlagSet _flags;
    private readonly bool _hasBackReference;
    private readonly int _slotCount;

    private RegexProgram(
        Instruction[] program,
        RegexCharacterSet[] sets,
        RegexDialect dialect,
        RegexFlagSet flags,
        bool hasBackReference,
        int slotCount
    )
    {
        _program = program;
        _sets = sets;
        _dialect = dialect;
        _flags = flags;
        _hasBackReference = hasBackReference;
        _slotCount = slotCount;
    }

    /// <inheritdoc/>
    public RegexDialect Dialect => _dialect;

    /// <summary>Compiles a syntax tree without flags.</summary>
    /// <param name="nodes">The array-backed tree; its indexes, and the set indexes of its nodes, must be valid.</param>
    /// <param name="root">The index of the root node, or <see cref="RegexNode.NoNode"/> for the empty pattern.</param>
    /// <param name="sets">The character sets the <see cref="RegexNodeKind.Set"/> nodes refer to.</param>
    /// <param name="dialect">The dialect.</param>
    /// <param name="program">The compiled program, <see langword="null"/> on an error.</param>
    /// <returns>See the overload with flags.</returns>
    public static RegexError TryCompile(
        ReadOnlySpan<RegexNode> nodes,
        int root,
        ReadOnlySpan<RegexCharacterSet> sets,
        RegexDialect dialect,
        out RegexProgram? program
    ) => TryCompile(nodes, root, sets, dialect, RegexFlagSet.None, out program);

    /// <summary>Compiles a syntax tree.</summary>
    /// <param name="nodes">The array-backed tree; its indexes, and the set indexes of its nodes, must be valid.</param>
    /// <param name="root">The index of the root node, or <see cref="RegexNode.NoNode"/> for the empty pattern.</param>
    /// <param name="sets">The character sets the <see cref="RegexNodeKind.Set"/> nodes refer to.</param>
    /// <param name="dialect">The dialect.</param>
    /// <param name="flags">The flags the matcher itself applies: <see cref="RegexFlagSet.MultiLine"/> to the anchors and <see cref="RegexFlagSet.IgnoreCase"/> to the back references; the parser has resolved the others into the tree.</param>
    /// <param name="program">The compiled program, <see langword="null"/> on an error.</param>
    /// <returns><see cref="RegexError.None"/>, <see cref="RegexError.ConstructNotAllowed"/>, <see cref="RegexError.InvalidBackReference"/>, or <see cref="RegexError.LimitExceeded"/>.</returns>
    public static RegexError TryCompile(
        ReadOnlySpan<RegexNode> nodes,
        int root,
        ReadOnlySpan<RegexCharacterSet> sets,
        RegexDialect dialect,
        RegexFlagSet flags,
        out RegexProgram? program
    )
    {
        program = null;
        if (dialect is not (RegexDialect.Xsd or RegexDialect.XPath))
        {
            return RegexError.ConstructNotAllowed;
        }

        var emitter = new Emitter(nodes, dialect);
        emitter.Emit(root, 0);
        _ = emitter.Add(Opcode.Match, 0, 0);
        if (emitter.Error != RegexError.None)
        {
            return emitter.Error;
        }

        program = new RegexProgram(
            emitter.ToArray(),
            sets.ToArray(),
            dialect,
            flags,
            emitter.HasBackReference,
            emitter.SlotCount
        );
        return RegexError.None;
    }

    /// <inheritdoc/>
    public RegexMatchResult Match(ReadOnlySpan<char> input, RegexStepLimits limits) => Match(input, limits, out _);

    /// <summary>Matches an input and reports the steps taken.</summary>
    /// <param name="input">The input; the whole of it must match in the XSD dialect, a match anywhere in it counts in the XPath dialect.</param>
    /// <param name="limits">The step ceilings, <see cref="RegexStepLimits.Default"/> for <see langword="null"/>; <see cref="RegexStepLimits.MaxNfaSteps"/> applies, or <see cref="RegexStepLimits.MaxBacktrackSteps"/> for a pattern with a back reference.</param>
    /// <param name="steps">The steps taken, never above the ceiling.</param>
    /// <returns>The outcome; <see cref="RegexMatchResult.Aborted"/> when the ceiling was reached.</returns>
    public RegexMatchResult Match(ReadOnlySpan<char> input, RegexStepLimits limits, out long steps)
    {
        if (_dialect == RegexDialect.XPath)
        {
            return Find(input, limits, out _, out _, out steps);
        }

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
            if (Accepts(_program[pc], codePoint))
            {
                _ = next.Add(pc + 1);
            }
        }
    }

    private bool Accepts(Instruction instruction, int codePoint) =>
        instruction.Opcode switch
        {
            Opcode.Char => instruction.A == codePoint,
            Opcode.Set => _sets[instruction.A].Contains(codePoint),
            Opcode.Any => codePoint is not ('\n' or '\r'),
            _ => false,
        };

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
