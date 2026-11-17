namespace NetEvolve.Schemas.Xml.Regex.Matching;

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using NetEvolve.Schemas.Xml.Regex.Contracts;

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
        Save,
        Progress,
        AnchorStart,
        AnchorEnd,
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
        public readonly Span<int> Starts;
        private readonly Span<int> _sparse;
        public int Count;

        public StateSet(Span<int> dense, Span<int> sparse)
            : this(dense, sparse, default) { }

        // The XPath search also keeps the input position at which the thread of each state started.
        public StateSet(Span<int> dense, Span<int> sparse, Span<int> starts)
        {
            Dense = dense;
            Starts = starts;
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

        public bool Add(int pc, int start)
        {
            if (!Add(pc))
            {
                return false;
            }

            Starts[Count - 1] = start;
            return true;
        }
    }

    // Compiles at most MaxProgramSize instructions; the first error wins and stops all emission.
    private sealed class Emitter
    {
        private readonly RegexNode[] _nodes;
        private readonly bool _xsd;
        private readonly int _maxGroup;
        private readonly List<Instruction> _code = [];
        private int _loops;

        public Emitter(ReadOnlySpan<RegexNode> nodes, RegexDialect dialect)
        {
            _nodes = nodes.ToArray();
            _xsd = dialect == RegexDialect.Xsd;
            foreach (var node in _nodes)
            {
                if (node.Kind == RegexNodeKind.BackReference)
                {
                    HasBackReference = true;
                }
                else if (node.Kind == RegexNodeKind.Group && node.Value > _maxGroup)
                {
                    _maxGroup = node.Value;
                }
            }
        }

        public RegexError Error { get; private set; }

        // A pattern with a back reference runs on the backtracking machine, which needs the capture and loop slots.
        public bool HasBackReference { get; }

        // Slots 2g and 2g + 1 hold the bounds of group g; the loop progress slots follow.
        public int SlotCount => (2 * (_maxGroup + 1)) + _loops;

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
                    _ = Add(Opcode.Any, 0, 0);
                    break;
                case RegexNodeKind.Group:
                    EmitGroup(node, depth);
                    break;
                case RegexNodeKind.Anchor when !_xsd:
                    _ = Add(node.Value == (int)RegexAnchor.Start ? Opcode.AnchorStart : Opcode.AnchorEnd, 0, 0);
                    break;
                case RegexNodeKind.BackReference when !_xsd:
                    if (node.Value > _maxGroup)
                    {
                        Fail(RegexError.InvalidBackReference);
                        break;
                    }

                    _ = Add(Opcode.BackRef, node.Value, 0);
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
                case RegexNodeKind.Empty:
                    break;
                default:
                    // Anchors and back references belong to the XPath dialect.
                    Fail(RegexError.ConstructNotAllowed);
                    break;
            }
        }

        private void EmitGroup(RegexNode node, int depth)
        {
            if (!HasBackReference || node.Value < 1)
            {
                Emit(node.FirstChild, depth + 1);
                return;
            }

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
            if (node.IsLazy && _xsd)
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
                var loop = Add(Opcode.Split, 0, 0);
                var slot = HasBackReference ? (2 * (_maxGroup + 1)) + _loops++ : -1;
                if (slot >= 0)
                {
                    // An iteration that consumed nothing fails, so the backtracking machine cannot loop on an empty body.
                    _ = Add(Opcode.Save, slot, 0);
                }

                Emit(node.FirstChild, depth + 1);
                if (slot >= 0)
                {
                    _ = Add(Opcode.Progress, slot, 0);
                }

                _ = Add(Opcode.Jump, loop, 0);
                PatchSplit(loop, node.IsLazy);
                return;
            }

            var splits = new List<int>();
            for (var i = node.Min; i < node.Max && Error == RegexError.None; i++)
            {
                splits.Add(Add(Opcode.Split, 0, 0));
                var start = _code.Count;
                Emit(node.FirstChild, depth + 1);
                if (_code.Count == start)
                {
                    break;
                }
            }

            foreach (var split in splits)
            {
                PatchSplit(split, node.IsLazy);
            }
        }

        // A split prefers the repeated body at the next instruction, or, when lazy, the exit at the end of the code so far.
        private void PatchSplit(int split, bool lazy)
        {
            var body = split + 1;
            var exit = _code.Count;
            Patch(split, lazy ? exit : body, lazy ? body : exit);
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
