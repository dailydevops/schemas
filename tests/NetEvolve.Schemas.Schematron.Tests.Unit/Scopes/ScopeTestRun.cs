namespace NetEvolve.Schemas.Schematron.Tests.Unit.Scopes;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Schematron;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Tests.Unit.Fakes;
using NetEvolve.Schemas.XPath.Xdm.Nodes;

/// <summary>
/// A run that records every variable binding. The text <c>bad</c> fails with a dynamic error; for a declared type the result
/// type follows the leading function name (<c>string(</c>, <c>count(</c>, <c>number(</c>) and a mismatch is a type error.
/// </summary>
internal sealed class ScopeTestRun : IQueryRun
{
    private SchematronProblem _problem;

    internal List<(int Slot, string Text, XPathNode Node)> Binds { get; } = [];

    internal List<(int Slot, string Value)> Literals { get; } = [];

    public QueryStatus TryBindVariable(int slot, QueryExpression value, in QueryFocus focus)
    {
        var expression = (ScopeTestExpression)value;
        Binds.Add((slot, expression.Text, focus.Node));
        if (expression.Text.Equals("bad", StringComparison.Ordinal))
        {
            _problem = new SchematronProblem(
                SchematronDiagnosticCodes.EvaluationFailed,
                "dynamic error",
                FakeModel.Origin,
                "FOAR0001"
            );
            return QueryStatus.Problem;
        }

        if (expression.DeclaredType is { } type && !Matches(expression.Text, type))
        {
            _problem = new SchematronProblem(
                SchematronDiagnosticCodes.LetType,
                $"The value does not match '{type}'.",
                FakeModel.Origin,
                "XPTY0004"
            );
            return QueryStatus.Problem;
        }

        return QueryStatus.Ok;
    }

    public void BindVariable(int slot, string value) => Literals.Add((slot, value));

    public SchematronProblem GetProblem() => _problem;

    public QueryStatus TryEvaluateBoolean(QueryExpression expression, in QueryFocus focus, out bool value)
    {
        value = true;
        return QueryStatus.Ok;
    }

    public QueryStatus TryEvaluate(QueryExpression expression, in QueryFocus focus, ISequenceSink sink) =>
        QueryStatus.Ok;

    public void Dispose() { }

    private static bool Matches(string text, string type) =>
        type switch
        {
            "xs:string" => text.StartsWith("string(", StringComparison.Ordinal),
            "xs:integer" => text.StartsWith("count(", StringComparison.Ordinal),
            "xs:double" => text.StartsWith("number(", StringComparison.Ordinal)
                || text.StartsWith("count(", StringComparison.Ordinal),
            _ => false,
        };
}
