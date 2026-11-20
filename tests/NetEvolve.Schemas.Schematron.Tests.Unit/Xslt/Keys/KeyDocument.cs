namespace NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Keys;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Model;
using NetEvolve.Schemas.XPath.Xdm.Nodes;

/// <summary>
/// One instance document of the key tests, built fresh for each test so that a second document is a second tree:
/// <c>&lt;catalog&gt;&lt;item id="i1" code="a" group="g1"/&gt;&lt;item id="i2" code="b" group="g1"/&gt;&lt;item id="i3" code="A" group="g2"&gt;
/// &lt;sub id="s1" code="x"/&gt;&lt;/item&gt;&lt;!--c--&gt;&lt;?pi d?&gt;&lt;other id="o1" code="a"&gt;txt&lt;/other&gt;&lt;/catalog&gt;</c>. The nodes are looked up by the <c>id</c> of an
/// element, <c>doc</c>, <c>catalog</c>, <c>comment</c>, and <c>pi</c>; an attribute is its element plus <c>@</c> and its name, such as
/// <c>i1@code</c>, and the text of an element is its element plus <c>#text</c>.
/// </summary>
internal sealed class KeyDocument
{
    private readonly Dictionary<string, XPathNode> _nodes = new(StringComparer.Ordinal);

    internal KeyDocument()
    {
        using var b = new XmlTreeBuilder();
        b.StartDocument(new XmlDeclaration("1.0", "utf-8", null));
        Open(b, "catalog", []);
        Leaf(b, "item", "i1", "a", "g1");
        Leaf(b, "item", "i2", "b", "g1");
        Open(b, "item", ["i3", "A", "g2"]);
        Leaf(b, "sub", "s1", "x", null);
        Close(b, "item");
        b.Comment("c");
        b.ProcessingInstruction(b.Names.Intern("pi"), "d");
        b.StartElement(Name(b, "other"), default);
        Attributes(b, "o1", "a", null);
        b.EndAttributes();
        b.Text("txt", XmlTextKind.Text);
        Close(b, "other");
        Close(b, "catalog");
        b.EndDocument();
        Tree = b.Complete();
        for (var child = Tree.Document.FirstChild; !child.IsEmpty; child = child.NextSibling)
        {
            Walk(child, Tree.Names);
        }

        _nodes["doc"] = new XPathNode(Tree.Document);
    }

    internal XmlDocumentTree Tree { get; }

    internal XPathNode this[string key] => _nodes[key];

    /// <summary>Renders nodes as the keys of this document in the order given, joined by a space.</summary>
    internal string Keys(IEnumerable<XPathNode> nodes)
    {
        var keys = new List<string>();
        foreach (var node in nodes)
        {
            var found = "?";
            foreach (var pair in _nodes)
            {
                if (pair.Value == node)
                {
                    found = pair.Key;
                    break;
                }
            }

            keys.Add(found);
        }

        return string.Join(" ", keys);
    }

    private static XmlName Name(XmlTreeBuilder b, string local) => b.Names.Intern(string.Empty, local, string.Empty);

    private static void Open(XmlTreeBuilder b, string local, string[] attributes)
    {
        b.StartElement(Name(b, local), default);
        if (attributes.Length == 3)
        {
            Attributes(b, attributes[0], attributes[1], attributes[2]);
        }

        b.EndAttributes();
    }

    private static void Close(XmlTreeBuilder b, string local) => b.EndElement(Name(b, local));

    private static void Leaf(XmlTreeBuilder b, string local, string id, string code, string? group)
    {
        b.StartElement(Name(b, local), default);
        Attributes(b, id, code, group);
        b.EndAttributes();
        b.EndElement(Name(b, local));
    }

    private static void Attributes(XmlTreeBuilder b, string id, string code, string? group)
    {
        b.Attribute(Name(b, "id"), id);
        b.Attribute(Name(b, "code"), code);
        if (group is not null)
        {
            b.Attribute(Name(b, "group"), group);
        }
    }

    private void Walk(XmlNode node, XmlNameTable names)
    {
        if (node.Kind == XmlNodeKind.Comment)
        {
            _nodes["comment"] = new XPathNode(node);
            return;
        }

        if (node.Kind == XmlNodeKind.ProcessingInstruction)
        {
            _nodes["pi"] = new XPathNode(node);
            return;
        }

        var key = names.GetString(node.Name.Local);
        for (var attribute = node.FirstAttribute; !attribute.IsEmpty; attribute = attribute.NextAttribute)
        {
            if (string.Equals(names.GetString(attribute.Name.Local), "id", StringComparison.Ordinal))
            {
                key = attribute.StringValue;
            }
        }

        _nodes[key] = new XPathNode(node);
        for (var attribute = node.FirstAttribute; !attribute.IsEmpty; attribute = attribute.NextAttribute)
        {
            _nodes[key + "@" + names.GetString(attribute.Name.Local)] = new XPathNode(attribute);
        }

        for (var child = node.FirstChild; !child.IsEmpty; child = child.NextSibling)
        {
            if (child.Kind == XmlNodeKind.Text)
            {
                _nodes[key + "#text"] = new XPathNode(child);
            }
            else
            {
                Walk(child, names);
            }
        }
    }
}
