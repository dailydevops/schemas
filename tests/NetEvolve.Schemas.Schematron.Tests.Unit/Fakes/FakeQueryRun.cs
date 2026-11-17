namespace NetEvolve.Schemas.Schematron.Tests.Unit.Fakes;

using System;
using System.Collections.Generic;
using System.Threading;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Xdm.Nodes;

/// <summary>A run that records calls and reports a problem for the text <c>bad</c>.</summary>
internal sealed class FakeQueryRun : IQueryRun
{
    private SchematronProblem _problem;

    internal List<string> Calls { get; } = [];

    internal bool Disposed { get; private set; }

    public QueryStatus TryEvaluateBoolean(QueryExpression expression, in QueryFocus focus, out bool value)
    {
        Calls.Add("boolean");
        value = ((FakeQueryExpression)expression).Text.Equals("bad", StringComparison.Ordinal) is false;
        return Status(expression);
    }

    public QueryStatus TryEvaluate(QueryExpression expression, in QueryFocus focus, ISequenceSink sink)
    {
        Calls.Add("evaluate");
        sink.Text(((FakeQueryExpression)expression).Text.AsSpan());
        return Status(expression);
    }

    public QueryStatus TryBindVariable(int slot, QueryExpression value, in QueryFocus focus)
    {
        Calls.Add("bind-expression");
        return Status(value);
    }

    public void BindVariable(int slot, string value) => Calls.Add("bind-literal:" + value);

    public SchematronProblem GetProblem() => _problem;

    public void Dispose() => Disposed = true;

    private QueryStatus Status(QueryExpression expression)
    {
        if (((FakeQueryExpression)expression).Text.Equals("bad", StringComparison.Ordinal) is false)
        {
            return QueryStatus.Ok;
        }

        _problem = new SchematronProblem(
            SchematronDiagnosticCodes.EvaluationFailed,
            "bad expression",
            FakeModel.Origin,
            "XPDY0002"
        );
        return QueryStatus.Problem;
    }
}
