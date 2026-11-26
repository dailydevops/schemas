namespace NetEvolve.Schemas.Xml.Regex.Matching;

using System;
using System.Buffers;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Regex.Contracts;
using NetEvolve.Schemas.Xml.Regex.Parsing;
using NetEvolve.Schemas.Xml.Regex.XPathDialect;

/// <summary>
/// A compiled pattern: the instruction array of the syntax tree. The <see cref="RegexDialect.Xsd"/> dialect matches the whole
/// input, run by a Pike/Thompson automaton simulation whose work is at most (input length + 1) times the program size and is
/// capped by <see cref="RegexStepLimits.MaxNfaSteps"/>; a pattern never backtracks. The <see cref="RegexDialect.XPath"/> dialect
/// searches the input for the leftmost match: patterns without back references run on the same kind of simulation, with
/// the threads kept in the order of the pattern so that greedy and reluctant quantifiers and the order of alternatives pick the
/// match, and patterns with a back reference run on a backtracking machine capped by
/// <see cref="RegexStepLimits.MaxBacktrackSteps"/>.
/// </summary>
/// <remarks>
/// Limitations: a counted repeat is expanded into the program, so the program size is capped at <see cref="MaxProgramSize"/>
/// and, for example, <c>a{1,1000000}</c> is rejected with <see cref="RegexError.LimitExceeded"/> (approved design AP-15 A), except that a repeat whose required length exceeds any input, such as <c>a{2147483647}</c>, compiles to a program that never matches; the
/// tree nesting is capped at <see cref="MaxNesting"/>; a match that exceeds the step ceiling is
/// <see cref="RegexMatchResult.Aborted"/> although the specification defines an answer; a lone surrogate in the input is
/// matched as the code point of that surrogate. In the XPath dialect an alternation prefers its first matching alternative
/// over a longer one, as the engines behind the XPath functions do, and <c>$</c> and <c>^</c> follow the text of the flag
/// <c>m</c>: with it, no line starts after a final newline and the end of the input is no line end after one. The backtracking
/// machine stops a loop at the first iteration that matches the empty string, as backtracking engines do, while the automaton
/// drops such an iteration; for a loop whose body can match the empty string, such as <c>(?:b?|c)*</c>, the two can therefore
/// pick different matches, each of them a valid one. The backtracking machine keeps its stack in a pooled array that grows with
/// the work done, at most 3 integers per step. The compiled program is
/// immutable, but an XSD program also holds a lazily built cache of automaton states (at most 256 states, each with a table
/// of 128 transition slots, so up to about 256 KB per program). The cache is built under a lock and read without one, so the
/// instance may be shared between threads; a call keeps its state on the stack or in a pooled array. Capture
/// enumeration, replace, and tokenize are not part of this type (DT-57).
/// </remarks>
public sealed partial class RegexProgram : IRegexMatcher
{
    /// <summary>The largest program, in instructions; a larger one is <see cref="RegexError.LimitExceeded"/>.</summary>
    public const int MaxProgramSize = 100_000;

    /// <summary>The deepest tree nesting; a deeper tree is <see cref="RegexError.LimitExceeded"/>.</summary>
    public const int MaxNesting = 64;

    // Programs up to this size keep their state arrays on the stack.
    private const int StackProgramSize = 128;

    private readonly Instruction[] _program;
    private readonly RegexCharacterSet[] _sets;
    private readonly RegexDialect _dialect;
    private readonly bool _backtracking;
    private readonly int _captureSlots;
    private readonly int _slotCount;

    private RegexProgram(
        Instruction[] program,
        RegexCharacterSet[] sets,
        RegexDialect dialect,
        bool backtracking,
        int captureSlots,
        int slotCount
    )
    {
        _program = program;
        _sets = sets;
        _dialect = dialect;
        _backtracking = backtracking;
        _captureSlots = captureSlots;
        _slotCount = slotCount;
        _dfa = dialect == RegexDialect.Xsd ? new Dfa() : null;
    }

    /// <inheritdoc/>
    public RegexDialect Dialect => _dialect;

    /// <summary>Compiles a syntax tree without options.</summary>
    /// <param name="nodes">The array-backed tree; its indexes, and the set indexes of its nodes, must be valid.</param>
    /// <param name="root">The index of the root node, or <see cref="RegexNode.NoNode"/> for the empty pattern.</param>
    /// <param name="sets">The character sets the <see cref="RegexNodeKind.Set"/> nodes refer to.</param>
    /// <param name="dialect">The dialect.</param>
    /// <param name="program">The compiled program, <see langword="null"/> on an error.</param>
    /// <returns><see cref="RegexError.None"/>, <see cref="RegexError.ConstructNotAllowed"/>, <see cref="RegexError.InvalidBackReference"/>, or <see cref="RegexError.LimitExceeded"/>.</returns>
    public static RegexError TryCompile(
        ReadOnlySpan<RegexNode> nodes,
        int root,
        ReadOnlySpan<RegexCharacterSet> sets,
        RegexDialect dialect,
        out RegexProgram? program
    ) => Compile(nodes, root, sets, dialect, RegexOptions.None, out program);

    /// <summary>Compiles the tree of a parsed pattern for the dialect and with the options it was parsed with.</summary>
    /// <param name="tree">The tree.</param>
    /// <param name="program">The compiled program, <see langword="null"/> on an error.</param>
    /// <returns><see cref="RegexError.None"/>, <see cref="RegexError.InvalidBackReference"/>, or <see cref="RegexError.LimitExceeded"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="tree"/> is <see langword="null"/>.</exception>
    public static RegexError TryCompile(RegexTree tree, out RegexProgram? program)
    {
        ArgumentNullException.ThrowIfNull(tree);

        var sets = new RegexCharacterSet[tree.SetCount];
        for (var i = 0; i < sets.Length; i++)
        {
            sets[i] = tree.GetSet(i);
        }

        return Compile(tree.Nodes, tree.Root, sets, tree.Dialect, tree.Options, out program);
    }

    private static RegexError Compile(
        ReadOnlySpan<RegexNode> nodes,
        int root,
        ReadOnlySpan<RegexCharacterSet> sets,
        RegexDialect dialect,
        RegexOptions options,
        out RegexProgram? program
    )
    {
        program = null;
        var xpath = dialect == RegexDialect.XPath;
        var backtracking = xpath && HasBackReference(nodes);
        var emitter = new Emitter(nodes, xpath, options, backtracking);
        emitter.Emit(root, 0);
        _ = emitter.Add(Opcode.Match, 0, 0);
        if (emitter.Error == RegexError.None && emitter.MaxBackReference > emitter.MaxGroup)
        {
            return RegexError.InvalidBackReference;
        }

        if (emitter.Error != RegexError.None)
        {
            return emitter.Error;
        }

        var captureSlots = 2 * (emitter.MaxGroup + 1);
        program = new RegexProgram(
            emitter.ToArray(),
            sets.ToArray(),
            dialect,
            backtracking,
            captureSlots,
            captureSlots + emitter.LoopCount
        );
        return RegexError.None;
    }

    private static bool HasBackReference(ReadOnlySpan<RegexNode> nodes)
    {
        for (var i = 0; i < nodes.Length; i++)
        {
            if (nodes[i].Kind == RegexNodeKind.BackReference)
            {
                return true;
            }
        }

        return false;
    }

    /// <inheritdoc/>
    /// <remarks>For the XPath dialect this is a search: <see cref="RegexMatchResult.Match"/> when any part of the input matches.</remarks>
    public RegexMatchResult Match(ReadOnlySpan<char> input, RegexStepLimits limits)
    {
        // The simulation takes at most (length + 1) times the program size in steps; below the ceiling no abort is possible,
        // so the deterministic path answers exactly as the simulation does.
        var effective = limits ?? RegexStepLimits.Default;
        if (
            _dfa is not null
            && (input.Length + 1L) * _program.Length <= effective.MaxNfaSteps
            && MatchDeterministic(input) is { } result
        )
        {
            return result;
        }

        return Match(input, effective, out _);
    }

    /// <summary>Finds the first match, from an index of the input.</summary>
    /// <param name="input">The input.</param>
    /// <param name="startIndex">The index to start the search at, 0 to the length of the input; the anchors still refer to the bounds of the whole input.</param>
    /// <param name="limits">The step ceilings, <see cref="RegexStepLimits.Default"/> for <see langword="null"/>.</param>
    /// <param name="index">The index of the match, -1 when there is none.</param>
    /// <param name="length">The length of the match, 0 when there is none.</param>
    /// <returns>The outcome; <see cref="RegexMatchResult.Aborted"/> when a step ceiling was reached.</returns>
    /// <remarks>For the XPath dialect this is the leftmost match; for the XSD dialect the input from <paramref name="startIndex"/> must match as a whole, and the match is that part of the input.</remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="startIndex"/> is negative or beyond the length of the input.</exception>
    public RegexMatchResult Find(
        ReadOnlySpan<char> input,
        int startIndex,
        RegexStepLimits limits,
        out int index,
        out int length
    )
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(startIndex, 0);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(startIndex, input.Length);

        if (_dialect == RegexDialect.XPath)
        {
            return Search(input, startIndex, limits, out index, out length, out _);
        }

        var result = Match(input.Slice(startIndex), limits, out _);
        var matched = result == RegexMatchResult.Match;
        index = matched ? startIndex : -1;
        length = matched ? input.Length - startIndex : 0;
        return result;
    }

    /// <summary>Matches an input and reports the steps taken.</summary>
    /// <param name="input">The input; the whole of it must match in the XSD dialect, any part of it in the XPath dialect.</param>
    /// <param name="limits">The step ceilings, <see cref="RegexStepLimits.Default"/> for <see langword="null"/>; <see cref="RegexStepLimits.MaxNfaSteps"/> applies, or <see cref="RegexStepLimits.MaxBacktrackSteps"/> for a pattern with a back reference.</param>
    /// <param name="steps">The steps taken, never above the ceiling.</param>
    /// <returns>The outcome; <see cref="RegexMatchResult.Aborted"/> when the ceiling was reached.</returns>
    public RegexMatchResult Match(ReadOnlySpan<char> input, RegexStepLimits limits, out long steps)
    {
        if (_dialect == RegexDialect.XPath)
        {
            return Search(input, 0, limits, out _, out _, out steps);
        }

        var size = _program.Length;
        if (size <= StackProgramSize)
        {
            // Sized to the program: the runtime zeroes the whole stack allocation on every call.
            Span<int> stack = stackalloc int[4 * size];
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

    private bool Accepts(in Instruction instruction, int codePoint) =>
        instruction.Opcode switch
        {
            Opcode.Char => instruction.A == codePoint,
            Opcode.Set => _sets[instruction.A].Contains(codePoint),
            Opcode.Any => codePoint is not ('\n' or '\r'),
            Opcode.AnyAll => true,
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
