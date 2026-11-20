namespace NetEvolve.Schemas.Xml.Regex.Matching;

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using NetEvolve.Schemas.Xml.Regex.Contracts;
using NetEvolve.Schemas.Xml.Regex.XPathDialect;

public sealed partial class RegexProgram
{
    private enum Opcode
    {
        Char,
        Set,
        Any,
        Split,
        Jump,
        Match,

        // The wildcard of the dot-all mode.
        AnyAll,

        // Records the position in a capture slot (A); only in programs with back references.
        Save,

        // Records the position at the start of a loop iteration in a loop slot (A); only in programs with back references.
        Mark,

        // Ends a loop iteration: leaves the loop when the iteration matched the empty string, so an empty body cannot spin.
        Progress,

        // The anchors; A is 1 in the multi-line mode.
        AssertStart,
        AssertEnd,

        // Matches the text of capture group A again; B is 1 when the comparison ignores case.
        BackRef,
    }

    [StructLayout(LayoutKind.Auto)]
    private readonly struct Instruction
    {
        public Instruction(Opcode opcode, int a, int b)
        {
            Opcode = opcode;
            A = a;
            B = b;
        }

        public Opcode Opcode { get; }

        public int A { get; }

        public int B { get; }
    }

    // A sparse set of program counters: constant-time membership and clear, no initialization of the arrays.
    private ref struct StateSet
    {
        public readonly Span<int> Dense;
        private readonly Span<int> _sparse;
        public int Count;

        public StateSet(Span<int> dense, Span<int> sparse)
        {
            Dense = dense;
            _sparse = sparse;
            Count = 0;
        }

        public void Clear() => Count = 0;

        public bool Add(int pc)
        {
            var slot = _sparse[pc];
            if ((uint)slot < (uint)Count && Dense[slot] == pc)
            {
                return false;
            }

            _sparse[pc] = Count;
            Dense[Count++] = pc;
            return true;
        }
    }

    // Compiles at most MaxProgramSize instructions; the first error wins and stops all emission.
    private sealed class Emitter
    {
        private readonly RegexNode[] _nodes;
        private readonly bool _xpath;
        private readonly RegexOptions _options;
        private readonly bool _backtracking;
        private readonly List<Instruction> _code = [];

        public Emitter(ReadOnlySpan<RegexNode> nodes, bool xpath, RegexOptions options, bool backtracking)
        {
            _nodes = nodes.ToArray();
            _xpath = xpath;
            _options = options;
            _backtracking = backtracking;
        }

        public RegexError Error { get; private set; }

        // The highest capture group emitted and the highest group a back reference names.
        public int MaxGroup { get; private set; }

        public int MaxBackReference { get; private set; }

        public int LoopCount { get; private set; }

        public Instruction[] ToArray() => _code.ToArray();

        public int Add(Opcode opcode, int a, int b)
        {
            if (_code.Count >= MaxProgramSize)
            {
                Fail(RegexError.LimitExceeded);
                return _code.Count;
            }

            _code.Add(new Instruction(opcode, a, b));
            return _code.Count - 1;
        }

        public void Emit(int index, int depth)
        {
            if (index == RegexNode.NoNode || Error != RegexError.None)
            {
                return;
            }

            if (depth > MaxNesting)
            {
                Fail(RegexError.LimitExceeded);
                return;
            }

            var node = _nodes[index];
            switch (node.Kind)
            {
                case RegexNodeKind.Character:
                    _ = Add(Opcode.Char, node.Value, 0);
                    break;
                case RegexNodeKind.Set:
                    _ = Add(Opcode.Set, node.Value, 0);
                    break;
                case RegexNodeKind.AnyChar:
                    _ = Add((_options & RegexOptions.DotAll) == RegexOptions.None ? Opcode.Any : Opcode.AnyAll, 0, 0);
                    break;
                case RegexNodeKind.Group:
                    EmitGroup(node, depth);
                    break;
                case RegexNodeKind.Concat:
                    for (var child = node.FirstChild; child != RegexNode.NoNode && Error == RegexError.None; )
                    {
                        Emit(child, depth + 1);
                        child = _nodes[child].NextSibling;
                    }

                    break;
                case RegexNodeKind.Alternation:
                    EmitAlternation(node, depth);
                    break;
                case RegexNodeKind.Repeat:
                    EmitRepeat(node, depth);
                    break;
                case RegexNodeKind.Anchor when _xpath:
                    EmitAnchor(node);
                    break;
                case RegexNodeKind.BackReference when _xpath:
                    EmitBackReference(node);
                    break;
                case RegexNodeKind.Empty:
                    break;
                default:
                    // Anchors and back references belong to the XPath dialect.
                    Fail(RegexError.ConstructNotAllowed);
                    break;
            }
        }

        private void EmitAnchor(RegexNode node) =>
            _ = Add(
                node.Value == (int)RegexAnchor.Start ? Opcode.AssertStart : Opcode.AssertEnd,
                (_options & RegexOptions.MultiLine) == RegexOptions.None ? 0 : 1,
                0
            );

        private void EmitBackReference(RegexNode node)
        {
            MaxBackReference = Math.Max(MaxBackReference, node.Value);
            _ = Add(Opcode.BackRef, node.Value, (_options & RegexOptions.IgnoreCase) == RegexOptions.None ? 0 : 1);
        }

        // A capturing group records its bounds, but only a program with back references reads them.
        private void EmitGroup(RegexNode node, int depth)
        {
            if (!_backtracking || node.Value == RegexNode.NoGroup)
            {
                Emit(node.FirstChild, depth + 1);
                return;
            }

            MaxGroup = Math.Max(MaxGroup, node.Value);
            _ = Add(Opcode.Save, 2 * node.Value, 0);
            Emit(node.FirstChild, depth + 1);
            _ = Add(Opcode.Save, (2 * node.Value) + 1, 0);
        }

        private void EmitAlternation(RegexNode node, int depth)
        {
            var jumps = new List<int>();
            for (var child = node.FirstChild; child != RegexNode.NoNode && Error == RegexError.None; )
            {
                var following = _nodes[child].NextSibling;
                if (following == RegexNode.NoNode)
                {
                    Emit(child, depth + 1);
                    break;
                }

                var split = Add(Opcode.Split, _code.Count + 1, 0);
                Emit(child, depth + 1);
                jumps.Add(Add(Opcode.Jump, 0, 0));
                Patch(split, b: _code.Count);
                child = following;
            }

            foreach (var jump in jumps)
            {
                Patch(jump, a: _code.Count);
            }
        }

        private void EmitRepeat(RegexNode node, int depth)
        {
            if (node.IsLazy && !_xpath)
            {
                Fail(RegexError.ConstructNotAllowed);
                return;
            }

            // Counted repeats are expanded (AP-15 A); an empty child cannot grow the program, so it ends the loops.
            for (var i = 0; i < node.Min && Error == RegexError.None; i++)
            {
                var start = _code.Count;
                Emit(node.FirstChild, depth + 1);
                if (_code.Count == start)
                {
                    break;
                }
            }

            if (node.Max == RegexNode.Unbounded)
            {
                EmitLoop(node, depth);
                return;
            }

            // The first branch of a split is the preferred one: the body for a greedy repeat, the exit for a reluctant one.
            var splits = new List<int>();
            for (var i = node.Min; i < node.Max && Error == RegexError.None; i++)
            {
                splits.Add(node.IsLazy ? Add(Opcode.Split, 0, _code.Count + 1) : Add(Opcode.Split, _code.Count + 1, 0));
                var start = _code.Count;
                Emit(node.FirstChild, depth + 1);
                if (_code.Count == start)
                {
                    break;
                }
            }

            foreach (var split in splits)
            {
                if (node.IsLazy)
                {
                    Patch(split, a: _code.Count);
                }
                else
                {
                    Patch(split, b: _code.Count);
                }
            }
        }

        // An unbounded loop; the back reference machine also needs to see an iteration that consumed nothing.
        private void EmitLoop(RegexNode node, int depth)
        {
            var split = Add(Opcode.Split, 0, 0);
            var body = _code.Count;
            var mark = LoopCount;
            if (_backtracking)
            {
                LoopCount++;
                _ = Add(Opcode.Mark, mark, 0);
            }

            Emit(node.FirstChild, depth + 1);
            if (_backtracking)
            {
                _ = Add(Opcode.Progress, mark, 0);
            }

            _ = Add(Opcode.Jump, split, 0);
            if (node.IsLazy)
            {
                Patch(split, a: _code.Count, b: body);
            }
            else
            {
                Patch(split, a: body, b: _code.Count);
            }
        }

        private void Patch(int at, int? a = null, int? b = null)
        {
            if (Error == RegexError.None)
            {
                var old = _code[at];
                _code[at] = new Instruction(old.Opcode, a ?? old.A, b ?? old.B);
            }
        }

        private void Fail(RegexError error)
        {
            if (Error == RegexError.None)
            {
                Error = error;
            }
        }
    }
}
