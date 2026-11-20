namespace NetEvolve.Schemas.XPath.Tests.Unit.Functions.QNames;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.QNames;
using NetEvolve.Schemas.Xml.Model;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Functions.Binding;
using NetEvolve.Schemas.XPath.Types;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// Runs the QName and URI functions through the default catalog, the way a compiled call does: resolve by expanded name and arity,
/// then <see cref="XPathFunction.Call"/>. An argument is written <c>kind:payload</c> (several items joined by <c>|</c>), and <c>()</c>
/// is the empty sequence; a QName is written <c>qname:prefix,namespace,local</c>. A result is shown with its type, an error as
/// <c>error:CODE</c>.
/// </summary>
/// <remarks>
/// The tree is <c>&lt;r xmlns="urn:def" xmlns:p="urn:p" xmlns:q="urn:q" xml:base="http://example.com/dir/file.xml" id="7"&gt;
/// &lt;p:a xmlns="" xmlns:p="urn:p2"&gt;&lt;b xmlns:x="urn:x"&gt;t1&lt;/b&gt;&lt;/p:a&gt;&lt;c xmlns:xml="http://www.w3.org/XML/1998/namespace"/&gt;
/// &lt;d xml:base="sub/"/&gt;&lt;!--cm--&gt;&lt;/r&gt;</c> under a document node; a second tree <c>&lt;o&gt;&lt;q/&gt;&lt;/o&gt;</c> has no
/// namespace declaration and no base URI.
/// </remarks>
internal static class QNamesHarness
{
    internal const string FunctionNamespace = "http://www.w3.org/2005/xpath-functions";
    internal const string XmlNamespace = "http://www.w3.org/XML/1998/namespace";

    private static readonly XmlDocumentTree Main = BuildMain();
    private static readonly XmlDocumentTree Second = BuildSecond();

    /// <summary>The nodes by key: doc, r, r@id, a, b, b/t, c, d, cm; and o.doc, o, q of the second tree.</summary>
    internal static readonly Dictionary<string, XPathNode> Nodes = Index();

    /// <summary>Resolves a built-in function of the default catalog for a language version.</summary>
    internal static bool TryResolve(
        string local,
        int arity,
        out XPathFunction? function,
        XPathVersion version = XPathVersion.V20
    ) =>
        FunctionCatalog.Default.Registry.TryResolve(
            XPathStaticContext.Default.ToBuilder().WithVersion(version).Build(),
            new XPathQName(FunctionNamespace, local),
            arity,
            XPathHostRequirements.None,
            out function
        );

    /// <summary>Calls a function of the default catalog in a fresh evaluation context without a focus.</summary>
    internal static string Run(string local, params string[] arguments) => RunWith(null, local, arguments);

    /// <summary>Calls a function of the default catalog with one string argument that may contain the separators of the argument notation.</summary>
    internal static string RunString(string local, string value)
    {
        TryResolve(local, 1, out var function);
        using var context = new XPathEvaluationContext();
        try
        {
            return Show(function!.Call(context, [XdmSequence.Create(XdmItem.FromString(value))]));
        }
        catch (XPathException failure)
        {
            return "error:" + failure.Diagnostic.Code;
        }
    }

    /// <summary>Calls a function of the default catalog with the given context item.</summary>
    internal static string RunWithContextItem(string contextItem, string local, params string[] arguments) =>
        RunWith(context => context.SetContextItem(ParseItem(contextItem)), local, arguments);

    /// <summary>Calls a function of the default catalog after <paramref name="setup"/> configured the evaluation context.</summary>
    internal static string RunWith(Action<XPathEvaluationContext>? setup, string local, params string[] arguments)
    {
        if (!TryResolve(local, arguments.Length, out var function))
        {
            return "error:not-registered";
        }

        return RunFunction(function!, setup, arguments);
    }

    /// <summary>Calls a given function instance.</summary>
    internal static string RunFunction(
        XPathFunction function,
        Action<XPathEvaluationContext>? setup,
        params string[] arguments
    )
    {
        using var context = new XPathEvaluationContext();
        setup?.Invoke(context);
        var sequences = new XdmSequence[arguments.Length];
        for (var i = 0; i < sequences.Length; i++)
        {
            sequences[i] = Parse(arguments[i]);
        }

        try
        {
            return Show(function.Call(context, sequences));
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
        switch (kind)
        {
            case "node":
                return XdmItem.FromNode(Nodes[payload]);
            case "string":
                return XdmItem.FromString(payload);
            case "untypedAtomic":
                return XdmItem.FromUntypedAtomic(payload);
            case "anyURI":
                return XdmItem.FromAnyUri(payload);
            case "integer":
                return XdmItem.FromInteger(long.Parse(payload, CultureInfo.InvariantCulture));
            case "qname":
                var parts = payload.Split(',');
                return XdmItem.FromQName(
                    new XdmQName(
                        new XsdQName(parts[1].Length == 0 ? null : parts[1], parts[2]),
                        parts[0].Length == 0 ? null : parts[0]
                    )
                );
            default:
                throw new ArgumentException("Unknown item kind " + kind, nameof(text));
        }
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
        if (item.TryGetQName(out var qname))
        {
            return "QName:"
                + (qname!.Prefix is null ? string.Empty : qname.Prefix + ":")
                + qname.Name.LocalName
                + "{"
                + qname.Name.NamespaceName
                + "}";
        }

        if (item.TryGetString(out var text))
        {
            return (item.TypeId == XdmTypes.Get("NCName") ? "NCName:" : "string:") + text;
        }

        if (item.TryGetAnyUri(out var uri))
        {
            return "anyURI:" + uri;
        }

        return item.TryGetInteger(out var integer)
            ? "integer:" + integer.ToString(CultureInfo.InvariantCulture)
            : item.Kind.ToString();
    }

    private static XmlName Name(XmlTreeBuilder b, string local, string prefix = "", string uri = "") =>
        b.Names.Intern(prefix, local, uri);

    private static void Declare(XmlTreeBuilder b, string prefix, string uri) =>
        b.NamespaceDeclaration(b.Names.Intern(prefix), b.Names.Intern(uri));

    private static XmlDocumentTree BuildMain()
    {
        using var b = new XmlTreeBuilder();
        b.StartDocument(new XmlDeclaration("1.0", "utf-8", null));
        var r = Name(b, "r", "", "urn:def");
        b.StartElement(r, default);
        Declare(b, "", "urn:def");
        Declare(b, "p", "urn:p");
        Declare(b, "q", "urn:q");
        b.Attribute(Name(b, "base", "xml", XmlNamespace), "http://example.com/dir/file.xml");
        b.Attribute(Name(b, "id"), "7");
        b.EndAttributes();

        var a = Name(b, "a", "p", "urn:p2");
        b.StartElement(a, default);
        Declare(b, "", string.Empty);
        Declare(b, "p", "urn:p2");
        b.EndAttributes();
        var bn = Name(b, "b");
        b.StartElement(bn, default);
        Declare(b, "x", "urn:x");
        b.EndAttributes();
        b.Text("t1", XmlTextKind.Text);
        b.EndElement(bn);
        b.EndElement(a);

        var c = Name(b, "c", "", "urn:def");
        b.StartElement(c, default);
        Declare(b, "xml", XmlNamespace);
        b.EndAttributes();
        b.EndElement(c);

        var d = Name(b, "d", "", "urn:def");
        b.StartElement(d, default);
        b.Attribute(Name(b, "base", "xml", XmlNamespace), "sub/");
        b.EndAttributes();
        b.EndElement(d);

        b.Comment("cm");
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
        var q = Name(b, "q");
        b.StartElement(q, default);
        b.EndAttributes();
        b.EndElement(q);
        b.EndElement(o);
        b.EndDocument();
        return b.Complete();
    }

    private static Dictionary<string, XPathNode> Index()
    {
        var doc = new XPathNode(Main.Document);
        var r = doc.FirstChild();
        var a = r.FirstChild();
        var c = a.NextSibling();
        var d = c.NextSibling();
        var o = new XPathNode(Second.Document);
        return new Dictionary<string, XPathNode>(StringComparer.Ordinal)
        {
            ["doc"] = doc,
            ["r"] = r,
            ["r@id"] = r.FirstAttribute().NextAttribute(),
            ["a"] = a,
            ["b"] = a.FirstChild(),
            ["b/t"] = a.FirstChild().FirstChild(),
            ["c"] = c,
            ["d"] = d,
            ["cm"] = d.NextSibling(),
            ["o.doc"] = o,
            ["o"] = o.FirstChild(),
            ["q"] = o.FirstChild().FirstChild(),
        };
    }
}
