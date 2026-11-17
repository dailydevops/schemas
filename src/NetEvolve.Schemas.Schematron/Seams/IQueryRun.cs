namespace NetEvolve.Schemas.Schematron.Seams;

using System;

/// <summary>One run of a binding: wraps one pooled evaluation context, one per validation call, not thread-safe.</summary>
internal interface IQueryRun : IDisposable
{
    QueryStatus TryEvaluateBoolean(QueryExpression expression, in QueryFocus focus, out bool value);

    QueryStatus TryEvaluate(QueryExpression expression, in QueryFocus focus, ISequenceSink sink);

    QueryStatus TryBindVariable(int slot, QueryExpression value, in QueryFocus focus);

    /// <summary>Binds an external parameter; the caller value is always a literal string.</summary>
    void BindVariable(int slot, string value);

    /// <summary>Gets the problem; valid after the last call returned <see cref="QueryStatus.Problem"/>.</summary>
    SchematronProblem GetProblem();
}
