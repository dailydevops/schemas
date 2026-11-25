namespace NetEvolve.Schemas.XPath.Tests.Unit.Formatting.Integers;

using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using NetEvolve.Schemas.XPath.Abstractions;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Functions.Binding;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// Runs <c>format-integer</c> through the default catalog, the way a compiled call does. The value is written as decimal digits of any
/// length, or <c>()</c> for the empty sequence; the result is the string, an error is <c>error:CODE</c>.
/// </summary>
internal static class FormatIntegerHarness
{
    internal const string FunctionNamespace = "http://www.w3.org/2005/xpath-functions";

    internal static bool TryResolve(int arity, out XPathFunction? function, XPathVersion version) =>
        FunctionCatalog.Default.Registry.TryResolve(
            XPathStaticContext.Default.ToBuilder().WithVersion(version).Build(),
            new XPathQName(FunctionNamespace, "format-integer"),
            arity,
            XPathHostRequirements.None,
            out function
        );

    /// <summary>Formats in XPath 3.1; a <see langword="null"/> language is the two-argument form.</summary>
    internal static string Format(string value, string picture, string? language = null)
    {
        var arity = language is null ? 2 : 3;
        if (!TryResolve(arity, out var function, XPathVersion.V31))
        {
            return "error:not-registered";
        }

        var arguments = new List<XdmSequence>
        {
            value == "()"
                ? XdmSequence.Empty
                : XdmSequence.Create(XdmItem.FromBigInteger(BigInteger.Parse(value, CultureInfo.InvariantCulture))),
            XdmSequence.Create(XdmItem.FromString(picture)),
        };
        if (language is not null)
        {
            arguments.Add(XdmSequence.Create(XdmItem.FromString(language)));
        }

        using var context = new XPathEvaluationContext();
        try
        {
            var result = function!.Call(context, arguments.ToArray());
            return result.Count == 1 && result[0].TryGetString(out var text) ? text! : "error:not-a-string";
        }
        catch (XPathException failure)
        {
            return "error:" + failure.Diagnostic.Code;
        }
    }
}
