namespace NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Instructions.Regex;

using System.Collections.Generic;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Xslt.Instructions.Regex;
using NetEvolve.Schemas.XPath.Abstractions;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The run of the regex tests. It holds the regex groups as a real run does for the slot of the XPath host (the instruction writes
/// them through <see cref="IXsltRegexRun"/>) and evaluates <c>regex-group()</c> with the host function of the package over that slot.
/// </summary>
internal sealed class RegexRun : IXsltRegexRun
{
    private readonly Dictionary<int, XdmSequence> _values = [];
    private SchematronProblem _problem;
    private XPathRegexGroups? _groups;

    /// <summary>Gets what the instruction wrote into the slot, one entry per write: the whole match, or <c>-</c> for absent.</summary>
    internal List<string> Writes { get; } = [];

    public XPathRegexGroups? RegexGroups
    {
        get => _groups;
        set
        {
            _groups = value;
            Writes.Add(value is { } g ? g.GetGroup(0).ToString() : "-");
        }
    }

    public QueryStatus TryEvaluateBoolean(QueryExpression expression, in QueryFocus focus, out bool value)
    {
        value = false;
        return QueryStatus.Problem;
    }

    public QueryStatus TryEvaluate(QueryExpression expression, in QueryFocus focus, ISequenceSink sink)
    {
        try
        {
            var value = ((RegexBinding.RegexExpression)expression).Node.Evaluate(this, in focus);
            for (var i = 0; i < value.Count; i++)
            {
                var item = value[i];
                sink.Item(in item);
            }

            return QueryStatus.Ok;
        }
        catch (RegexBinding.EvaluationException failure)
        {
            _problem = failure.Problem;
            return QueryStatus.Problem;
        }
    }

    public QueryStatus TryBindVariable(int slot, QueryExpression value, in QueryFocus focus) => QueryStatus.Problem;

    public void BindVariable(int slot, string value) => _values[slot] = XdmSequence.Create(XdmItem.FromString(value));

    public SchematronProblem GetProblem() => _problem;

    public void Dispose() { }

    internal void SetValue(int slot, in XdmSequence value) => _values[slot] = value;

    internal bool TryGetValue(int slot, out XdmSequence value) => _values.TryGetValue(slot, out value);

    /// <summary>Calls <c>regex-group()</c> with the slot of this run, as the XPath run of #628 will.</summary>
    internal XdmSequence CallRegexGroup(XdmSequence argument)
    {
        using var context = new XPathEvaluationContext();
        context.Host.RegexGroups = _groups;
        return new RegexGroupFunction().Call(context, [argument]);
    }
}
