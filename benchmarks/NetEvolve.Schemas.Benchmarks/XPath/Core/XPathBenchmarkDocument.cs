namespace NetEvolve.Schemas.Benchmarks.XPath.Core;

using System.Xml;
using System.Xml.XPath;
using NetEvolve.Schemas.Xml.Model;
using NetEvolve.Schemas.Xml.Model.Loading;
using NetEvolve.Schemas.XPath.Xdm.Nodes;

/// <summary>The benchmark invoice, loaded for this library and for <c>System.Xml.XPath</c>, both with all whitespace kept.</summary>
internal static class XPathBenchmarkDocument
{
    private static readonly string Text = XPathBenchmarkInput.CreateInvoice();

    /// <summary>Loads the invoice into the document model of this library.</summary>
    public static XmlDocumentTree LoadTree() => XmlDocumentLoader.Load(Text.AsSpan());

    /// <summary>Loads the invoice for <c>System.Xml.XPath</c>.</summary>
    public static XPathNavigator CreateNavigator()
    {
        using var reader = XmlReader.Create(new StringReader(Text));
        return new XPathDocument(reader, XmlSpace.Preserve).CreateNavigator();
    }

    /// <summary>Wraps the document node of a loaded tree.</summary>
    public static XPathNode Root(XmlDocumentTree tree) => new(tree.Document);

    /// <summary>Counts the items of a <c>System.Xml.XPath</c> result: the nodes of a node-set, otherwise one.</summary>
    public static int Count(object result) => result is XPathNodeIterator nodes ? nodes.Count : 1;
}
