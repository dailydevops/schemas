namespace NetEvolve.Schemas.Schematron.Tests.Unit.Evaluation.Assertions;

using System;
using System.Collections.Generic;
using System.Threading;
using NetEvolve.Schemas.Schematron;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Tests.Unit.Fakes;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// A fake binding with scripted results. A test expression is <see langword="true"/>, <see langword="false"/>, <c>()</c> (the empty sequence, which is
/// false), or <c>bad</c> (a dynamic error). A value expression yields the items or the text a test registered for its text, the
/// empty sequence otherwise, and <c>bad</c> fails. <c>invalid</c> does not compile; <c>$name</c> reads the slot of that name.
/// </summary>
internal sealed class AssertionTestBinding : IQueryBinding
{
    internal Dictionary<string, XdmItem[]> Items { get; } = new(StringComparer.Ordinal);

    internal Dictionary<string, string> Texts { get; } = new(StringComparer.Ordinal);

    /// <summary>Gets the value expressions that push their items and then fail, as a binding does that errors partway through a sequence.</summary>
    internal HashSet<string> FailAfterPush { get; } = new(StringComparer.Ordinal);

    /// <summary>Gets the text of every compile call with the kind and the scope it was compiled in.</summary>
    internal List<(string Text, QueryExpressionKind Kind, QueryStaticScope Scope)> Compiled { get; } = [];

    /// <summary>Gets the text of every evaluation in order; switched off by the allocation tests.</summary>
    internal List<string> Evaluated { get; } = [];

    internal bool Record { get; set; } = true;

    public string Name => "assertion-test";

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
        Compiled.Add((source, kind, scope));
        expression = null!;
        problem = default;
        if (source.Contains("invalid", StringComparison.Ordinal))
        {
            problem = new SchematronProblem(
                SchematronDiagnosticCodes.ExpressionInvalid,
                "invalid expression",
                FakeModel.Origin,
                "XPST0003"
            );
            return false;
        }

        var slots = new List<int>();
        if (source.Length > 0 && source[0] == '$')
        {
            var slot = Resolve(scope, source.Substring(1));
            if (slot < 0)
            {
                problem = new SchematronProblem(
                    SchematronDiagnosticCodes.VariableUndefined,
                    $"The variable '{source}' is not in scope.",
                    FakeModel.Origin,
                    "XPST0008"
                );
                return false;
            }

            slots.Add(slot);
        }

        expression = new AssertionTestExpression(source, kind, [.. slots]);
        return true;
    }

    public IQueryRun BeginRun(QueryResources resources, CancellationToken cancellationToken) => new Run(this);

    private static int Resolve(QueryStaticScope scope, string name)
    {
        for (var slot = scope.VariableNames.Count - 1; slot >= 0; slot--)
        {
            if (string.Equals(scope.VariableNames[slot], name, StringComparison.Ordinal))
            {
                return slot;
            }
        }

        return -1;
    }

    private sealed class Run(AssertionTestBinding owner) : IQueryRun
    {
        private SchematronProblem _problem;

        public QueryStatus TryEvaluateBoolean(QueryExpression expression, in QueryFocus focus, out bool value)
        {
            var text = ((AssertionTestExpression)expression).Text;
            if (owner.Record)
            {
                owner.Evaluated.Add(text);
            }

            value = string.Equals(text, "true", StringComparison.Ordinal);
            return string.Equals(text, "bad", StringComparison.Ordinal) ? Fail() : QueryStatus.Ok;
        }

        public QueryStatus TryEvaluate(QueryExpression expression, in QueryFocus focus, ISequenceSink sink)
        {
            var text = ((AssertionTestExpression)expression).Text;
            if (owner.Record)
            {
                owner.Evaluated.Add(text);
            }

            if (string.Equals(text, "bad", StringComparison.Ordinal))
            {
                return Fail();
            }

            if (owner.Texts.TryGetValue(text, out var pushed))
            {
                sink.Text(pushed.AsSpan());
            }
            else if (owner.Items.TryGetValue(text, out var items))
            {
                for (var i = 0; i < items.Length; i++)
                {
                    sink.Item(in items[i]);
                }
            }

            return owner.FailAfterPush.Contains(text) ? Fail() : QueryStatus.Ok;
        }

        public QueryStatus TryBindVariable(int slot, QueryExpression value, in QueryFocus focus) =>
            throw new NotSupportedException("The assertion code binds through ISlotBinder only.");

        public void BindVariable(int slot, string value) =>
            throw new NotSupportedException("The assertion code binds through ISlotBinder only.");

        public SchematronProblem GetProblem() => _problem;

        public void Dispose() { }

        private QueryStatus Fail()
        {
            _problem = new SchematronProblem(
                SchematronDiagnosticCodes.EvaluationFailed,
                "bad expression",
                FakeModel.Origin,
                "XPDY0002"
            );
            return QueryStatus.Problem;
        }
    }
}
