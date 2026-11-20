namespace NetEvolve.Schemas.XPath.Tests.Unit.Formatting.Numbers;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.XPath.Abstractions;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Functions.Binding;
using NetEvolve.Schemas.XPath.Tests.Unit.Functions.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// Runs <c>format-number</c> through the default catalog, the way a compiled call does: resolve by expanded name, language version, and
/// host flag, then <see cref="XPathFunction.Call"/>. The XSLT 2.0 rules are selected by version 2.0 with the XSLT 2.0 host flag, the 3.1
/// rules by version 3.1. A value is written <c>kind:payload</c> as in the node harness; a result is the string, an error is <c>error:CODE</c>.
/// </summary>
internal static class FormatNumberHarness
{
    internal const string FunctionNamespace = "http://www.w3.org/2005/xpath-functions";

    /// <summary>Resolves <c>format-number</c> for a language version and the host flags that are set.</summary>
    internal static bool TryResolve(
        int arity,
        out XPathFunction? function,
        XPathVersion version,
        XPathHostRequirements host = XPathHostRequirements.None
    ) =>
        FunctionCatalog.Default.Registry.TryResolve(
            XPathStaticContext.Default.ToBuilder().WithVersion(version).Build(),
            new XPathQName(FunctionNamespace, "format-number"),
            arity,
            host,
            out function
        );

    /// <summary>Formats under the XSLT 2.0 rules: version 2.0 and the XSLT 2.0 host flag.</summary>
    internal static string Xslt20(
        string value,
        string picture,
        string? name = null,
        IXPathDecimalFormatProvider? formats = null
    ) => Run(XPathVersion.V20, XPathHostRequirements.Xslt20Host, value, picture, name, formats);

    /// <summary>Formats under the 3.1 rules: version 3.1, no host flag.</summary>
    internal static string Xpath31(
        string value,
        string picture,
        string? name = null,
        IXPathDecimalFormatProvider? formats = null
    ) => Run(XPathVersion.V31, XPathHostRequirements.None, value, picture, name, formats);

    /// <summary>Formats under a language version and host flag; a <see langword="null"/> name is the two-argument form.</summary>
    internal static string Run(
        XPathVersion version,
        XPathHostRequirements host,
        string value,
        string picture,
        string? name,
        IXPathDecimalFormatProvider? formats
    )
    {
        var arity = name is null ? 2 : 3;
        if (!TryResolve(arity, out var function, version, host))
        {
            return "error:not-registered";
        }

        var arguments = new List<XdmSequence>
        {
            NodesHarness.Parse(value),
            XdmSequence.Create(XdmItem.FromString(picture)),
        };
        if (name is not null)
        {
            arguments.Add(XdmSequence.Create(XdmItem.FromString(name)));
        }

        using var context = new XPathEvaluationContext();
        context.Host.DecimalFormats = formats;
        return Call(function!, context, arguments.ToArray());
    }

    /// <summary>Calls a function and shows the string result or the error code.</summary>
    internal static string Call(XPathFunction function, XPathEvaluationContext context, XdmSequence[] arguments)
    {
        try
        {
            var result = function.Call(context, arguments);
            return result.Count == 1 && result[0].TryGetString(out var text) ? text! : "error:not-a-string";
        }
        catch (XPathException failure)
        {
            return "error:" + failure.Diagnostic.Code;
        }
    }

    /// <summary>A provider over a fixed set of formats; the unnamed format has the default name.</summary>
    internal sealed class Formats : IXPathDecimalFormatProvider
    {
        private readonly Dictionary<XPathQName, XPathDecimalFormat> _formats = [];

        /// <summary>Adds a format under a name in no namespace; the empty name is the unnamed format.</summary>
        internal Formats Add(string name, XPathDecimalFormat format) =>
            Add(name.Length == 0 ? default : new XPathQName(string.Empty, name), format);

        /// <summary>Adds a format under an expanded name.</summary>
        internal Formats Add(XPathQName name, XPathDecimalFormat format)
        {
            _formats[name] = format;
            return this;
        }

        /// <inheritdoc />
        public bool TryGetDecimalFormat(XPathQName name, out XPathDecimalFormat? format) =>
            _formats.TryGetValue(name, out format);
    }
}
