namespace NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Functions;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The little expression language of the function tests. Limitation: the XPath evaluator (XP-19, #297) is not implemented, so the
/// tests of the function package cannot run real XPath. This subset has the primitives the fixture functions need and nothing else:
/// integer and string literals, variables, the operators <c>or and = != &lt; &lt;= &gt; &gt;= + - * idiv mod</c>, conditionals, sequences,
/// <c>not true false string string-length substring translate concat xs:integer</c>, and calls of stylesheet functions. All control
/// flow of the fixtures (recursion, accumulators, branching on the result) is written in the XSLT text, not here.
/// </summary>
internal static class MiniQuery
{
    internal abstract class Node
    {
        internal abstract XdmSequence Evaluate(MiniRun run, in QueryFocus focus);

        internal virtual void Collect(List<int> slots) { }
    }

    /// <summary>A failure of the evaluation, carried as exception inside the evaluator only.</summary>
    internal sealed class Failure(SchematronProblem problem) : Exception("mini evaluation failure")
    {
        internal SchematronProblem Problem { get; } = problem;
    }

    internal static bool TryParse(
        string text,
        Func<string, int> resolveVariable,
        Func<string, string?> resolvePrefix,
        out Node node,
        out string error
    )
    {
        var parser = new Parser(text, resolveVariable, resolvePrefix);
        try
        {
            node = parser.ParseAll();
            error = string.Empty;
            return true;
        }
        catch (FormatException exception)
        {
            node = null!;
            error = exception.Message;
            return false;
        }
    }

    internal static long Integer(in XdmSequence value)
    {
        if (value.Count == 1 && value[0].TryGetInteger(out var integer))
        {
            return integer;
        }

        throw Fail("XPTY0004", "An integer is required.");
    }

    internal static string Text(in XdmSequence value)
    {
        if (value.Count == 0)
        {
            return string.Empty;
        }

        if (value.Count == 1)
        {
            if (value[0].TryGetString(out var text) && text is not null)
            {
                return text;
            }

            if (value[0].TryGetInteger(out var integer))
            {
                return integer.ToString(CultureInfo.InvariantCulture);
            }
        }

        throw Fail("XPTY0004", "A string is required.");
    }

    internal static bool Effective(in XdmSequence value)
    {
        if (value.Count == 0)
        {
            return false;
        }

        if (value.Count == 1)
        {
            var item = value[0];
            if (item.TryGetBoolean(out var boolean))
            {
                return boolean;
            }

            if (item.TryGetInteger(out var integer))
            {
                return integer != 0;
            }

            if (item.TryGetString(out var text))
            {
                return !string.IsNullOrEmpty(text);
            }
        }

        throw Fail("FORG0006", "No effective boolean value.");
    }

    private static long NonZero(in XdmSequence value)
    {
        var number = Integer(value);
        return number == 0 ? throw Fail("FOAR0001", "Division by zero.") : number;
    }

    internal static Failure Fail(string constraint, string message) =>
        new(new SchematronProblem(SchematronDiagnosticCodes.EvaluationFailed, message, default, constraint));

    private static XdmSequence One(XdmItem item) => XdmSequence.Create(item);

    private sealed class Literal(XdmSequence value) : Node
    {
        internal override XdmSequence Evaluate(MiniRun run, in QueryFocus focus) => value;
    }

    private sealed class ContextItem : Node
    {
        internal override XdmSequence Evaluate(MiniRun run, in QueryFocus focus) =>
            focus.Node.Tree is null
                ? throw Fail("XPDY0002", "The context item is absent.")
                : One(XdmItem.FromNode(focus.Node));
    }

    private sealed class Variable(string name, int slot) : Node
    {
        internal override XdmSequence Evaluate(MiniRun run, in QueryFocus focus) =>
            run.TryGetValue(slot, out var value)
                ? value
                : throw Fail("XPDY0002", $"The variable ${name} is not bound.");

        internal override void Collect(List<int> slots) => slots.Add(slot);
    }

    private sealed class Sequence(List<Node> items) : Node
    {
        internal override XdmSequence Evaluate(MiniRun run, in QueryFocus focus)
        {
            var all = new List<XdmItem>();
            foreach (var item in items)
            {
                var value = item.Evaluate(run, in focus);
                for (var i = 0; i < value.Count; i++)
                {
                    all.Add(value[i]);
                }
            }

            return XdmSequence.Create(all.ToArray());
        }

        internal override void Collect(List<int> slots)
        {
            foreach (var item in items)
            {
                item.Collect(slots);
            }
        }
    }

    private sealed class Binary(string op, Node left, Node right) : Node
    {
        internal override XdmSequence Evaluate(MiniRun run, in QueryFocus focus)
        {
            var l = left.Evaluate(run, in focus);
            if (op == "or")
            {
                return One(XdmItem.FromBoolean(Effective(l) || Effective(right.Evaluate(run, in focus))));
            }

            if (op == "and")
            {
                return One(XdmItem.FromBoolean(Effective(l) && Effective(right.Evaluate(run, in focus))));
            }

            var r = right.Evaluate(run, in focus);
            switch (op)
            {
                case "=" or "!=" or "<" or "<=" or ">" or ">=":
                    return One(XdmItem.FromBoolean(Compare(l, r)));
                case "+":
                    return One(XdmItem.FromInteger(Integer(l) + Integer(r)));
                case "-":
                    return One(XdmItem.FromInteger(Integer(l) - Integer(r)));
                case "*":
                    return One(XdmItem.FromInteger(Integer(l) * Integer(r)));
                case "idiv":
                    return One(XdmItem.FromInteger(Integer(l) / NonZero(r)));
                default:
                    return One(XdmItem.FromInteger(Integer(l) % NonZero(r)));
            }
        }

        internal override void Collect(List<int> slots)
        {
            left.Collect(slots);
            right.Collect(slots);
        }

        private bool Compare(in XdmSequence l, in XdmSequence r)
        {
            if (l.Count == 0 || r.Count == 0)
            {
                return false;
            }

            int order;
            if (l[0].TryGetInteger(out var a) && r[0].TryGetInteger(out var b))
            {
                order = a.CompareTo(b);
            }
            else if (l[0].TryGetString(out var s) && r[0].TryGetString(out var t))
            {
                order = string.CompareOrdinal(s, t);
            }
            else if (l[0].TryGetBoolean(out var x) && r[0].TryGetBoolean(out var y))
            {
                order = x.CompareTo(y);
            }
            else
            {
                throw Fail("XPTY0004", "The operands cannot be compared.");
            }

            return op switch
            {
                "=" => order == 0,
                "!=" => order != 0,
                "<" => order < 0,
                "<=" => order <= 0,
                ">" => order > 0,
                _ => order >= 0,
            };
        }
    }

    private sealed class Negate(Node operand) : Node
    {
        internal override XdmSequence Evaluate(MiniRun run, in QueryFocus focus) =>
            One(XdmItem.FromInteger(-Integer(operand.Evaluate(run, in focus))));

        internal override void Collect(List<int> slots) => operand.Collect(slots);
    }

    private sealed class Conditional(Node test, Node whenTrue, Node whenFalse) : Node
    {
        internal override XdmSequence Evaluate(MiniRun run, in QueryFocus focus) =>
            Effective(test.Evaluate(run, in focus))
                ? whenTrue.Evaluate(run, in focus)
                : whenFalse.Evaluate(run, in focus);

        internal override void Collect(List<int> slots)
        {
            test.Collect(slots);
            whenTrue.Collect(slots);
            whenFalse.Collect(slots);
        }
    }

    private sealed class Call(string name, string? uri, List<Node> arguments) : Node
    {
        internal override XdmSequence Evaluate(MiniRun run, in QueryFocus focus)
        {
            var values = new XdmSequence[arguments.Count];
            for (var i = 0; i < values.Length; i++)
            {
                values[i] = arguments[i].Evaluate(run, in focus);
            }

            return uri is null ? Builtin(values) : run.CallFunction(uri, name, values);
        }

        internal override void Collect(List<int> slots)
        {
            foreach (var argument in arguments)
            {
                argument.Collect(slots);
            }
        }

        private XdmSequence Builtin(XdmSequence[] a)
        {
            switch (name)
            {
                case "true":
                    return One(XdmItem.FromBoolean(true));
                case "false":
                    return One(XdmItem.FromBoolean(false));
                case "not":
                    return One(XdmItem.FromBoolean(!Effective(a[0])));
                case "string":
                    return One(XdmItem.FromString(Text(a[0])));
                case "string-length":
                    return One(XdmItem.FromInteger(Text(a[0]).Length));
                case "concat":
                    var builder = new StringBuilder();
                    foreach (var part in a)
                    {
                        _ = builder.Append(Text(part));
                    }

                    return One(XdmItem.FromString(builder.ToString()));
                case "translate":
                    var source = Text(a[0]);
                    var from = Text(a[1]);
                    var to = Text(a[2]);
                    var result = new StringBuilder();
                    foreach (var character in source)
                    {
                        var at = from.IndexOf(character);
                        if (at < 0)
                        {
                            _ = result.Append(character);
                        }
                        else if (at < to.Length)
                        {
                            _ = result.Append(to[at]);
                        }
                    }

                    return One(XdmItem.FromString(result.ToString()));
                case "substring":
                    var text = Text(a[0]);
                    var start = (int)Integer(a[1]) - 1;
                    var length = a.Length > 2 ? (int)Integer(a[2]) : text.Length - start;
                    start = Math.Max(start, 0);
                    length = Math.Max(Math.Min(length, text.Length - start), 0);
                    return One(XdmItem.FromString(text.Substring(start, length)));
                case "integer":
                    var value = Text(a[0]);
                    return long.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed)
                        ? One(XdmItem.FromInteger(parsed))
                        : throw Fail("FORG0001", $"'{value}' is not an integer.");
                default:
                    throw Fail("XPST0017", $"The function {name} is not known.");
            }
        }
    }

    private sealed class Parser(string text, Func<string, int> resolveVariable, Func<string, string?> resolvePrefix)
    {
        private int _at;

        internal Node ParseAll()
        {
            var node = ParseSequence();
            Skip();
            return _at == text.Length ? node : throw new FormatException($"Unexpected '{text[_at]}' at {_at}.");
        }

        private Node ParseSequence()
        {
            var first = ParseOr();
            Skip();
            if (Peek() != ',')
            {
                return first;
            }

            var items = new List<Node> { first };
            while (Peek() == ',')
            {
                _at++;
                items.Add(ParseOr());
                Skip();
            }

            return new Sequence(items);
        }

        private Node ParseOr()
        {
            var left = ParseAnd();
            while (TryWord("or"))
            {
                left = new Binary("or", left, ParseAnd());
            }

            return left;
        }

        private Node ParseAnd()
        {
            var left = ParseComparison();
            while (TryWord("and"))
            {
                left = new Binary("and", left, ParseComparison());
            }

            return left;
        }

        private Node ParseComparison()
        {
            var left = ParseAdditive();
            Skip();
            foreach (var op in new[] { "!=", "<=", ">=", "=", "<", ">" })
            {
                if (string.CompareOrdinal(text, _at, op, 0, op.Length) == 0)
                {
                    _at += op.Length;
                    return new Binary(op, left, ParseAdditive());
                }
            }

            return left;
        }

        private Node ParseAdditive()
        {
            var left = ParseMultiplicative();
            while (true)
            {
                Skip();
                if (Peek() is '+' or '-')
                {
                    var op = text[_at++].ToString();
                    left = new Binary(op, left, ParseMultiplicative());
                }
                else
                {
                    return left;
                }
            }
        }

        private Node ParseMultiplicative()
        {
            var left = ParseUnary();
            while (true)
            {
                Skip();
                if (Peek() == '*')
                {
                    _at++;
                    left = new Binary("*", left, ParseUnary());
                }
                else if (TryWord("idiv"))
                {
                    left = new Binary("idiv", left, ParseUnary());
                }
                else if (TryWord("mod"))
                {
                    left = new Binary("mod", left, ParseUnary());
                }
                else
                {
                    return left;
                }
            }
        }

        private Node ParseUnary()
        {
            Skip();
            if (Peek() == '-')
            {
                _at++;
                return new Negate(ParseUnary());
            }

            return ParsePrimary();
        }

        private Node ParsePrimary()
        {
            Skip();
            var c = Peek();
            if (char.IsDigit(c))
            {
                var start = _at;
                while (char.IsDigit(Peek()))
                {
                    _at++;
                }

                var number = long.Parse(text.Substring(start, _at - start), CultureInfo.InvariantCulture);
                return new Literal(One(XdmItem.FromInteger(number)));
            }

            if (c is '\'' or '"')
            {
                var end = text.IndexOf(c, _at + 1);
                if (end < 0)
                {
                    throw new FormatException("Unterminated string literal.");
                }

                var literal = text.Substring(_at + 1, end - _at - 1);
                _at = end + 1;
                return new Literal(One(XdmItem.FromString(literal)));
            }

            if (c == '$')
            {
                _at++;
                var name = ReadName();
                var slot = resolveVariable(name);
                return slot < 0 ? throw new FormatException($"VAR:{name}") : new Variable(name, slot);
            }

            if (c == '.')
            {
                _at++;
                return new ContextItem();
            }

            if (c == '(')
            {
                _at++;
                Skip();
                if (Peek() == ')')
                {
                    _at++;
                    return new Literal(XdmSequence.Empty);
                }

                var inner = ParseSequence();
                Expect(')');
                return inner;
            }

            var word = ReadName();
            if (word.Length == 0)
            {
                throw new FormatException($"Unexpected '{c}' at {_at}.");
            }

            Skip();
            if (word == "if" && Peek() == '(')
            {
                _at++;
                var test = ParseSequence();
                Expect(')');
                ExpectWord("then");
                var whenTrue = ParseOr();
                ExpectWord("else");
                return new Conditional(test, whenTrue, ParseOr());
            }

            Expect('(');
            var arguments = new List<Node>();
            Skip();
            if (Peek() == ')')
            {
                _at++;
            }
            else
            {
                while (true)
                {
                    arguments.Add(ParseOr());
                    Skip();
                    if (Peek() == ',')
                    {
                        _at++;
                        continue;
                    }

                    Expect(')');
                    break;
                }
            }

            var colon = word.IndexOf(':');
            if (colon < 0)
            {
                return new Call(word, null, arguments);
            }

            var prefix = word.Substring(0, colon);
            var local = word.Substring(colon + 1);
            var uri = resolvePrefix(prefix) ?? throw new FormatException($"PREFIX:{prefix}");
            return prefix == "xs" ? new Call(local, null, arguments) : new Call(local, uri, arguments);
        }

        private string ReadName()
        {
            var start = _at;
            while (
                _at < text.Length
                && (
                    char.IsLetterOrDigit(text[_at])
                    || text[_at] is '_' or ':'
                    || (text[_at] == '-' && _at + 1 < text.Length && char.IsLetter(text[_at + 1]))
                )
            )
            {
                _at++;
            }

            return text.Substring(start, _at - start);
        }

        private bool TryWord(string word)
        {
            Skip();
            if (
                string.CompareOrdinal(text, _at, word, 0, word.Length) == 0
                && (_at + word.Length == text.Length || !char.IsLetterOrDigit(text[_at + word.Length]))
            )
            {
                _at += word.Length;
                return true;
            }

            return false;
        }

        private void ExpectWord(string word)
        {
            if (!TryWord(word))
            {
                throw new FormatException($"Expected '{word}' at {_at}.");
            }
        }

        private void Expect(char c)
        {
            Skip();
            if (Peek() != c)
            {
                throw new FormatException($"Expected '{c}' at {_at}.");
            }

            _at++;
        }

        private char Peek() => _at < text.Length ? text[_at] : '\0';

        private void Skip()
        {
            while (_at < text.Length && char.IsWhiteSpace(text[_at]))
            {
                _at++;
            }
        }
    }
}
