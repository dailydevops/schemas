namespace NetEvolve.Schemas.XPath.Tests.Unit.Functions.DateTime;

using System;
using System.Globalization;
using System.Text;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.DateTime;
using NetEvolve.Schemas.Xml.Datatypes.Duration;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Functions.Binding;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// Runs the built-in date and time functions through the default catalog, the way a compiled call does: resolve by expanded name
/// and arity, then <see cref="XPathFunction.Call"/>. An argument is written <c>type:lexical</c>, and <c>()</c> is the empty sequence;
/// a result is shown in its canonical lexical form, an error as <c>error:CODE</c>.
/// </summary>
internal static class DateTimeHarness
{
    internal const string FunctionNamespace = "http://www.w3.org/2005/xpath-functions";

    /// <summary>Resolves a built-in function of the default catalog.</summary>
    internal static bool TryResolve(string local, int arity, out XPathFunction? function) =>
        FunctionCatalog.Default.Registry.TryResolve(
            XPathStaticContext.Default,
            new XPathQName(FunctionNamespace, local),
            arity,
            XPathHostRequirements.None,
            out function
        );

    /// <summary>Calls a function with the given arguments in a fresh evaluation context.</summary>
    internal static string Run(string local, params string[] arguments) => RunWith(null, local, arguments);

    /// <summary>Calls a function with the given arguments after <paramref name="setup"/> configured the evaluation context.</summary>
    internal static string RunWith(Action<XPathEvaluationContext>? setup, string local, params string[] arguments)
    {
        using var context = new XPathEvaluationContext();
        setup?.Invoke(context);
        return RunIn(context, local, arguments);
    }

    /// <summary>Calls a function in an existing evaluation context.</summary>
    internal static string RunIn(XPathEvaluationContext context, string local, params string[] arguments)
    {
        if (!TryResolve(local, arguments.Length, out var function))
        {
            return "error:not-registered";
        }

        var sequences = new XdmSequence[arguments.Length];
        for (var i = 0; i < sequences.Length; i++)
        {
            sequences[i] = Parse(arguments[i]);
        }

        try
        {
            return Show(function!.Call(context, sequences));
        }
        catch (XPathException failure)
        {
            return "error:" + failure.Diagnostic.Code;
        }
    }

    /// <summary>Parses <c>type:lexical</c> into a sequence of one item; <c>()</c> is the empty sequence.</summary>
    internal static XdmSequence Parse(string argument)
    {
        if (string.Equals(argument, "()", StringComparison.Ordinal))
        {
            return XdmSequence.Empty;
        }

        var colon = argument.IndexOf(':', StringComparison.Ordinal);
        var type = argument.Substring(0, colon);
        var lexical = argument.Substring(colon + 1);
        return XdmSequence.Create(
            type switch
            {
                "dateTime" => Temporal(lexical, XsdDateTimeKind.DateTime),
                "date" => Temporal(lexical, XsdDateTimeKind.Date),
                "time" => Temporal(lexical, XsdDateTimeKind.Time),
                "duration" => Span(lexical, DurationKind.Duration),
                "yearMonthDuration" => Span(lexical, DurationKind.YearMonth),
                "dayTimeDuration" => Span(lexical, DurationKind.DayTime),
                "string" => XdmItem.FromString(lexical),
                "untypedAtomic" => XdmItem.FromUntypedAtomic(lexical),
                "integer" => XdmItem.FromInteger(long.Parse(lexical, CultureInfo.InvariantCulture)),
                _ => throw new ArgumentException("Unknown argument type " + type, nameof(argument)),
            }
        );
    }

    /// <summary>Shows a result sequence: the canonical lexical form of each item, separated by a comma.</summary>
    internal static string Show(XdmSequence sequence)
    {
        var text = new StringBuilder();
        for (var i = 0; i < sequence.Count; i++)
        {
            if (i > 0)
            {
                _ = text.Append(',');
            }

            _ = text.Append(Show(sequence[i]));
        }

        return text.ToString();
    }

    private static string Show(XdmItem item)
    {
        if (item.TryGetInteger(out var integer))
        {
            return integer.ToString(CultureInfo.InvariantCulture);
        }

        if (item.TryGetDecimal(out var number))
        {
            return number.ToString();
        }

        if (item.TryGetDateTime(out var temporal))
        {
            return temporal.ToString();
        }

        if (item.TryGetDuration(out var duration))
        {
            return duration.ToString(
                item.Kind == XdmItemKind.YearMonthDuration ? DurationKind.YearMonth : DurationKind.DayTime
            );
        }

        return item.Kind.ToString();
    }

    private static XdmItem Temporal(string lexical, XsdDateTimeKind kind) =>
        XdmItem.FromDateTime(XsdDateTime.Parse(lexical, kind, XsdVersion.V10).Value);

    private static XdmItem Span(string lexical, DurationKind kind) =>
        XdmItem.FromDuration(XsdDuration.Parse(lexical, kind).Value, kind);
}
