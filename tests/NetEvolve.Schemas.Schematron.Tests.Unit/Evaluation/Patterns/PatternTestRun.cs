namespace NetEvolve.Schemas.Schematron.Tests.Unit.Evaluation.Patterns;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Tests.Unit.Evaluation.Assertions;
using NetEvolve.Schemas.Schematron.Tests.Unit.Fakes;

/// <summary>
/// A run that delegates the evaluation of assertions to the assertion test binding, binds variables itself (the text <c>bad</c>
/// fails), and lets the scripted matcher raise a dynamic error through <see cref="Raise"/>.
/// </summary>
internal sealed class PatternTestRun : IQueryRun
{
    private readonly IQueryRun _inner;
    private readonly List<string> _log;
    private SchematronProblem _problem;

    internal PatternTestRun(AssertionTestBinding binding, List<string> log)
    {
        _inner = binding.BeginRun(null!, default);
        _log = log;
    }

    public QueryStatus TryEvaluateBoolean(QueryExpression expression, in QueryFocus focus, out bool value) =>
        _inner.TryEvaluateBoolean(expression, in focus, out value);

    public QueryStatus TryEvaluate(QueryExpression expression, in QueryFocus focus, ISequenceSink sink) =>
        _inner.TryEvaluate(expression, in focus, sink);

    public QueryStatus TryBindVariable(int slot, QueryExpression value, in QueryFocus focus)
    {
        _log.Add("bind:" + slot);
        if (string.Equals(((AssertionTestExpression)value).Text, "bad", StringComparison.Ordinal))
        {
            Raise("XPDY0002");
            return QueryStatus.Problem;
        }

        return QueryStatus.Ok;
    }

    public void BindVariable(int slot, string value) => _log.Add("bind-literal:" + slot);

    public SchematronProblem GetProblem() => _problem.Message is null ? _inner.GetProblem() : _problem;

    public void Dispose() => _inner.Dispose();

    /// <summary>Makes the next <see cref="GetProblem"/> return a dynamic error with the given XPath code.</summary>
    internal void Raise(string xpathCode) =>
        _problem = new SchematronProblem(
            SchematronDiagnosticCodes.EvaluationFailed,
            "dynamic error " + xpathCode,
            FakeModel.Origin,
            xpathCode
        );
}
