namespace NetEvolve.Schemas.XPath.Tests.Unit.Functions.Nodes;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.DateTime;
using NetEvolve.Schemas.Xml.Datatypes.Decimals;
using NetEvolve.Schemas.Xml.Datatypes.Duration;
using NetEvolve.Schemas.Xml.Model;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Functions.Binding;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// Runs the node and context functions through the default catalog, the way a compiled call does: resolve by expanded name and
/// arity, then <see cref="XPathFunction.Call"/>. An argument is written <c>kind:payload</c> (several items joined by <c>|</c>), and
/// <c>()</c> is the empty sequence; a result is shown with its type, a node by its key, an error as <c>error:CODE</c>.
/// </summary>
/// <remarks>
/// The tree is <c>&lt;r xml:lang="en-US" id="7" xmlns:p="urn:p"&gt;&lt;p:a x="1" p:y="2"&gt;t1&lt;/p:a&gt;&lt;b xml:lang="de"&gt;&lt;c/&gt;&lt;/b&gt;
/// &lt;d xml:lang=""/&gt;&lt;e&gt;&lt;!--cm--&gt;&lt;?target data?&gt;&lt;/e&gt;&lt;f xml:lang="EN"&gt;&lt;g/&gt;&lt;/f&gt;&lt;h xmlns="urn:d"/&gt;&lt;/r&gt;</c>
/// under a document node; a second tree <c>&lt;o&gt;&lt;q/&gt;&lt;/o&gt;</c> has no <c>xml:lang</c>.
/// </remarks>
internal static class NodesHarness
{
    internal const string FunctionNamespace = "http://www.w3.org/2005/xpath-functions";

    private const string XmlNamespace = "http://www.w3.org/XML/1998/namespace";

    private static readonly XmlDocumentTree Main = BuildMain();
    private static readonly XmlDocumentTree Second = BuildSecond();

    /// <summary>The nodes by key: doc, r, r@lang, r@id, a, a@x, a@y, a/t, b, b@lang, c, d, d@lang, e, cm, pi, f, f@lang, g, h; and o.doc, o, q of the second tree.</summary>
    internal static readonly Dictionary<string, XPathNode> Nodes = Index();

    /// <summary>Resolves a built-in function of the default catalog for a language version and the host flags that are set.</summary>
    internal static bool TryResolve(
        string local,
        int arity,
        out XPathFunction? function,
        XPathVersion version = XPathVersion.V20,
        XPathHostRequirements host = XPathHostRequirements.None
    ) =>
        FunctionCatalog.Default.Registry.TryResolve(
            XPathStaticContext.Default.ToBuilder().WithVersion(version).Build(),
            new XPathQName(FunctionNamespace, local),
            arity,
            host,
            out function
        );

    /// <summary>Calls a function in a fresh evaluation context without a focus.</summary>
    internal static string Run(string local, params string[] arguments) => RunWith(null, local, arguments);

    /// <summary>Calls a function with the given context item, at position 1 of 1.</summary>
    internal static string RunWithContextItem(string contextItem, string local, params string[] arguments) =>
        RunWith(context => context.SetContextItem(ParseItem(contextItem)), local, arguments);

    /// <summary>Calls a function after <paramref name="setup"/> configured the evaluation context.</summary>
    internal static string RunWith(Action<XPathEvaluationContext>? setup, string local, params string[] arguments)
    {
        using var context = new XPathEvaluationContext();
        setup?.Invoke(context);
        return RunIn(context, local, arguments);
    }

    /// <summary>Calls a function in an existing evaluation context, as an XSLT 2.0 host would: the flag makes <c>generate-id</c> visible in the 2.0 static context.</summary>
    internal static string RunIn(XPathEvaluationContext context, string local, params string[] arguments)
    {
        if (!TryResolve(local, arguments.Length, out var function, host: XPathHostRequirements.Xslt20Host))
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

    /// <summary>Parses a sequence: <c>()</c> is empty, items are joined by <c>|</c>.</summary>
    internal static XdmSequence Parse(string argument)
    {
        if (string.Equals(argument, "()", StringComparison.Ordinal))
        {
            return XdmSequence.Empty;
        }

        var parts = argument.Split('|');
        if (parts.Length == 1)
        {
            return XdmSequence.Create(ParseItem(parts[0]));
        }

        var items = new XdmItem[parts.Length];
        for (var i = 0; i < items.Length; i++)
        {
            items[i] = ParseItem(parts[i]);
        }

        return XdmSequence.Create(items);
    }

    /// <summary>Parses one item written <c>kind:payload</c>.</summary>
    internal static XdmItem ParseItem(string text)
    {
        var colon = text.IndexOf(':', StringComparison.Ordinal);
        var kind = text.Substring(0, colon);
        var payload = text.Substring(colon + 1);
        return kind switch
        {
            "node" => XdmItem.FromNode(Nodes[payload]),
            "string" => XdmItem.FromString(payload),
            "untypedAtomic" => XdmItem.FromUntypedAtomic(payload),
            "anyURI" => XdmItem.FromAnyUri(payload),
            "boolean" => XdmItem.FromBoolean(string.Equals(payload, "true", StringComparison.Ordinal)),
            "integer" => XdmItem.FromInteger(long.Parse(payload, CultureInfo.InvariantCulture)),
            "decimal" => XdmItem.FromDecimal(XsdDecimal.Parse(payload).Value),
            "double" => XdmItem.FromDouble(ParseFloating(payload)),
            "float" => XdmItem.FromFloat((float)ParseFloating(payload)),
            "date" => XdmItem.FromDateTime(XsdDateTime.Parse(payload, XsdDateTimeKind.Date, XsdVersion.V10).Value),
            "dateTime" => XdmItem.FromDateTime(
                XsdDateTime.Parse(payload, XsdDateTimeKind.DateTime, XsdVersion.V10).Value
            ),
            "duration" => XdmItem.FromDuration(XsdDuration.Parse(payload, DurationKind.Duration).Value),
            "yearMonthDuration" => XdmItem.FromDuration(
                XsdDuration.Parse(payload, DurationKind.YearMonth).Value,
                DurationKind.YearMonth
            ),
            _ => throw new ArgumentException("Unknown item kind " + kind, nameof(text)),
        };
    }

    /// <summary>Shows a result sequence: each item with its type, separated by a comma.</summary>
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
        if (item.TryGetNode(out var node))
        {
            foreach (var entry in Nodes)
            {
                if (entry.Value.IsSameNode(in node))
                {
                    return "node:" + entry.Key;
                }
            }

            return "node:unknown";
        }

        if (item.TryGetString(out var text))
        {
            return "string:" + text;
        }

        if (item.TryGetAnyUri(out var uri))
        {
            return "anyURI:" + uri;
        }

        if (item.TryGetUntypedAtomic(out var untyped))
        {
            return "untypedAtomic:" + untyped;
        }

        if (item.TryGetBoolean(out var boolean))
        {
            return "boolean:" + (boolean ? "true" : "false");
        }

        return item.TryGetInteger(out var integer)
            ? "integer:" + integer.ToString(CultureInfo.InvariantCulture)
            : item.Kind.ToString();
    }

    private static double ParseFloating(string payload) =>
        payload switch
        {
            "INF" => double.PositiveInfinity,
            "-INF" => double.NegativeInfinity,
            "NaN" => double.NaN,
            _ => double.Parse(payload, NumberStyles.Float, CultureInfo.InvariantCulture),
        };

    private static XmlName Name(XmlTreeBuilder b, string local, string prefix = "", string uri = "") =>
        b.Names.Intern(prefix, local, uri);

    private static void Leaf(XmlTreeBuilder b, XmlName name, string? lang = null)
    {
        b.StartElement(name, default);
        if (lang is not null)
        {
            b.Attribute(Name(b, "lang", "xml", XmlNamespace), lang);
        }

        b.EndAttributes();
        b.EndElement(name);
    }

    private static XmlDocumentTree BuildMain()
    {
        using var b = new XmlTreeBuilder();
        b.StartDocument(new XmlDeclaration("1.0", "utf-8", null));
        var r = Name(b, "r");
        b.StartElement(r, default);
        b.NamespaceDeclaration(b.Names.Intern("p"), b.Names.Intern("urn:p"));
        b.Attribute(Name(b, "lang", "xml", XmlNamespace), "en-US");
        b.Attribute(Name(b, "id"), "7");
        b.EndAttributes();

        var a = Name(b, "a", "p", "urn:p");
        b.StartElement(a, default);
        b.Attribute(Name(b, "x"), "1");
        b.Attribute(Name(b, "y", "p", "urn:p"), "2");
        b.EndAttributes();
        b.Text("t1", XmlTextKind.Text);
        b.EndElement(a);

        var bn = Name(b, "b");
        b.StartElement(bn, default);
        b.Attribute(Name(b, "lang", "xml", XmlNamespace), "de");
        b.EndAttributes();
        Leaf(b, Name(b, "c"));
        b.EndElement(bn);

        Leaf(b, Name(b, "d"), "");

        var e = Name(b, "e");
        b.StartElement(e, default);
        b.EndAttributes();
        b.Comment("cm");
        b.ProcessingInstruction(b.Names.Intern("target"), "data");
        b.EndElement(e);

        var f = Name(b, "f");
        b.StartElement(f, default);
        b.Attribute(Name(b, "lang", "xml", XmlNamespace), "EN");
        b.EndAttributes();
        Leaf(b, Name(b, "g"));
        b.EndElement(f);

        Leaf(b, Name(b, "h", "", "urn:d"));
        b.EndElement(r);
        b.EndDocument();
        return b.Complete();
    }

    private static XmlDocumentTree BuildSecond()
    {
        using var b = new XmlTreeBuilder();
        b.StartDocument(new XmlDeclaration("1.0", "utf-8", null));
        var o = Name(b, "o");
        b.StartElement(o, default);
        b.EndAttributes();
        Leaf(b, Name(b, "q"));
        b.EndElement(o);
        b.EndDocument();
        return b.Complete();
    }

    private static Dictionary<string, XPathNode> Index()
    {
        var doc = new XPathNode(Main.Document);
        var r = doc.FirstChild();
        var a = r.FirstChild();
        var b = a.NextSibling();
        var d = b.NextSibling();
        var e = d.NextSibling();
        var f = e.NextSibling();
        var o = new XPathNode(Second.Document);
        return new Dictionary<string, XPathNode>(StringComparer.Ordinal)
        {
            ["doc"] = doc,
            ["r"] = r,
            ["r@lang"] = r.FirstAttribute(),
            ["r@id"] = r.FirstAttribute().NextAttribute(),
            ["a"] = a,
            ["a@x"] = a.FirstAttribute(),
            ["a@y"] = a.FirstAttribute().NextAttribute(),
            ["a/t"] = a.FirstChild(),
            ["b"] = b,
            ["b@lang"] = b.FirstAttribute(),
            ["c"] = b.FirstChild(),
            ["d"] = d,
            ["d@lang"] = d.FirstAttribute(),
            ["e"] = e,
            ["cm"] = e.FirstChild(),
            ["pi"] = e.FirstChild().NextSibling(),
            ["f"] = f,
            ["f@lang"] = f.FirstAttribute(),
            ["g"] = f.FirstChild(),
            ["h"] = f.NextSibling(),
            ["o.doc"] = o,
            ["o"] = o.FirstChild(),
            ["q"] = o.FirstChild().FirstChild(),
        };
    }
}
