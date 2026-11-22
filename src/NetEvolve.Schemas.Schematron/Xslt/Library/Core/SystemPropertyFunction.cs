namespace NetEvolve.Schemas.Schematron.Xslt.Library.Core;

using System;
using NetEvolve.Arguments;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Functions.Binding;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// <c>system-property()</c> (XSLT 2.0, 16.6.5): the value of a property of the XSLT namespace, the zero-length string for every other
/// name, and XTDE1390 for a name that is not a lexical QName or whose prefix is not bound. The values are fixed constants of the
/// package and read no host data (design #150 EP-13).
/// </summary>
/// <remarks>
/// Limitations: deviation from the note of XSLT 2.0 16.6.5, which says a processor must not return "2.0" for <c>xsl:version</c> unless
/// it conforms to XSLT 2.0. The package implements the subset of XSLT 2.0 that Schematron needs and is not conformant, but rule sets
/// gate their XSLT 2.0 code on this value, so it is "2.0" under every XSLT binding, including <c>xslt3</c>, which evaluates with the
/// XPath 2.0 grammar; the maintainer may overturn it (decision 3 of issue #285). The feature properties are "no" (no serialization,
/// no schema awareness, no backwards compatible mode until #298, no namespace axis). An unprefixed name has no namespace.
/// </remarks>
internal sealed class SystemPropertyFunction : XPathFunction
{
    private const string XsltNamespace = "http://www.w3.org/1999/XSL/Transform";

    private static readonly string ProductVersion = typeof(SystemPropertyFunction)
        .Assembly.GetName()
        .Version?.ToString()
        ?? "0.0.0.0";

    private readonly XPathStaticContext _statics;

    internal SystemPropertyFunction(XPathStaticContext statics)
        : base(
            XsltFunctionScope.Name("system-property"),
            1,
            1,
            [new XPathParameterType(XPathItemType.String, Occurrence.ExactlyOne)]
        )
    {
        ArgumentNullException.ThrowIfNull(statics);

        _statics = statics;
    }

    /// <inheritdoc />
    public override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation)
    {
        _ = invocation.Arguments[0][0].TryGetString(out var lexical);
        if (!XsltLexicalName.TryExpand(_statics, lexical!, null, out var name))
        {
            return XsltLexicalName.Reject(ref invocation, "XTDE1390", lexical!);
        }

        invocation.Result = XdmSequence.Create(XdmItem.FromString(Value(name)));
        return XPathEvaluationStatus.Ok;
    }

    private static string Value(XPathQName name) =>
        !string.Equals(name.NamespaceName, XsltNamespace, StringComparison.Ordinal)
            ? string.Empty
            : name.LocalName switch
            {
                "version" => "2.0",
                "vendor" => "NetEvolve",
                "vendor-url" => "https://github.com/dailydevops/schemas",
                "product-name" => "NetEvolve.Schemas.Schematron",
                "product-version" => ProductVersion,
                "is-schema-aware" => "no",
                "supports-serialization" => "no",
                "supports-backwards-compatibility" => "no",
                "supports-namespace-axis" => "no",
                _ => string.Empty,
            };
}
