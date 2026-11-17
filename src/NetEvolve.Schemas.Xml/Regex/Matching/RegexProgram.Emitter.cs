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
        private readonly List<Instruction> _code = [];

        public Emitter(ReadOnlySpan<RegexNode> nodes) => _nodes = nodes.ToArray();

        public RegexError Error { get; private set; }

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
                    Emit(node.FirstChild, depth + 1);
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
            if (node.IsLazy)
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
                var loop = Add(Opcode.Split, _code.Count + 1, 0);
                Emit(node.FirstChild, depth + 1);
                _ = Add(Opcode.Jump, loop, 0);
                Patch(loop, b: _code.Count);
                return;
            }

            var splits = new List<int>();
            for (var i = node.Min; i < node.Max && Error == RegexError.None; i++)
            {
                splits.Add(Add(Opcode.Split, _code.Count + 1, 0));
                var start = _code.Count;
                Emit(node.FirstChild, depth + 1);
                if (_code.Count == start)
                {
                    break;
                }
            }

            foreach (var split in splits)
            {
                Patch(split, b: _code.Count);
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
