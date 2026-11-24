namespace NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Instructions.Regex;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Tests.Unit.Fakes;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// A binding for the regex tests. Limitation: the XPath run of the pipeline does not offer the regex groups slot yet (#628), so the
/// tests cannot run real XPath; this binding knows a small language: <c>.</c>, <c>'text'</c>, <c>$name</c>, <c>()</c>, an integer,
/// a sequence <c>a, b</c>, and the calls <c>concat</c>, <c>position</c>, <c>last</c>, <c>count</c>, <c>error</c>, and
/// <c>regex-group</c>, which is the function of the package called with the slot of the run.
/// </summary>
internal sealed class RegexBinding : IQueryBinding
{
    public string Name => "xslt2";

    public bool AllowsXsltInstructions => true;

    public bool TryCompile(
        ReadOnlySpan<char> text,
        QueryExpressionKind kind,
        QueryStaticScope scope,
        out QueryExpression expression,
        out SchematronProblem problem
    )
    {
        var source = text.ToString();
        expression = null!;
        try
        {
            var node = new Parser(source, scope).ParseAll();
            var slots = new List<int>();
            node.Collect(slots);
            expression = new RegexExpression(node, [.. slots]);
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
        return new RegexRun();
    }

    internal static EvaluationException Failure(string constraint, string message) =>
        new(new SchematronProblem(SchematronDiagnosticCodes.EvaluationFailed, message, FakeModel.Origin, constraint));

    internal sealed class RegexExpression(Node node, int[] slots) : QueryExpression
    {
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
            : base("regex test evaluation failure") => Problem = problem;

        internal SchematronProblem Problem { get; }
    }

    internal abstract class Node
    {
        internal abstract XdmSequence Evaluate(RegexRun run, in QueryFocus focus);

        internal virtual void Collect(List<int> slots) { }
    }

    private sealed class Literal(XdmSequence value) : Node
    {
        internal override XdmSequence Evaluate(RegexRun run, in QueryFocus focus) => value;
    }

    private sealed class ContextItem : Node
    {
        internal override XdmSequence Evaluate(RegexRun run, in QueryFocus focus) =>
            focus.Item.Kind == XdmItemKind.None
                ? throw Failure("XPDY0002", "The context item is absent.")
                : XdmSequence.Create(focus.Item);
    }

    private sealed class Variable(string name, int slot) : Node
    {
        internal override XdmSequence Evaluate(RegexRun run, in QueryFocus focus) =>
            run.TryGetValue(slot, out var value)
                ? value
                : throw Failure("XPDY0002", $"The variable ${name} is not bound.");

        internal override void Collect(List<int> slots) => slots.Add(slot);
    }

    private sealed class Sequence(List<Node> parts) : Node
    {
        internal override XdmSequence Evaluate(RegexRun run, in QueryFocus focus)
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

    private sealed class Call(string name, List<Node> arguments) : Node
    {
        internal override XdmSequence Evaluate(RegexRun run, in QueryFocus focus)
        {
            var values = new XdmSequence[arguments.Count];
            for (var i = 0; i < values.Length; i++)
            {
                values[i] = arguments[i].Evaluate(run, in focus);
            }

            return name switch
            {
                "count" => XdmSequence.Create(XdmItem.FromInteger(values[0].Count)),
                "concat" => Text(
                    string.Concat(Array.ConvertAll(values, v => v.Count == 0 ? string.Empty : Show(v[0])))
                ),
                "position" => XdmSequence.Create(XdmItem.FromInteger(focus.Position)),
                "last" => XdmSequence.Create(XdmItem.FromInteger(focus.ResolveSize())),
                "error" => throw Failure("FOER0000", "error() was called."),
                "regex-group" => CallRegexGroup(run, values[0]),
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

        private static XdmSequence CallRegexGroup(RegexRun run, XdmSequence argument)
        {
            try
            {
                return run.CallRegexGroup(argument);
            }
            catch (XPathException exception)
            {
                throw Failure(exception.Diagnostic.Code.ToString(), exception.Message);
            }
        }

        private static XdmSequence Text(string value) => XdmSequence.Create(XdmItem.FromString(value));

        private static string Show(XdmItem item) =>
            item.TryGetString(out var text) && text is not null ? text
            : item.TryGetInteger(out var integer) ? integer.ToString(CultureInfo.InvariantCulture)
            : item.Kind.ToString();
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
            var parts = new List<Node> { ParseTerm() };
            while (Peek(','))
            {
                _at++;
                parts.Add(ParseTerm());
            }

            return parts.Count == 1 ? parts[0] : new Sequence(parts);
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
                case '$':
                    _at++;
                    return ParseVariable(Name());
                case >= '0' and <= '9':
                    return ParseInteger();
                default:
                    return ParseCall(Name());
            }
        }

        private Literal ParseInteger()
        {
            var start = _at;
            while (_at < text.Length && char.IsDigit(text[_at]))
            {
                _at++;
            }

            var number = long.Parse(text.AsSpan(start, _at - start), NumberStyles.None, CultureInfo.InvariantCulture);
            return new Literal(XdmSequence.Create(XdmItem.FromInteger(number)));
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

        private Call ParseCall(string name)
        {
            Expect('(');
            var arguments = new List<Node>();
            if (!Peek(')'))
            {
                arguments.Add(ParseTerm());
                while (Peek(','))
                {
                    _at++;
                    arguments.Add(ParseTerm());
                }
            }

            Expect(')');
            return new Call(name, arguments);
        }

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
