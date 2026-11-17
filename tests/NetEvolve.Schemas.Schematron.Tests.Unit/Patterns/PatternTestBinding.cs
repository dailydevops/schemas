namespace NetEvolve.Schemas.Schematron.Tests.Unit.Patterns;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Tests.Unit.Fakes;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.XPath.Xdm.Nodes;

/// <summary>
/// A binding that evaluates only the predicate texts the pattern tests use: <c>@name</c>, <c>@name = 'value'</c>,
/// <c>position() = N</c>, <c>position() = last()</c>, <c>true()</c>, and <c>bad</c> (an evaluation problem). A text that
/// contains <c>invalid</c> does not compile. Every compiled text is recorded, so a test sees what the matcher handed over.
/// </summary>
internal sealed class PatternTestBinding : IQueryBinding
{
    internal List<string> Compiled { get; } = [];

    internal int EvaluateCalls { get; private set; }

    public string Name => "pattern-test";

    public bool AllowsXsltInstructions => false;

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
        expression = new FakeQueryExpression(source);
        problem = default;
        if (source.Contains("invalid", StringComparison.Ordinal))
        {
            problem = new SchematronProblem(
                SchematronDiagnosticCodes.ExpressionInvalid,
                "invalid predicate",
                FakeModel.Origin,
                "XPST0003"
            );
            return false;
        }

        return true;
    }

    public IQueryRun BeginRun(QueryResources resources, CancellationToken cancellationToken) => new Run(this);

    private sealed class Run(PatternTestBinding owner) : IQueryRun
    {
        private SchematronProblem _problem;

        public QueryStatus TryEvaluateBoolean(QueryExpression expression, in QueryFocus focus, out bool value)
        {
            owner.EvaluateCalls++;
            var text = ((FakeQueryExpression)expression).Text;
            value = false;
            if (string.Equals(text, "bad", StringComparison.Ordinal))
            {
                _problem = new SchematronProblem(
                    SchematronDiagnosticCodes.EvaluationFailed,
                    "bad predicate",
                    FakeModel.Origin,
                    "XPDY0002"
                );
                return QueryStatus.Problem;
            }

            value = Evaluate(text, focus);
            return QueryStatus.Ok;
        }

        public QueryStatus TryEvaluate(QueryExpression expression, in QueryFocus focus, ISequenceSink sink) =>
            throw new NotSupportedException();

        public QueryStatus TryBindVariable(int slot, QueryExpression value, in QueryFocus focus) =>
            throw new NotSupportedException();

        public void BindVariable(int slot, string value) => throw new NotSupportedException();

        public SchematronProblem GetProblem() => _problem;

        public void Dispose() { }

        private static bool Evaluate(string text, in QueryFocus focus)
        {
            if (text.StartsWith("position() = ", StringComparison.Ordinal))
            {
                var operand = text.Substring("position() = ".Length);
                operand = operand.StartsWith('(', StringComparison.Ordinal) ? operand.Substring(1, operand.Length - 2) : operand;
                var expected = string.Equals(operand, "last()", StringComparison.Ordinal) ? focus.Size : Sum(operand);
                return focus.Position == expected;
            }

            if (text.StartsWith('@', StringComparison.Ordinal))
            {
                var parts = text.Split([" = "], 2, StringSplitOptions.None);
                return HasAttribute(focus.Node, parts[0].Substring(1), parts.Length == 2 ? parts[1].Trim('\'') : null);
            }

            return string.Equals(text, "true()", StringComparison.Ordinal);
        }

        private static int Sum(string operand)
        {
            var total = 0;
            foreach (var part in operand.Split('+'))
            {
                total += int.Parse(part, CultureInfo.InvariantCulture);
            }

            return total;
        }

        private static bool HasAttribute(XPathNode element, string name, string? value)
        {
            var names = element.Tree!.Names;
            for (var attribute = element.Node.FirstAttribute; !attribute.IsEmpty; attribute = attribute.NextAttribute)
            {
                if (
                    string.Equals(names.GetString(attribute.Name.Local), name, StringComparison.Ordinal)
                    && (value is null || string.Equals(attribute.StringValue, value, StringComparison.Ordinal))
                )
                {
                    return true;
                }
            }

            return false;
        }
    }
}
