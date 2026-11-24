namespace NetEvolve.Schemas.XPath.Tests.Unit.Formatting.Dates;

using System.Collections.Generic;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.DateTime;
using NetEvolve.Schemas.XPath.Abstractions;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Functions.Binding;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// Runs <c>format-date</c>, <c>format-dateTime</c>, and <c>format-time</c> through the default catalog, the way a compiled call does.
/// The value is given in its lexical form, or <c>()</c> for the empty sequence; the result is the string, <c>()</c> for the empty
/// sequence, an error is <c>error:CODE</c>.
/// </summary>
internal static class FormatDateTimeHarness
{
    internal const string FunctionNamespace = "http://www.w3.org/2005/xpath-functions";

    internal static bool TryResolve(
        string name,
        int arity,
        XPathVersion version,
        XPathHostRequirements host,
        out XPathFunction? function
    ) =>
        FunctionCatalog.Default.Registry.TryResolve(
            XPathStaticContext.Default.ToBuilder().WithVersion(version).Build(),
            new XPathQName(FunctionNamespace, name),
            arity,
            host,
            out function
        );

    /// <summary>Formats in XPath 3.1 with the two-argument form.</summary>
    internal static string Format(string name, string value, string picture) =>
        Run(name, value, picture, null, XPathVersion.V31, XPathHostRequirements.None);

    /// <summary>Formats in XPath 3.1 with the five-argument form; a <see langword="null"/> argument is the empty sequence.</summary>
    internal static string Format(
        string name,
        string value,
        string picture,
        string? language,
        string? calendar,
        string? place = null
    ) => Run(name, value, picture, [language, calendar, place], XPathVersion.V31, XPathHostRequirements.None);

    /// <summary>Formats with a version and a host flag, in the two-argument form.</summary>
    internal static string Format(
        string name,
        string value,
        string picture,
        XPathVersion version,
        XPathHostRequirements host
    ) => Run(name, value, picture, null, version, host);

    private static string Run(
        string name,
        string value,
        string picture,
        string?[]? five,
        XPathVersion version,
        XPathHostRequirements host
    )
    {
        if (!TryResolve(name, five is null ? 2 : 5, version, host, out var function))
        {
            return "error:not-registered";
        }

        var kind = name switch
        {
            "format-date" => XsdDateTimeKind.Date,
            "format-time" => XsdDateTimeKind.Time,
            _ => XsdDateTimeKind.DateTime,
        };
        var arguments = new List<XdmSequence>
        {
            value == "()"
                ? XdmSequence.Empty
                : XdmSequence.Create(XdmItem.FromDateTime(XsdDateTime.Parse(value, kind, XsdVersion.V10).Value)),
            XdmSequence.Create(XdmItem.FromString(picture)),
        };
        if (five is not null)
        {
            foreach (var text in five)
            {
                arguments.Add(text is null ? XdmSequence.Empty : XdmSequence.Create(XdmItem.FromString(text)));
            }
        }

        using var context = new XPathEvaluationContext();
        try
        {
            var result = function!.Call(context, arguments.ToArray());
            if (result.Count == 0)
            {
                return "()";
            }

            return result.Count == 1 && result[0].TryGetString(out var text) ? text! : "error:not-a-string";
        }
        catch (XPathException failure)
        {
            return "error:" + failure.Diagnostic.Code;
        }
    }
}
