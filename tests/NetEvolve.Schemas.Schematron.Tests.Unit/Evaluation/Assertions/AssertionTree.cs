namespace NetEvolve.Schemas.Schematron.Tests.Unit.Evaluation.Assertions;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Model;
using NetEvolve.Schemas.XPath.Xdm.Nodes;

/// <summary>
/// The tree <c>&lt;root&gt;&lt;order id="7"&gt;txt&lt;!--c--&gt;&lt;?pi d?&gt;&lt;item&gt;1&lt;/item&gt;&lt;item&gt;2&lt;/item&gt;&lt;/order&gt;
/// &lt;order/&gt;&lt;p:x xmlns:p="urn:p"/&gt;&lt;/root&gt;</c> with a recorded position for every element.
/// </summary>
internal static class AssertionTree
{
    private static readonly XmlDocumentTree Tree = Build();

    /// <summary>The nodes by key: doc, root (1,1), order1 (2,3), id, txt, c, pi, item1 (3,5), item2 (4,5), order2 (5,3), px (6,3), ns (the namespace node of px).</summary>
    private static readonly Dictionary<string, XPathNode> Nodes = Index(Tree);

    internal static XPathNode Node(string key) =>
        Nodes.TryGetValue(key, out var node) ? node : throw new ArgumentException(key, nameof(key));

    private static XmlName Name(XmlTreeBuilder b, string local, string prefix = "", string uri = "") =>
        b.Names.Intern(prefix, local, uri);

    private static XmlDocumentTree Build()
    {
        using var b = new XmlTreeBuilder();
        b.StartDocument(new XmlDeclaration("1.0", "utf-8", null));
        var root = Name(b, "root");
        var order = Name(b, "order");
        var item = Name(b, "item");
        var x = Name(b, "x", "p", "urn:p");

        b.StartElement(root, new XmlPosition(1, 1, 0));
        b.EndAttributes();

        b.StartElement(order, new XmlPosition(2, 3, 10));
        b.Attribute(Name(b, "id"), "7");
        b.EndAttributes();
        b.Text("txt", XmlTextKind.Text);
        b.Comment("c");
        b.ProcessingInstruction(b.Names.Intern("pi"), "d");
        b.StartElement(item, new XmlPosition(3, 5, 20));
        b.EndAttributes();
        b.Text("1", XmlTextKind.Text);
        b.EndElement(item);
        b.StartElement(item, new XmlPosition(4, 5, 30));
        b.EndAttributes();
        b.Text("2", XmlTextKind.Text);
        b.EndElement(item);
        b.EndElement(order);

        b.StartElement(order, new XmlPosition(5, 3, 40));
        b.EndAttributes();
        b.EndElement(order);

        b.StartElement(x, new XmlPosition(6, 3, 50));
        b.NamespaceDeclaration(b.Names.Intern("p"), b.Names.Intern("urn:p"));
        b.EndAttributes();
        b.EndElement(x);

        b.EndElement(root);
        b.EndDocument();
        return b.Complete();
    }

    private static Dictionary<string, XPathNode> Index(XmlDocumentTree tree)
    {
        var doc = tree.Document;
        var root = doc.FirstChild;
        var order1 = root.FirstChild;
        var txt = order1.FirstChild;
        var comment = txt.NextSibling;
        var pi = comment.NextSibling;
        var item1 = pi.NextSibling;
        var item2 = item1.NextSibling;
        var order2 = order1.NextSibling;
        var px = order2.NextSibling;
        var ns = px.FirstNamespace;
        return new Dictionary<string, XPathNode>(StringComparer.Ordinal)
        {
            ["doc"] = new(doc),
            ["root"] = new(root),
            ["order1"] = new(order1),
            ["id"] = new(order1.FirstAttribute),
            ["txt"] = new(txt),
            ["c"] = new(comment),
            ["pi"] = new(pi),
            ["item1"] = new(item1),
            ["item2"] = new(item2),
            ["order2"] = new(order2),
            ["px"] = new(px),
            ["ns"] = new(ns),
        };
    }
}
