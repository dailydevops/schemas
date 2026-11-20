namespace NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Instructions.Grouping;

using System;
using System.Collections.Generic;
using System.Globalization;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Xslt.Instructions.Grouping;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The run of the grouping tests. It holds the current group and the grouping key as a real run does for the slots of the XPath host
/// (the instruction writes them through <see cref="IXsltGroupRun"/>), evaluates <c>current-group()</c> and
/// <c>current-grouping-key()</c> with the host functions of the package over those slots, and records every evaluation.
/// </summary>
internal sealed class GroupRun(GroupBinding owner) : IXsltGroupRun
{
    private readonly Dictionary<int, XdmSequence> _values = [];
    private SchematronProblem _problem;
    private XdmSequence? _group;
    private XdmSequence? _key;

    /// <summary>Gets the texts of the evaluations with the position and the size of their focus, as <c>text@position/size</c>.</summary>
    internal List<string> Evaluations { get; } = [];

    /// <summary>Gets what the instruction wrote into the slots, one entry per write, as <c>group:count</c> or <c>key:text</c> (<c>-</c> for absent).</summary>
    internal List<string> Writes { get; } = [];

    public XdmSequence? CurrentGroup
    {
        get => _group;
        set
        {
            _group = value;
            Writes.Add("group:" + (value is { } g ? g.Count.ToString(CultureInfo.InvariantCulture) : "-"));
        }
    }

    public XdmSequence? CurrentGroupingKey
    {
        get => _key;
        set
        {
            _key = value;
            Writes.Add("key:" + (value is { } k ? (k.Count == 0 ? "empty" : GroupBinding.Show(k[0])) : "-"));
        }
    }

    public QueryStatus TryEvaluateBoolean(QueryExpression expression, in QueryFocus focus, out bool value)
    {
        value = false;
        try
        {
            var result = Evaluate(expression, in focus);
            value = result.Count > 0 && Effective(result[0]);
            return QueryStatus.Ok;
        }
        catch (GroupBinding.EvaluationException failure)
        {
            _problem = failure.Problem;
            return QueryStatus.Problem;
        }
    }

    public QueryStatus TryEvaluate(QueryExpression expression, in QueryFocus focus, ISequenceSink sink)
    {
        try
        {
            var value = Evaluate(expression, in focus);
            for (var i = 0; i < value.Count; i++)
            {
                var item = value[i];
                sink.Item(in item);
            }

            return QueryStatus.Ok;
        }
        catch (GroupBinding.EvaluationException failure)
        {
            _problem = failure.Problem;
            return QueryStatus.Problem;
        }
    }

    public QueryStatus TryBindVariable(int slot, QueryExpression value, in QueryFocus focus)
    {
        try
        {
            _values[slot] = Evaluate(value, in focus);
            return QueryStatus.Ok;
        }
        catch (GroupBinding.EvaluationException failure)
        {
            _problem = failure.Problem;
            return QueryStatus.Problem;
        }
    }

    public void BindVariable(int slot, string value) => _values[slot] = XdmSequence.Create(XdmItem.FromString(value));

    public SchematronProblem GetProblem() => _problem;

    public void Dispose() { }

    internal void SetValue(int slot, in XdmSequence value) => _values[slot] = value;

    internal bool TryGetValue(int slot, out XdmSequence value) => _values.TryGetValue(slot, out value);

    /// <summary>Calls the host function of the binding with the slots of this run, as the XPath run of #628 will.</summary>
    internal XdmSequence CallHost(string name)
    {
        var function = Array.Find(owner.Functions, f => string.Equals(f.Name.LocalName, name, StringComparison.Ordinal))
            ?? throw GroupBinding.Failure("XPST0017", $"No function {name} under the binding '{owner.Name}'.");

        using var context = new XPathEvaluationContext();
        context.Host.CurrentGroup = _group;
        context.Host.CurrentGroupingKey = _key;
        var call = new XPathFunctionCall(context, default);
        return function.Invoke(ref call) == XPathEvaluationStatus.Error
            ? throw GroupBinding.Failure(call.Diagnostic.Code.ToString(), call.Diagnostic.Message)
            : call.Result;
    }

    private XdmSequence Evaluate(QueryExpression expression, in QueryFocus focus)
    {
        var typed = (GroupBinding.GroupExpression)expression;
        Evaluations.Add($"{typed.Text}@{focus.Position}/{focus.ResolveSize()}");
        return typed.Node.Evaluate(this, in focus);
    }

    private static bool Effective(in XdmItem item)
    {
        if (item.TryGetBoolean(out var boolean) && item.Kind == XdmItemKind.Boolean)
        {
            return boolean;
        }

        if (item.TryGetInteger(out var integer))
        {
            return integer != 0;
        }

        return item.IsNode || !string.IsNullOrEmpty(GroupBinding.Show(in item));
    }
}
