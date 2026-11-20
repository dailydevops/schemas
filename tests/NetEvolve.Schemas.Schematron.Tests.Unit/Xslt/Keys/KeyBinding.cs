namespace NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Keys;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Tests.Unit.Fakes;
using NetEvolve.Schemas.Schematron.Xslt.Keys;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.DateTime;
using NetEvolve.Schemas.Xml.Datatypes.Decimals;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// A binding for the key tests. The XPath evaluator (XP-19, #297) is not implemented, so the tests of the key package cannot run real
/// XPath; this binding knows exactly the texts the tests use: <c>@name</c>, <c>@name = 'value'</c>, <c>.</c>, <c>'text'</c>,
/// <c>$name</c>, <c>()</c>, <c>a, b</c>, a typed literal such as <c>int:1</c>, <c>dec:1.5</c>, <c>dbl:1e0</c>, <c>flt:1.5</c>,
/// <c>bool:true</c>, or <c>date:2020-01-01</c>, and a call <c>f:name(argument)</c> of a function that the test hands over. The text
/// <c>bad</c> fails when evaluated and a text with <c>invalid</c> does not compile. Every compiled text and every evaluation is counted.
/// </summary>
internal sealed class KeyBinding : IQueryBinding
{
    internal List<string> Compiled { get; } = [];

    /// <summary>Gets the number of evaluations of a value expression, a nodes expression, and a boolean test.</summary>
    internal int Evaluations { get; private set; }

    /// <summary>Gets or sets the function behind <c>f:name(argument)</c>; it gets the local name and the evaluated argument.</summary>
    internal Func<string, XdmSequence, XdmSequence>? Function { get; set; }

    internal KeyRun? LastRun { get; private set; }

    public string Name => "key-test";

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
        Compiled.Add(source);
        expression = new KeyExpression(source, SlotsOf(source, scope));
        problem = default;
        if (!source.Contains("invalid", StringComparison.Ordinal))
        {
            return true;
        }

        problem = new SchematronProblem(
            SchematronDiagnosticCodes.ExpressionInvalid,
            "invalid expression",
            FakeModel.Origin,
            "XPST0003"
        );
        return false;
    }

    public IQueryRun BeginRun(QueryResources resources, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        LastRun = new KeyRun(this);
        return LastRun;
    }

    // Every $name in the text reads the slot of that name; the last declaration of a name wins.
    private static int[] SlotsOf(string source, QueryStaticScope scope)
    {
        var slots = new List<int>();
        var parts = source.Split('$');
        for (var i = 1; i < parts.Length; i++)
        {
            var length = 0;
            while (length < parts[i].Length && char.IsLetterOrDigit(parts[i][length]))
            {
                length++;
            }

            var name = parts[i].Substring(0, length);
            for (var slot = scope.VariableNames.Count - 1; slot >= 0; slot--)
            {
                if (string.Equals(scope.VariableNames[slot], name, StringComparison.Ordinal))
                {
                    slots.Add(slot);
                    break;
                }
            }
        }

        return [.. slots];
    }

    internal sealed class KeyExpression(string text, int[] slots) : QueryExpression
    {
        internal string Text { get; } = text;

        internal override ReadOnlySpan<int> ReferencedSlots => slots;
    }

    internal sealed class KeyRun(KeyBinding owner) : IQueryRun
    {
        private SchematronProblem _problem;

        public QueryStatus TryEvaluateBoolean(QueryExpression expression, in QueryFocus focus, out bool value)
        {
            owner.Evaluations++;
            value = false;
            var text = ((KeyExpression)expression).Text;
            try
            {
                value = text switch
                {
                    "bad" => throw Failure("bad test"),
                    "true()" => true,
                    _ when Starts(text, '@') => HasAttribute(focus.Node, text),
                    _ => throw Failure($"The test '{text}' is not known."),
                };
                return QueryStatus.Ok;
            }
            catch (EvaluationException failure)
            {
                _problem = failure.Problem;
                return QueryStatus.Problem;
            }
        }

        public QueryStatus TryEvaluate(QueryExpression expression, in QueryFocus focus, ISequenceSink sink)
        {
            owner.Evaluations++;
            try
            {
                var value = Evaluate(((KeyExpression)expression).Text.Trim(), focus);
                for (var i = 0; i < value.Count; i++)
                {
                    var item = value[i];
                    sink.Item(in item);
                }

                return QueryStatus.Ok;
            }
            catch (EvaluationException failure)
            {
                _problem = failure.Problem;
                return QueryStatus.Problem;
            }
        }

        public QueryStatus TryBindVariable(int slot, QueryExpression value, in QueryFocus focus) =>
            throw new NotSupportedException();

        public void BindVariable(int slot, string value) => throw new NotSupportedException();

        public SchematronProblem GetProblem() => _problem;

        public void Dispose() { }

        private static bool Starts(string text, char first) => text.Length > 0 && text[0] == first;

        private static EvaluationException Failure(string message) =>
            new(
                new SchematronProblem(SchematronDiagnosticCodes.EvaluationFailed, message, FakeModel.Origin, "XPDY0002")
            );

        private static bool HasAttribute(XPathNode element, string text)
        {
            var parts = text.Split([" = "], 2, StringSplitOptions.None);
            var attribute = Attribute(element, parts[0].Substring(1));
            return !attribute.IsEmpty()
                && (
                    parts.Length == 1
                    || string.Equals(attribute.StringValue(), parts[1].Trim('\''), StringComparison.Ordinal)
                );
        }

        private static XPathNode Attribute(XPathNode element, string name)
        {
            var names = element.Tree!.Names;
            for (var attribute = element.FirstAttribute(); !attribute.IsEmpty(); attribute = attribute.NextAttribute())
            {
                if (string.Equals(names.GetString(attribute.Name().Local), name, StringComparison.Ordinal))
                {
                    return attribute;
                }
            }

            return default;
        }

        private XdmSequence Evaluate(string text, in QueryFocus focus)
        {
            if (text.Contains(',', StringComparison.Ordinal) && !text.Contains('(', StringComparison.Ordinal))
            {
                var items = new List<XdmItem>();
                foreach (var part in text.Split(','))
                {
                    var one = Evaluate(part.Trim(), focus);
                    for (var i = 0; i < one.Count; i++)
                    {
                        items.Add(one[i]);
                    }
                }

                return XdmSequence.Create(items.ToArray());
            }

            return EvaluateSingle(text, focus);
        }

        private XdmSequence EvaluateSingle(string text, in QueryFocus focus)
        {
            if (string.Equals(text, "bad", StringComparison.Ordinal))
            {
                throw Failure("bad expression");
            }

            if (string.Equals(text, "()", StringComparison.Ordinal))
            {
                return XdmSequence.Empty;
            }

            if (string.Equals(text, ".", StringComparison.Ordinal))
            {
                return XdmSequence.Create(focus.Item);
            }

            if (Starts(text, '@'))
            {
                var attribute = Attribute(focus.Node, text.Substring(1));
                return attribute.IsEmpty() ? XdmSequence.Empty : XdmSequence.Create(XdmItem.FromNode(attribute));
            }

            if (Starts(text, '\''))
            {
                return XdmSequence.Create(XdmItem.FromString(text.Trim('\'')));
            }

            if (text.StartsWith("f:", StringComparison.Ordinal))
            {
                var open = text.IndexOf('(', StringComparison.Ordinal);
                var argument = Evaluate(text.Substring(open + 1, text.Length - open - 2).Trim(), focus);
                return owner.Function is { } function
                    ? function(text.Substring(2, open - 2), argument)
                    : throw Failure("No function.");
            }

            return Typed(text);
        }

        internal static XdmSequence Typed(string text)
        {
            var colon = text.IndexOf(':', StringComparison.Ordinal);
            var value = text.Substring(colon + 1);
            return XdmSequence.Create(
                text.Substring(0, colon) switch
                {
                    "int" => XdmItem.FromInteger(long.Parse(value, CultureInfo.InvariantCulture)),
                    "dec" => XdmItem.FromDecimal(XsdDecimal.Parse(value.AsSpan()).Value),
                    "dbl" => XdmItem.FromDouble(double.Parse(value, CultureInfo.InvariantCulture)),
                    "flt" => XdmItem.FromFloat(float.Parse(value, CultureInfo.InvariantCulture)),
                    "bool" => XdmItem.FromBoolean(string.Equals(value, "true", StringComparison.Ordinal)),
                    "date" => XdmItem.FromDateTime(
                        XsdDateTime.Parse(value, XsdDateTimeKind.Date, XsdVersion.V10).Value
                    ),
                    _ => throw Failure($"The text '{text}' is not known."),
                }
            );
        }
    }

    private sealed class EvaluationException : Exception
    {
        internal EvaluationException()
            : this(default(SchematronProblem)) { }

        internal EvaluationException(string message)
            : base(message) { }

        internal EvaluationException(string message, Exception innerException)
            : base(message, innerException) { }

        internal EvaluationException(SchematronProblem problem)
            : base("key test evaluation failure") => Problem = problem;

        internal SchematronProblem Problem { get; }
    }
}
