namespace NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Functions;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Xslt.Functions;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// A run over <see cref="MiniQuery"/> with one table of slot values. It records the focus of every evaluation and the calls of the
/// function entry hooks, and calls stylesheet functions through the caller and the table of the fixture.
/// </summary>
internal sealed class MiniRun : IXsltFunctionRun
{
    private readonly Dictionary<int, XdmSequence> _values = [];
    private SchematronProblem _problem;

    internal XsltFunctionTable? Table { get; set; }

    internal XsltFunctionCaller? Caller { get; set; }

    public object? FunctionCalls { get; set; }

    internal List<QueryFocus> Focuses { get; } = [];

    internal List<string> Events { get; } = [];

    internal int Active { get; private set; }

    internal int Count(string eventName)
    {
        var count = 0;
        foreach (var recorded in Events)
        {
            if (string.Equals(recorded, eventName, System.StringComparison.Ordinal))
            {
                count++;
            }
        }

        return count;
    }

    internal int MaxActive { get; private set; }

    public QueryStatus TryEvaluateBoolean(QueryExpression expression, in QueryFocus focus, out bool value)
    {
        value = false;
        Focuses.Add(focus);
        try
        {
            value = MiniQuery.Effective(((MiniExpression)expression).Node.Evaluate(this, in focus));
            return QueryStatus.Ok;
        }
        catch (MiniQuery.Failure failure)
        {
            _problem = failure.Problem;
            return QueryStatus.Problem;
        }
    }

    public QueryStatus TryEvaluate(QueryExpression expression, in QueryFocus focus, ISequenceSink sink)
    {
        Focuses.Add(focus);
        try
        {
            var value = ((MiniExpression)expression).Node.Evaluate(this, in focus);
            for (var i = 0; i < value.Count; i++)
            {
                var item = value[i];
                sink.Item(in item);
            }

            return QueryStatus.Ok;
        }
        catch (MiniQuery.Failure failure)
        {
            _problem = failure.Problem;
            return QueryStatus.Problem;
        }
    }

    public QueryStatus TryBindVariable(int slot, QueryExpression value, in QueryFocus focus)
    {
        Focuses.Add(focus);
        try
        {
            _values[slot] = ((MiniExpression)value).Node.Evaluate(this, in focus);
            Events.Add("bind:" + slot);
            return QueryStatus.Ok;
        }
        catch (MiniQuery.Failure failure)
        {
            _problem = failure.Problem;
            return QueryStatus.Problem;
        }
    }

    public void BindVariable(int slot, string value) => _values[slot] = XdmSequence.Create(XdmItem.FromString(value));

    public SchematronProblem GetProblem() => _problem;

    public void FailWith(in SchematronProblem problem) => _problem = problem;

    public void SetValue(int slot, in XdmSequence value) => _values[slot] = value;

    public bool TryGetValue(int slot, out XdmSequence value) => _values.TryGetValue(slot, out value);

    public void ClearValue(int slot) => _ = _values.Remove(slot);

    public void EnterFunction()
    {
        Active++;
        MaxActive = Math.Max(MaxActive, Active);
        Events.Add("enter");
    }

    public void ExitFunction()
    {
        Active--;
        Events.Add("exit");
    }

    public void Dispose() { }

    internal XdmSequence CallFunction(string uri, string local, XdmSequence[] arguments)
    {
        if (
            Table is null
            || Caller is null
            || !Table.TryGet(new XPathQName(uri, local), arguments.Length, out var function)
        )
        {
            throw MiniQuery.Fail("XPST0017", $"No function {local}#{arguments.Length}.");
        }

        return Caller.TryCall(function, arguments, out var result) == QueryStatus.Ok
            ? result
            : throw new MiniQuery.Failure(Caller.GetProblem());
    }
}
