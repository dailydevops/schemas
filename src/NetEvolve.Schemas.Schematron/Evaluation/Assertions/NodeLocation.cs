namespace NetEvolve.Schemas.Schematron.Evaluation.Assertions;

using System;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Model;
using NetEvolve.Schemas.XPath.Xdm.Nodes;

/// <summary>The position and the SVRL location path of a reported node (design #137 AP-14).</summary>
/// <remarks>
/// Limitations: the Xml package records positions for elements only, so an attribute, text, comment, or processing instruction
/// node reports the position of its parent element, and a document node reports 0, 0. The location path is built recursively, so
/// its depth is bounded by the nesting depth that the reader limits. Each step counts its preceding siblings, so reporting every
/// child of one parent costs O(n²) in the child count; the path of a rare report is cheap, and a cached sibling index is the
/// upgrade when a schema reports on very wide parents.
/// </remarks>
internal static class NodeLocation
{
    /// <summary>Gets the line and column of an element, of the parent element for any other node, and 0, 0 for a document node.</summary>
    /// <param name="node">The reported node.</param>
    /// <param name="source">The system identifier of the instance, or <see langword="null"/>.</param>
    /// <returns>The location.</returns>
    internal static DocumentLocation Of(in XPathNode node, string? source)
    {
        var xml = node.Node;
        if (xml.Tree is not XmlDocumentTree tree)
        {
            return new DocumentLocation(source, 0, 0);
        }

        var element = xml.Kind == XmlNodeKind.Element ? xml : xml.Parent;
        if (element.IsEmpty || element.Kind != XmlNodeKind.Element)
        {
            return new DocumentLocation(source, 0, 0);
        }

        var position = tree.GetPosition(element.Index);
        return new DocumentLocation(source, position.Line, position.Column);
    }

    /// <summary>Appends the location path that XPath 1.0 and 2.0 processors evaluate to the node.</summary>
    /// <param name="node">The reported node.</param>
    /// <param name="into">The sink that receives the path.</param>
    internal static void AppendPath(in XPathNode node, TextSink into)
    {
        Argument.ThrowIfNull(into);

        var xml = node.Node;
        if (xml.Kind == XmlNodeKind.Document)
        {
            into.Text("/".AsSpan());
            return;
        }

        AppendStep(xml, into);
    }

    private static void AppendStep(XmlNode node, TextSink into)
    {
        var parent = node.Parent;
        if (!parent.IsEmpty && parent.Kind != XmlNodeKind.Document)
        {
            AppendStep(parent, into);
        }

        var names = node.Tree!.Names;
        switch (node.Kind)
        {
            case XmlNodeKind.Element:
                into.Text("/*[local-name()=".AsSpan());
                AppendLiteral(names.GetString(node.Name.Local), into);
                into.Text(" and namespace-uri()=".AsSpan());
                AppendLiteral(names.GetString(node.Name.NamespaceUri), into);
                into.Text("][".AsSpan());
                into.Integer(Position(node, parent));
                into.Text("]".AsSpan());
                break;
            case XmlNodeKind.Attribute:
                into.Text("/@*[local-name()=".AsSpan());
                AppendLiteral(names.GetString(node.Name.Local), into);
                into.Text(" and namespace-uri()=".AsSpan());
                AppendLiteral(names.GetString(node.Name.NamespaceUri), into);
                into.Text("]".AsSpan());
                break;
            case XmlNodeKind.Text:
                into.Text("/text()[".AsSpan());
                into.Integer(Position(node, parent));
                into.Text("]".AsSpan());
                break;
            case XmlNodeKind.Comment:
                into.Text("/comment()[".AsSpan());
                into.Integer(Position(node, parent));
                into.Text("]".AsSpan());
                break;
            case XmlNodeKind.ProcessingInstruction:
                into.Text("/processing-instruction(".AsSpan());
                AppendLiteral(names.GetString(node.Name.Local), into);
                into.Text(")[".AsSpan());
                into.Integer(Position(node, parent));
                into.Text("]".AsSpan());
                break;
            default:
                break;
        }
    }

    // A literal uses the quote that its text does not contain; XPath 1.0 has no escape.
    private static void AppendLiteral(string text, TextSink into)
    {
        var quote = text.AsSpan().IndexOf('\'') >= 0 ? "\"" : "'";
        into.Text(quote.AsSpan());
        into.Text(text.AsSpan());
        into.Text(quote.AsSpan());
    }

    // The one-based index among the siblings of the same kind and expanded name, as an XPath predicate counts them.
    private static int Position(XmlNode node, XmlNode parent)
    {
        var count = 1;
        var hasName = node.Kind is XmlNodeKind.Element or XmlNodeKind.ProcessingInstruction;
        for (var sibling = parent.FirstChild; !sibling.IsEmpty && sibling != node; sibling = sibling.NextSibling)
        {
            if (sibling.Kind == node.Kind && (!hasName || SameName(sibling.Name, node.Name)))
            {
                count++;
            }
        }

        return count;
    }

    // The prefix does not take part: two prefixes of one namespace name the same element.
    private static bool SameName(XmlName left, XmlName right) =>
        left.Local == right.Local && left.NamespaceUri == right.NamespaceUri;
}
