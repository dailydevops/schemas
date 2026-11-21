namespace NetEvolve.Schemas.Schematron.Bindings.Registry;

using System;
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
internal sealed class XPathQueryRun : IQueryRun
{
    // The public code type, not the internal table of the XPath package, which the seam rule keeps out of reach.
    private static readonly XPathErrorCode UnsetContext = new("XPDY0002");

    private readonly XPathEvaluationContext _context = new();
    private readonly CancellationToken _cancellationToken;
    private XdmSequence[] _values = new XdmSequence[16];
    private bool[] _bound = new bool[16];
    private SchematronProblem _problem;

    internal XPathQueryRun(QueryResources resources, CancellationToken cancellationToken)
    {
        Argument.ThrowIfNull(resources);

        _cancellationToken = cancellationToken;
        _context.ResourceResolver = resources.XPathResolver;
        _context.Host.DecimalFormats = NoDecimalFormats.Instance;
    }

    public QueryStatus TryEvaluateBoolean(QueryExpression expression, in QueryFocus focus, out bool value)
    {
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
        Debug.Assert(value is not null, "The value of a parameter is a string.");

        Store(slot, XdmSequence.Create(XdmItem.FromString(value!)));
    }

    public SchematronProblem GetProblem() => _problem;

    public void Dispose()
    {
        // The values hold items of the instance; nothing of a run stays reachable from the pooled context.
        Array.Clear(_values, 0, _values.Length);
        Array.Clear(_bound, 0, _bound.Length);
        _context.Reset();
        _context.Dispose();
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

        SetFocus(in focus);
        var slots = query.ReferencedSlots;
        for (var i = 0; i < query.Names.Length; i++)
        {
            var slot = slots[i];
            if (slot < _bound.Length && _bound[slot])
            {
                _context.Variables.Set(query.Names[i], _values[slot]);
            }
            else
            {
                _context.Variables.Remove(query.Names[i]);
            }
        }

        var result = query.Expression.TryEvaluate(_context);
        sequence = result.Value;
        if (result.Status == XPathEvaluationStatus.Ok)
        {
            return true;
        }

        _ = Fail(query, result.Diagnostic);
        return false;
    }

    private void SetFocus(in QueryFocus focus)
    {
        if (focus.Item.Kind == XdmItemKind.None)
        {
            // The focus type of the context is internal to the XPath package and outside the seam, so the public reset clears the focus; the
            // variables are set after it, and the slot of the decimal formats is set again.
            _context.Reset();
            _context.Host.DecimalFormats = NoDecimalFormats.Instance;
            return;
        }

        var item = focus.Item;
        var position = Math.Max(focus.Position, 1);
        _context.SetContextItem(in item, position, Math.Max(focus.ResolveSize(), position));
        _context.Host.CurrentItem = item;
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
