namespace NetEvolve.Schemas.XPath.Tests.Unit.Api;

using System;
using System.Text;
using NetEvolve.Schemas.Xml.Model.Loading;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>Compiles and evaluates through the public API only, and renders a value as text.</summary>
internal static class ApiFixture
{
    public const string FunctionNamespace = "http://www.w3.org/2005/xpath-functions";

    public const string SchemaNamespace = "http://www.w3.org/2001/XMLSchema";

    /// <summary>The static context of the tests: the prefixes <c>xs</c> and <c>fn</c> are declared.</summary>
    public static XPathStaticContext Statics() => Builder().Build();

    /// <summary>A builder that starts from the static context of the tests.</summary>
    public static XPathStaticContext.Builder Builder() =>
        XPathStaticContext
            .Default.ToBuilder()
            .WithNamespace("xs", SchemaNamespace)
            .WithNamespace("fn", FunctionNamespace);

    /// <summary>Loads a document and wraps its document node.</summary>
    public static XPathNode Load(string xml) => new(XmlDocumentLoader.Load(xml.AsSpan()).Document);

    /// <summary>Compiles an expression that must compile.</summary>
    public static XPathExpression Compile(string expression, XPathStaticContext? statics = null) =>
        XPathExpression.Compile(expression, statics ?? Statics());

    /// <summary>Evaluates an expression with an optional context document and renders the outcome, or <c>error:CODE</c>.</summary>
    public static string Run(string expression, string? xml = null, XPathStaticContext? statics = null)
    {
        var result = XPathExpression.TryCompile(expression, statics ?? Statics());
        if (!result.Success)
        {
            return "error:" + result.Diagnostic.Code;
        }

        using var context = new XPathEvaluationContext();
        if (xml is not null)
        {
            context.SetContextItem(XdmItem.FromNode(Load(xml)));
        }

        var outcome = result.Expression!.TryEvaluate(context);
        return outcome.Status == XPathEvaluationStatus.Ok ? Render(outcome.Value) : "error:" + outcome.Diagnostic.Code;
    }

    /// <summary>Renders the items of a value, separated by a bar.</summary>
    public static string Render(in XdmSequence value)
    {
        var text = new StringBuilder();
        for (var i = 0; i < value.Count; i++)
        {
            _ = text.Append(i > 0 ? "|" : string.Empty).Append(Render(value[i]));
        }

        return text.ToString();
    }

    private static string Render(in XdmItem item)
    {
        if (item.TryGetNode(out var node))
        {
            return node.Node.StringValue;
        }

        if (item.TryGetBoolean(out var boolean))
        {
            return boolean ? "true" : "false";
        }

        if (item.TryGetInteger(out var integer))
        {
            return integer.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        if (item.TryGetDouble(out var number))
        {
            return number.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
        }

        return item.TryGetString(out var value) ? value! : item.Kind.ToString();
    }
}
