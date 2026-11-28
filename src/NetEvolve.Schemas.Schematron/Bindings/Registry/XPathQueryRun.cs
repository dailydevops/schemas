namespace NetEvolve.Schemas.Schematron.Bindings.Registry;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Resources;
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
/// The values live in an array indexed by slot, which every context of the run reads directly: the expression was compiled with the
/// slot of each variable (<see cref="XPathQueryBinding.CreateStaticContext"/>), so two slots with one name never see each other and
/// the expression reads the slot that was visible where it was written. A slot that is not bound fails the evaluation with XPDY0002
/// instead of reading the value of another slot.
/// </para>
/// <para>
/// The size of the focus is -1 when nothing needs it; the context needs a size of at least the position, so an unknown size is taken
/// as the position (<c>last()</c> then equals <c>position()</c>). The token is looked at before every evaluation.
/// </para>
/// </remarks>
internal sealed class XPathQueryRun
    : IParameterRun,
        IXsltFunctionRun,
        IXsltGroupRun,
        IXsltRegexRun,
        IXPathResourceResolver
{
    // The public code type, not the internal table of the XPath package, which the seam rule keeps out of reach.
    private static readonly XPathErrorCode UnsetContext = new("XPDY0002");

    private readonly List<XPathEvaluationContext> _contexts = [];
    private readonly QueryResources _resources;
    private readonly CancellationToken _cancellationToken;
    private int _level;
    private bool _nested;
    private object? _functions;
    private IXPathKeyProvider? _keys;
    private XdmSequence[] _values = new XdmSequence[16];
    private bool[] _bound = new bool[16];
    private readonly Stack<(XdmSequence? Group, XdmSequence? Key, XPathRegexGroups? Regex)> _saved = new();
    private SchematronProblem _problem;
    private RunLoading? _loading;

    internal XPathQueryRun(QueryResources resources, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(resources);

        _cancellationToken = cancellationToken;
        _resources = resources;
    }

    /// <inheritdoc />
    public XdmSequence? CurrentGroup { get; set; }

    /// <inheritdoc />
    public XdmSequence? CurrentGroupingKey { get; set; }

    /// <inheritdoc />
    public XPathRegexGroups? RegexGroups { get; set; }

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
        var context = Begin(in focus);

        // The effective boolean value is computed by the engine, which stops a path test at its first node.
        _level++;
        XPathEvaluationStatus status;
        XPathDiagnostic diagnostic;
        try
        {
            status = query.Expression.TryEvaluateBoolean(context, out value, out diagnostic);
        }
        finally
        {
            _level--;
        }

        return status == XPathEvaluationStatus.Ok ? QueryStatus.Ok : Fail(query, in diagnostic);
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

    public QueryStatus TryBindParameter(int slot, string value, string type, in DocumentLocation location)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(type);

        if (!TryConvert(value, type, out var item))
        {
            _problem = new SchematronProblem(
                SchematronDiagnosticCodes.LetType,
                $"The value '{value}' of the parameter cannot be converted to the declared type '{type}'.",
                location,
                "XTTE0590"
            );
            return QueryStatus.Problem;
        }

        Store(slot, XdmSequence.Create(item));
        return QueryStatus.Ok;
    }

    // The lexical forms of an untyped atomic value after whitespace collapse (XPath 2.0 casting).
    // ponytail: an xs:integer beyond the range of long does not convert; add a big-integer parse when a caller needs it.
    private static bool TryConvert(string value, string type, out XdmItem item)
    {
        var text = value.Trim(' ', '\t', '\r', '\n');
        item = default;
        switch (type)
        {
            case "xs:string":
                item = XdmItem.FromString(value);
                return true;
            case "xs:boolean":
                if (text is "true" or "1" or "false" or "0")
                {
                    item = XdmItem.FromBoolean(text is "true" or "1");
                    return true;
                }

                return false;
            case "xs:integer":
                if (long.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var integer))
                {
                    item = XdmItem.FromInteger(integer);
                    return true;
                }

                return false;
            case "xs:double":
                return TryParseDouble(text, out item);
            default:
                return false;
        }
    }

    private static bool TryParseDouble(string text, out XdmItem item)
    {
        item = default;
        double number;
        switch (text)
        {
            case "INF" or "+INF":
                number = double.PositiveInfinity;
                break;
            case "-INF":
                number = double.NegativeInfinity;
                break;
            case "NaN":
                number = double.NaN;
                break;
            default:
                // The invariant parser also takes words such as "Infinity"; the lexical form has digits only.
                if (
                    text.Length == 0
                    || text.AsSpan().IndexOfAny("INaityfn".AsSpan()) >= 0
                    || !double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out number)
                )
                {
                    return false;
                }

                break;
        }

        item = XdmItem.FromDouble(number);
        return true;
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
    // is nested in another one runs on a context of its own (see Acquire), which is cleared when it is taken, so the focus needs no
    // save here. The group, the key, and the regex groups live in the run, not in a context, so they are saved and cleared.
    public void EnterFunction()
    {
        _saved.Push((CurrentGroup, CurrentGroupingKey, RegexGroups));
        CurrentGroup = null;
        CurrentGroupingKey = null;
        RegexGroups = null;
    }

    public void ExitFunction() => (CurrentGroup, CurrentGroupingKey, RegexGroups) = _saved.Pop();

    public void FailWith(in SchematronProblem problem)
    {
        _problem = problem;
        _nested = true;
    }

    public SchematronProblem GetProblem() => _problem;

    // The resolver of the contexts of this run: the document functions load through the core loader, so the limits of the validator
    // count for the whole run (MaxExternalResources) and for every resource (MaxResourceSize), and a denied or missing resource is
    // the failure of the call. A breached limit is remembered, so the failed call reports the limit and not the generic load error.
    ResourceResult IResourceResolver.Resolve(in ResourceRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var loading = _loading ??= new RunLoading(new ResourceLoader(_resources.Resolver, _resources.Limits));
        var loaded = loading.Loader.Load(
            request.Kind,
            request.Reference,
            request.BaseUri,
            _cancellationToken,
            request.PublicId,
            request.NamespaceUri
        );
        switch (loaded.Status)
        {
            case ResourceLoadStatus.Loaded:
                return ResourceResult.Found(loaded.Content, loaded.SystemId);
            case ResourceLoadStatus.LimitExceeded:
                loading.Breach = loaded;
                return ResourceResult.Denied(loaded.Message);
            default:
                return loaded.Code == CoreDiagnosticCodes.ResourceNotFound
                    ? ResourceResult.NotFound(loaded.Message)
                    : ResourceResult.Denied(loaded.Message);
        }
    }

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
        var context = Begin(in focus);

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

    // Prepares the context of the next evaluation: the focus; the variables are the slots of the run.
    private XPathEvaluationContext Begin(in QueryFocus focus)
    {
        _cancellationToken.ThrowIfCancellationRequested();

        _nested = false;
        if (_level == 0 && _loading is { } loading)
        {
            // A breach belongs to the call that met it: a limit hit by an earlier call, which did not fail (doc-available), is no
            // reason of a later failure.
            loading.Breach = null;
        }

        var context = Acquire();
        SetFocus(context, in focus);
        context.Host.CurrentGroup = CurrentGroup;
        context.Host.CurrentGroupingKey = CurrentGroupingKey;
        context.Host.RegexGroups = RegexGroups;
        return context;
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

        var context = new XPathEvaluationContext { ResourceResolver = this };
        context.Variables.AttachSlots(_values, _bound);
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
            foreach (var context in _contexts)
            {
                context.Variables.AttachSlots(_values, _bound);
            }
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

        if (_loading?.Breach is { } breach && IsResourceError(diagnostic.QName.LocalName))
        {
            _loading.Breach = null;
            _problem = new SchematronProblem(breach.Code, breach.Message, query.Location, diagnostic.QName.LocalName);
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

    // The codes that a failed load of the document functions ends in: fn:doc, the text functions, and document().
    private static bool IsResourceError(string code) => code is "FODC0002" or "FODC0005" or "FOUT1170" or "XTRE1160";

    // The loader of a run and the first breached limit, created together on the first load so that a run without a load carries one reference.
    private sealed class RunLoading(ResourceLoader loader)
    {
        internal ResourceLoader Loader { get; } = loader;

        internal ResourceLoadResult? Breach { get; set; }
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
