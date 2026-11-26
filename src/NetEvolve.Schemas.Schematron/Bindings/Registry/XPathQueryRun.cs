namespace NetEvolve.Schemas.Schematron.Bindings.Registry;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.XPath.Abstractions;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Evaluation.Atomization;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// One run of <see cref="XPathQueryBinding"/>: one pooled <see cref="XPathEvaluationContext"/>, the values of the variable slots, and
/// the problem of the last failed call. One run serves one validation call and is not thread-safe.
/// </summary>
/// <remarks>
/// <para>
/// The values live in an array indexed by slot. Before an evaluation the run copies only the slots the expression reads into the
/// variables of the context, under the names the expression was compiled with, so two slots with one name never see each other: the
/// expression reads the slot that was visible where it was written. A slot that is not bound is removed from the variables, so the
/// evaluation fails with XPDY0002 instead of reading the value of another slot.
/// </para>
/// <para>
/// The size of the focus is -1 when nothing needs it; the context needs a size of at least the position, so an unknown size is taken
/// as the position (<c>last()</c> then equals <c>position()</c>). The token is looked at before every evaluation.
/// </para>
/// </remarks>
internal sealed class XPathQueryRun : IXsltFunctionRun
{
    // The public code type, not the internal table of the XPath package, which the seam rule keeps out of reach.
    private static readonly XPathErrorCode UnsetContext = new("XPDY0002");

    private readonly List<XPathEvaluationContext> _contexts = [];
    private readonly IXPathResourceResolver _resolver;
    private readonly CancellationToken _cancellationToken;
    private int _level;
    private bool _nested;
    private object? _functions;
    private IXPathKeyProvider? _keys;
    private XdmSequence[] _values = new XdmSequence[16];
    private bool[] _bound = new bool[16];
    private SchematronProblem _problem;

    internal XPathQueryRun(QueryResources resources, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(resources);

        _cancellationToken = cancellationToken;
        _resolver = resources.XPathResolver;
    }

    /// <inheritdoc />
    public object? FunctionCalls
    {
        get => _functions;
        set
        {
            _functions = value;
            foreach (var context in _contexts)
            {
                context.Host.HostState = value;
            }
        }
    }

    /// <inheritdoc />
    public IXPathKeyProvider? Keys
    {
        get => _keys;
        set
        {
            _keys = value;
            foreach (var context in _contexts)
            {
                context.Host.Keys = value;
            }
        }
    }

    public QueryStatus TryEvaluateBoolean(QueryExpression expression, in QueryFocus focus, out bool value)
    {
        ArgumentNullException.ThrowIfNull(expression);

        var query = Cast(expression);
        value = false;
        if (!TryRun(query, in focus, out var sequence))
        {
            return QueryStatus.Problem;
        }

        try
        {
            value = XPathAtomization.EffectiveBooleanValue(in sequence);
            return QueryStatus.Ok;
        }
        catch (XPathException failure)
        {
            return Fail(query, failure.Diagnostic);
        }
    }

    public QueryStatus TryEvaluate(QueryExpression expression, in QueryFocus focus, ISequenceSink sink)
    {
        ArgumentNullException.ThrowIfNull(expression);
        ArgumentNullException.ThrowIfNull(sink);

        var query = Cast(expression);
        if (!TryRun(query, in focus, out var sequence))
        {
            return QueryStatus.Problem;
        }

        foreach (var item in sequence)
        {
            sink.Item(in item);
        }

        return QueryStatus.Ok;
    }

    public QueryStatus TryBindVariable(int slot, QueryExpression value, in QueryFocus focus)
    {
        ArgumentNullException.ThrowIfNull(value);

        var query = Cast(value);
        if (!TryRun(query, in focus, out var sequence))
        {
            return QueryStatus.Problem;
        }

        if (query.DeclaredType is { } type && !Satisfies(in sequence, type))
        {
            _problem = new SchematronProblem(
                SchematronDiagnosticCodes.LetType,
                $"The value of the variable does not match the declared type '{type}'.",
                query.Location,
                "XPTY0004"
            );
            return QueryStatus.Problem;
        }

        Store(slot, sequence);
        return QueryStatus.Ok;
    }

    public void BindVariable(int slot, string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        Store(slot, XdmSequence.Create(XdmItem.FromString(value)));
    }

    public void SetValue(int slot, in XdmSequence value) => Store(slot, value);

    public bool TryGetValue(int slot, out XdmSequence value)
    {
        if (slot < _bound.Length && _bound[slot])
        {
            value = _values[slot];
            return true;
        }

        value = XdmSequence.Empty;
        return false;
    }

    public void ClearValue(int slot)
    {
        if (slot < _bound.Length)
        {
            _bound[slot] = false;
            _values[slot] = default;
        }
    }

    // A function body starts with no focus, no current item, no group, no grouping key, and no regex groups: an evaluation that
    // is nested in another one runs on a context of its own (see Acquire), which is cleared when it is taken, so there is nothing
    // to save here and the evaluation that called the function gets its own context back unchanged.
    public void EnterFunction() { }

    public void ExitFunction() { }

    public void FailWith(in SchematronProblem problem)
    {
        _problem = problem;
        _nested = true;
    }

    public SchematronProblem GetProblem() => _problem;

    public void Dispose()
    {
        // The values hold items of the instance; nothing of a run stays reachable from the pooled contexts.
        Array.Clear(_values, 0, _values.Length);
        Array.Clear(_bound, 0, _bound.Length);
        foreach (var context in _contexts)
        {
            context.Reset();
            context.Dispose();
        }
    }

    private static XPathQueryExpression Cast(QueryExpression expression)
    {
        Debug.Assert(expression is XPathQueryExpression, "The expression was compiled by another binding.");

        return (XPathQueryExpression)expression;
    }

    // The function conversion rules for the supported types: one item, of the type or promoted to it. The layering rule keeps the
    // constant checker of the namespace Scopes out of reach of a binding, so the table is repeated here.
    private static bool Satisfies(in XdmSequence sequence, string type)
    {
        if (sequence.Count != 1)
        {
            return false;
        }

        var kind = sequence[0].Kind;
        return type switch
        {
            "xs:boolean" => kind == XdmItemKind.Boolean,
            "xs:integer" => kind == XdmItemKind.XsInteger,
            "xs:double" => kind
                is XdmItemKind.XsDouble
                    or XdmItemKind.XsInteger
                    or XdmItemKind.XsDecimal
                    or XdmItemKind.XsFloat,
            "xs:string" => kind is XdmItemKind.XsString or XdmItemKind.AnyUri,
            _ => false,
        };
    }

    private bool TryRun(XPathQueryExpression query, in QueryFocus focus, out XdmSequence sequence)
    {
        _cancellationToken.ThrowIfCancellationRequested();

        _nested = false;
        var context = Acquire();
        SetFocus(context, in focus);
        var slots = query.ReferencedSlots;
        for (var i = 0; i < query.Names.Length; i++)
        {
            var slot = slots[i];
            if (slot < _bound.Length && _bound[slot])
            {
                context.Variables.Set(query.Names[i], _values[slot]);
            }
            else
            {
                context.Variables.Remove(query.Names[i]);
            }
        }

        // A stylesheet function called from the expression evaluates its body through this run, so the context of the evaluation
        // that is running must stay untouched: the nested one takes the next context.
        _level++;
        XPathResult result;
        try
        {
            result = query.Expression.TryEvaluate(context);
        }
        finally
        {
            _level--;
        }

        sequence = result.Value;
        if (result.Status == XPathEvaluationStatus.Ok)
        {
            return true;
        }

        _ = Fail(query, result.Diagnostic);
        return false;
    }

    // One context per level of nested evaluations. A context of a nested level is cleared when it is taken, because it may hold the
    // focus and the host slots of an earlier evaluation at that level.
    private XPathEvaluationContext Acquire()
    {
        if (_level < _contexts.Count)
        {
            var existing = _contexts[_level];
            if (_level > 0)
            {
                existing.Reset();
                Prepare(existing);
            }

            return existing;
        }

        var context = new XPathEvaluationContext { ResourceResolver = _resolver };
        Prepare(context);
        _contexts.Add(context);
        return context;
    }

    // The host slots that every evaluation of the run starts with; the public reset clears them, so they are set again after it.
    private void Prepare(XPathEvaluationContext context)
    {
        context.Host.DecimalFormats = NoDecimalFormats.Instance;
        context.Host.HostState = _functions;
        context.Host.Keys = _keys;
    }

    private void SetFocus(XPathEvaluationContext context, in QueryFocus focus)
    {
        if (focus.Item.Kind == XdmItemKind.None)
        {
            // The focus type of the context is internal to the XPath package and outside the seam, so the public reset clears the
            // focus; the variables are set after it, and the host slots are set again.
            context.Reset();
            Prepare(context);
            return;
        }

        var item = focus.Item;
        var position = Math.Max(focus.Position, 1);
        context.SetContextItem(in item, position, Math.Max(focus.ResolveSize(), position));
        context.Host.CurrentItem = item;
    }

    private void Store(int slot, XdmSequence value)
    {
        if (slot >= _values.Length)
        {
            var length = Math.Max(slot + 1, _values.Length * 2);
            Array.Resize(ref _values, length);
            Array.Resize(ref _bound, length);
        }

        _values[slot] = value;
        _bound[slot] = true;
    }

    // An unbound variable and an absent context item are the same dynamic error, XPDY0002.
    private QueryStatus Fail(XPathQueryExpression query, in XPathDiagnostic diagnostic)
    {
        if (_nested)
        {
            // A stylesheet function failed inside the expression: the problem of the callee, with its own code and location, stays.
            _nested = false;
            return QueryStatus.Problem;
        }

        _problem = new SchematronProblem(
            diagnostic.QName == UnsetContext
                ? SchematronDiagnosticCodes.VariableUndefined
                : SchematronDiagnosticCodes.EvaluationFailed,
            diagnostic.Message,
            query.Location,
            diagnostic.QName.LocalName
        );
        return QueryStatus.Problem;
    }

    // The slot of the decimal formats of a run: there is no format yet, so the default format applies and a named one is unknown.
    private sealed class NoDecimalFormats : IXPathDecimalFormatProvider
    {
        internal static NoDecimalFormats Instance { get; } = new();

        public bool TryGetDecimalFormat(XPathQName name, out XPathDecimalFormat? format)
        {
            format = null;
            return false;
        }
    }
}
