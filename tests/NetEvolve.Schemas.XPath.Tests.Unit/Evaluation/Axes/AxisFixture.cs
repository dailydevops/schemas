namespace NetEvolve.Schemas.XPath.Tests.Unit.Evaluation.Axes;

using System;
using System.Collections.Generic;
using System.Text;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Model;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Evaluation.Axes;
using NetEvolve.Schemas.XPath.Syntax.Parser;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Xdm.Nodes;

/// <summary>
/// The tree <c>&lt;a id="1" p:lang="en"&gt;&lt;b&gt;t1&lt;/b&gt;&lt;!--c--&gt;&lt;?pi d?&gt;&lt;b&gt;t2&lt;i&gt;x&lt;/i&gt;t3&lt;/b&gt;&lt;p:e/&gt;&lt;/a&gt;</c>
/// with <c>p</c> bound to <c>urn:p</c>, and a step evaluator that renders the visited nodes as labels.
/// </summary>
internal static class AxisFixture
{
    public static readonly XPathStaticContext Statics = XPathStaticContext
        .Default.ToBuilder()
        .WithNamespace("p", "urn:p")
        .Build();

    private static readonly XmlDocumentTree Tree = Build();

    /// <summary>The nodes by key: doc, a, id, lang, b1, t1, c, pi, b2, t2, i, x, t3, e.</summary>
    public static readonly Dictionary<string, XPathNode> Nodes = Index(Tree);

    public static XPathNode Node(string key) => Nodes[key];

    public static XmlDocumentTree Build(int fanOut = 0)
    {
        using var b = new XmlTreeBuilder();
        b.StartDocument(new XmlDeclaration("1.0", "utf-8", null));
        var a = Name(b, "a");
        b.StartElement(a, default);
        b.NamespaceDeclaration(b.Names.Intern("p"), b.Names.Intern("urn:p"));
        b.Attribute(Name(b, "id"), "1");
        b.Attribute(Name(b, "lang", "p", "urn:p"), "en");
        b.EndAttributes();
        var bn = Name(b, "b");
        b.StartElement(bn, default);
        b.EndAttributes();
        b.Text("t1", XmlTextKind.Text);
        b.EndElement(bn);
        b.Comment("c");
        b.ProcessingInstruction(b.Names.Intern("pi"), "d");
        b.StartElement(bn, default);
        b.EndAttributes();
        b.Text("t2", XmlTextKind.Text);
        var i = Name(b, "i");
        b.StartElement(i, default);
        b.EndAttributes();
        b.Text("x", XmlTextKind.Text);
        b.EndElement(i);
        b.Text("t3", XmlTextKind.Text);
        b.EndElement(bn);
        var e = Name(b, "e", "p", "urn:p");
        b.StartElement(e, default);
        b.EndAttributes();
        b.EndElement(e);
        for (var n = 0; n < fanOut; n++)
        {
            b.StartElement(bn, default);
            b.EndAttributes();
            for (var m = 0; m < fanOut; m++)
            {
                b.StartElement(i, default);
                b.EndAttributes();
                b.EndElement(i);
            }

            b.EndElement(bn);
        }

        b.EndElement(a);
        b.EndDocument();
        return b.Complete();
    }

    public static string Label(XPathNode node)
    {
        var names = node.Tree!.Names;
        var name = node.Name();
        var qualified = names.GetString(name.Prefix) is { Length: > 0 } prefix
            ? prefix + ":" + names.GetString(name.Local)
            : names.GetString(name.Local);
        return node.Kind switch
        {
            XdmNodeKind.Document => "/",
            XdmNodeKind.Element => qualified,
            XdmNodeKind.Attribute => "@" + qualified,
            XdmNodeKind.Text => "'" + node.StringValue() + "'",
            XdmNodeKind.Comment => "#" + node.StringValue(),
            _ => "?" + qualified,
        };
    }

    public static AxisStep Parse(string step)
    {
        var parsed = XPathParser.TryParse(step, out var expression, out var diagnostic);
        if (!parsed || expression is not AxisStep axisStep)
        {
            throw new InvalidOperationException($"'{step}' is not an axis step: {diagnostic.Message}");
        }

        return axisStep;
    }

    /// <summary>Evaluates one axis step text from a context node and renders the visited nodes in delivery order.</summary>
    public static string Eval(string step, string context, XPathStaticContext? statics = null)
    {
        var parsed = Parse(step);
        if (!XPathNodeTest.TryFromSyntax(parsed.Test, parsed.Axis, statics ?? Statics, out var test))
        {
            throw new InvalidOperationException($"'{step}' is not supported.");
        }

        var contextNode = Node(context);
        var iterator = new XPathAxisIterator(contextNode, parsed.Axis, test.Bind(contextNode.Tree!.Names));
        var text = new StringBuilder();
        while (iterator.MoveNext())
        {
            if (text.Length > 0)
            {
                text.Append(' ');
            }

            text.Append(Label(iterator.Current));
        }

        return text.ToString();
    }

    private static XmlName Name(XmlTreeBuilder b, string local, string prefix = "", string uri = "") =>
        b.Names.Intern(prefix, local, uri);

    private static Dictionary<string, XPathNode> Index(XmlDocumentTree tree)
    {
        var doc = new XPathNode(tree.Document);
        var a = doc.FirstChild();
        var b1 = a.FirstChild();
        var c = b1.NextSibling();
        var pi = c.NextSibling();
        var b2 = pi.NextSibling();
        var t2 = b2.FirstChild();
        var i = t2.NextSibling();
        return new()
        {
            ["doc"] = doc,
            ["a"] = a,
            ["id"] = a.FirstAttribute(),
            ["lang"] = a.FirstAttribute().NextAttribute(),
            ["b1"] = b1,
            ["t1"] = b1.FirstChild(),
            ["c"] = c,
            ["pi"] = pi,
            ["b2"] = b2,
            ["t2"] = t2,
            ["i"] = i,
            ["x"] = i.FirstChild(),
            ["t3"] = i.NextSibling(),
            ["e"] = b2.NextSibling(),
        };
    }
}
