namespace NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Instructions.Grouping;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Tests.Unit.Fakes;
using NetEvolve.Schemas.Schematron.Xslt.Instructions.Grouping;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.DateTime;
using NetEvolve.Schemas.Xml.Datatypes.Decimals;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// A binding for the grouping tests. Limitation: the XPath evaluator (XP-19, #297) is not implemented, so the tests of the grouping
/// instruction cannot run real XPath; this binding knows a small language and nothing else. Terms: <c>.</c>, <c>*</c> (the child
/// elements of the context node), <c>@name</c>, <c>'text'</c>, <c>$name</c>, <c>()</c>, a typed literal such as <c>int:1</c>,
/// <c>dec:1.5</c>, <c>dbl:1e0</c>, <c>dbl:NaN</c>, <c>bool:true</c>, or <c>date:2020-01-01</c>, a sequence <c>(a, b)</c> or <c>a, b</c>,
/// the comparison <c>a = b</c> over the text of the items, and the calls <c>count</c>, <c>key</c> (the text of the first item), <c>show</c> (the text of every
/// item, separated by a space), <c>concat</c>, <c>position</c>, <c>last</c>, <c>error</c> (fails), and the two functions under test
/// <c>current-group</c> and <c>current-grouping-key</c>, which are the host functions of the package called with the slots of the run.
/// The text of an element is its name, then <c>=k</c> and <c>/j</c> for the attributes <c>k</c> and <c>j</c>, as in
/// <see cref="GroupDocument"/>. Every compiled text is recorded.
/// </summary>
internal sealed class GroupBinding : IQueryBinding
{
    internal GroupBinding(string name = "xslt2")
    {
        ArgumentNullException.ThrowIfNull(name);

        Name = name;
        Functions = XsltGroupingFunctions.Create(this);
    }

    internal List<string> Compiled { get; } = [];

    internal XPathHostFunction[] Functions { get; }

    internal GroupRun? LastRun { get; private set; }

    public string Name { get; }

    public bool AllowsXsltInstructions => Name.StartsWith("xslt", StringComparison.Ordinal);

    public bool TryCompile(
        ReadOnlySpan<char> text,
        QueryExpressionKind kind,
        QueryStaticScope scope,
        out QueryExpression expression,
        out SchematronProblem problem
    )
    {
        var source = text.ToString();
        Compiled.Add(source);
        expression = null!;
        try
        {
            var node = new Parser(source, scope).ParseAll();
            var slots = new List<int>();
            node.Collect(slots);
            expression = new GroupExpression(source, node, [.. slots]);
            problem = default;
            return true;
        }
        catch (FormatException exception)
        {
            problem = new SchematronProblem(
                SchematronDiagnosticCodes.ExpressionInvalid,
                exception.Message,
                FakeModel.Origin,
                "XPST0003"
            );
            return false;
        }
    }

    public IQueryRun BeginRun(QueryResources resources, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        LastRun = new GroupRun(this);
        return LastRun;
    }

    internal sealed class GroupExpression(string text, Node node, int[] slots) : QueryExpression
    {
        internal string Text { get; } = text;

        internal Node Node { get; } = node;

        internal override ReadOnlySpan<int> ReferencedSlots => slots;
    }

    internal sealed class EvaluationException : Exception
    {
        internal EvaluationException()
            : this(default(SchematronProblem)) { }

        internal EvaluationException(string message)
            : base(message) { }

        internal EvaluationException(string message, Exception innerException)
            : base(message, innerException) { }

        internal EvaluationException(SchematronProblem problem)
            : base("group test evaluation failure") => Problem = problem;

        internal SchematronProblem Problem { get; }
    }

    internal static EvaluationException Failure(string constraint, string message) =>
        new(new SchematronProblem(SchematronDiagnosticCodes.EvaluationFailed, message, FakeModel.Origin, constraint));

    /// <summary>Gives the text of an item as the language defines it.</summary>
    internal static string Show(in XdmItem item)
    {
        if (item.TryGetNode(out var node))
        {
            if (node.Node.Kind != XmlNodeKind.Element)
            {
                return node.StringValue();
            }

            var names = node.Tree!.Names;
            var text = new StringBuilder(names.GetString(node.Name().Local));
            for (var attribute = node.FirstAttribute(); !attribute.IsEmpty(); attribute = attribute.NextAttribute())
            {
                var local = names.GetString(attribute.Name().Local);
                _ = text.Append(string.Equals(local, "k", StringComparison.Ordinal) ? '=' : '/')
                    .Append(attribute.StringValue());
            }

            return text.ToString();
        }

        if (item.TryGetString(out var value) && value is not null)
        {
            return value;
        }

        if (item.TryGetInteger(out var integer))
        {
            return integer.ToString(CultureInfo.InvariantCulture);
        }

        if (item.TryGetBoolean(out var boolean))
        {
            return boolean ? "true" : "false";
        }

        if (item.TryGetDouble(out var number))
        {
            return double.IsNaN(number) ? "NaN" : number.ToString("R", CultureInfo.InvariantCulture);
        }

        return item.Kind.ToString();
    }

    internal abstract class Node
    {
        internal abstract XdmSequence Evaluate(GroupRun run, in QueryFocus focus);

        internal virtual void Collect(List<int> slots) { }
    }

    private sealed class Literal(XdmSequence value) : Node
    {
        internal override XdmSequence Evaluate(GroupRun run, in QueryFocus focus) => value;
    }

    private sealed class ContextItem : Node
    {
        internal override XdmSequence Evaluate(GroupRun run, in QueryFocus focus) =>
            focus.Item.Kind == XdmItemKind.None
                ? throw Failure("XPDY0002", "The context item is absent.")
                : XdmSequence.Create(focus.Item);
    }

    private sealed class Children : Node
    {
        internal override XdmSequence Evaluate(GroupRun run, in QueryFocus focus)
        {
            var items = new List<XdmItem>();
            for (var child = focus.Node.FirstChild(); !child.IsEmpty(); child = child.NextSibling())
            {
                if (child.Node.Kind == XmlNodeKind.Element)
                {
                    items.Add(XdmItem.FromNode(child));
                }
            }

            return XdmSequence.Create(items.ToArray());
        }
    }

    private sealed class Attribute(string name) : Node
    {
        internal override XdmSequence Evaluate(GroupRun run, in QueryFocus focus)
        {
            if (focus.Node.Tree is null)
            {
                return XdmSequence.Empty;
            }

            var names = focus.Node.Tree.Names;
            for (var a = focus.Node.FirstAttribute(); !a.IsEmpty(); a = a.NextAttribute())
            {
                if (string.Equals(names.GetString(a.Name().Local), name, StringComparison.Ordinal))
                {
                    return XdmSequence.Create(XdmItem.FromNode(a));
                }
            }

            return XdmSequence.Empty;
        }
    }

    private sealed class Variable(string name, int slot) : Node
    {
        internal override XdmSequence Evaluate(GroupRun run, in QueryFocus focus) =>
            run.TryGetValue(slot, out var value)
                ? value
                : throw Failure("XPDY0002", $"The variable ${name} is not bound.");

        internal override void Collect(List<int> slots) => slots.Add(slot);
    }

    private sealed class Sequence(List<Node> parts) : Node
    {
        internal override XdmSequence Evaluate(GroupRun run, in QueryFocus focus)
        {
            var items = new List<XdmItem>();
            foreach (var part in parts)
            {
                var value = part.Evaluate(run, in focus);
                for (var i = 0; i < value.Count; i++)
                {
                    items.Add(value[i]);
                }
            }

            return XdmSequence.Create(items.ToArray());
        }

        internal override void Collect(List<int> slots)
        {
            foreach (var part in parts)
            {
                part.Collect(slots);
            }
        }
    }

    private sealed class Equal(Node left, Node right) : Node
    {
        internal override XdmSequence Evaluate(GroupRun run, in QueryFocus focus)
        {
            var l = left.Evaluate(run, in focus);
            var r = right.Evaluate(run, in focus);
            for (var i = 0; i < l.Count; i++)
            {
                for (var j = 0; j < r.Count; j++)
                {
                    if (string.Equals(Show(l[i]), Show(r[j]), StringComparison.Ordinal))
                    {
                        return XdmSequence.Create(XdmItem.FromBoolean(true));
                    }
                }
            }

            return XdmSequence.Create(XdmItem.FromBoolean(false));
        }

        internal override void Collect(List<int> slots)
        {
            left.Collect(slots);
            right.Collect(slots);
        }
    }

    private sealed class Call(string name, List<Node> arguments) : Node
    {
        internal override XdmSequence Evaluate(GroupRun run, in QueryFocus focus)
        {
            var values = new XdmSequence[arguments.Count];
            for (var i = 0; i < values.Length; i++)
            {
                values[i] = arguments[i].Evaluate(run, in focus);
            }

            return name switch
            {
                "count" => XdmSequence.Create(XdmItem.FromInteger(values[0].Count)),
                "key" => Text(values[0].Count == 0 ? string.Empty : Show(values[0][0])),
                "show" => Text(Join(values[0])),
                "concat" => Text(string.Concat(Array.ConvertAll(values, v => Join(v)))),
                "position" => XdmSequence.Create(XdmItem.FromInteger(focus.Position)),
                "last" => XdmSequence.Create(XdmItem.FromInteger(focus.ResolveSize())),
                "error" => throw Failure("FOER0000", "error() was called."),
                "current-group" or "current-grouping-key" => run.CallHost(name),
                _ => throw Failure("XPST0017", $"No function {name}."),
            };
        }

        internal override void Collect(List<int> slots)
        {
            foreach (var argument in arguments)
            {
                argument.Collect(slots);
            }
        }

        private static XdmSequence Text(string value) => XdmSequence.Create(XdmItem.FromString(value));

        private static string Join(XdmSequence value)
        {
            var parts = new string[value.Count];
            for (var i = 0; i < parts.Length; i++)
            {
                parts[i] = Show(value[i]);
            }

            return string.Join(" ", parts);
        }
    }

    private sealed class Parser(string text, QueryStaticScope scope)
    {
        private int _at;

        internal Node ParseAll()
        {
            var node = ParseSequence();
            SkipSpace();
            return _at == text.Length ? node : throw new FormatException($"Unexpected '{text[_at]}' at {_at}.");
        }

        private Node ParseSequence()
        {
            var parts = new List<Node> { ParseComparison() };
            while (Peek(','))
            {
                _at++;
                parts.Add(ParseComparison());
            }

            return parts.Count == 1 ? parts[0] : new Sequence(parts);
        }

        private Node ParseComparison()
        {
            var left = ParseTerm();
            if (!Peek('='))
            {
                return left;
            }

            _at++;
            return new Equal(left, ParseTerm());
        }

        private Node ParseTerm()
        {
            SkipSpace();
            if (_at >= text.Length)
            {
                throw new FormatException("A term is missing.");
            }

            switch (text[_at])
            {
                case '(':
                    _at++;
                    if (Peek(')'))
                    {
                        _at++;
                        return new Literal(XdmSequence.Empty);
                    }

                    var inner = ParseSequence();
                    Expect(')');
                    return inner;
                case '\'':
                    var length = text.AsSpan(_at + 1).IndexOf('\'');
                    if (length < 0)
                    {
                        throw new FormatException("A string literal is not closed.");
                    }

                    var value = text.Substring(_at + 1, length);
                    _at += length + 2;
                    return new Literal(XdmSequence.Create(XdmItem.FromString(value)));
                case '.':
                    _at++;
                    return new ContextItem();
                case '*':
                    _at++;
                    return new Children();
                case '@':
                    _at++;
                    return new Attribute(Name());
                case '$':
                    _at++;
                    return ParseVariable(Name());
                default:
                    return ParseNamed();
            }
        }

        private Variable ParseVariable(string name)
        {
            for (var slot = scope.VariableNames.Count - 1; slot >= 0; slot--)
            {
                if (string.Equals(scope.VariableNames[slot], name, StringComparison.Ordinal))
                {
                    return new Variable(name, slot);
                }
            }

            throw new FormatException($"The variable ${name} is not in scope.");
        }

        private Node ParseNamed()
        {
            var name = Name();
            if (_at < text.Length && text[_at] == '(')
            {
                _at++;
                var arguments = new List<Node>();
                if (!Peek(')'))
                {
                    arguments.Add(ParseComparison());
                    while (Peek(','))
                    {
                        _at++;
                        arguments.Add(ParseComparison());
                    }
                }

                Expect(')');
                return new Call(name, arguments);
            }

            if (_at < text.Length && text[_at] == ':')
            {
                _at++;
                var start = _at;
                while (_at < text.Length && text[_at] is not (',' or ')' or ' ' or '='))
                {
                    _at++;
                }

                return new Literal(Typed(name, text.Substring(start, _at - start)));
            }

            throw new FormatException($"The name '{name}' is not known.");
        }

        private static XdmSequence Typed(string type, string value) =>
            XdmSequence.Create(
                type switch
                {
                    "int" => XdmItem.FromInteger(long.Parse(value, CultureInfo.InvariantCulture)),
                    "dec" => XdmItem.FromDecimal(XsdDecimal.Parse(value.AsSpan()).Value),
                    "dbl" => XdmItem.FromDouble(double.Parse(value, CultureInfo.InvariantCulture)),
                    "bool" => XdmItem.FromBoolean(string.Equals(value, "true", StringComparison.Ordinal)),
                    "date" => XdmItem.FromDateTime(
                        XsdDateTime.Parse(value, XsdDateTimeKind.Date, XsdVersion.V10).Value
                    ),
                    _ => throw new FormatException($"The type '{type}' is not known."),
                }
            );

        private string Name()
        {
            var start = _at;
            while (_at < text.Length && (char.IsLetterOrDigit(text[_at]) || text[_at] is '-' or '_'))
            {
                _at++;
            }

            return _at == start
                ? throw new FormatException($"A name is missing at {_at}.")
                : text.Substring(start, _at - start);
        }

        private bool Peek(char expected)
        {
            SkipSpace();
            return _at < text.Length && text[_at] == expected;
        }

        private void Expect(char expected)
        {
            if (!Peek(expected))
            {
                throw new FormatException($"'{expected}' is missing at {_at}.");
            }

            _at++;
        }

        private void SkipSpace()
        {
            while (_at < text.Length && text[_at] == ' ')
            {
                _at++;
            }
        }
    }
}
