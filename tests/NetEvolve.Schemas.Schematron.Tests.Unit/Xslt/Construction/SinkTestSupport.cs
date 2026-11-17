namespace NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Construction;

using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Model;
using NetEvolve.Schemas.Xml.Text.NameTable;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;

internal static class SinkTestSupport
{
    internal static SpanInterningNameTable NewTable() => new(1000);

    /// <summary>Builds a document node over one element with one text child and returns the element.</summary>
    internal static XdmItem Element(string name, string text)
    {
        using var builder = new XmlTreeBuilder(NewTable(), null, false);
        var qname = builder.Names.Intern("", name, "");
        builder.StartDocument(default);
        builder.StartElement(qname, default);
        builder.EndAttributes();
        builder.Text(text, XmlTextKind.Text);
        builder.EndElement(qname);
        builder.EndDocument();
        var tree = builder.Complete();
        return XdmItem.FromNode(new XPathNode(new XmlNode(tree, 1)));
    }

    /// <summary>Builds an element with one attribute and returns the attribute node.</summary>
    internal static XdmItem Attribute(string name, string value)
    {
        using var builder = new XmlTreeBuilder(NewTable(), null, false);
        var qname = builder.Names.Intern("", name, "");
        builder.StartElement(qname, default);
        builder.Attribute(qname, value);
        builder.EndAttributes();
        builder.EndElement(qname);
        var tree = builder.Complete();
        return XdmItem.FromNode(new XPathNode(new XmlNode(tree, 0).FirstAttribute));
    }

    internal static XmlNode Node(in XdmItem item)
    {
        _ = item.TryGetNode(out var node);
        return node.Node;
    }

    internal static XdmItem Item(XmlNode node) => XdmItem.FromNode(new XPathNode(node));
}
