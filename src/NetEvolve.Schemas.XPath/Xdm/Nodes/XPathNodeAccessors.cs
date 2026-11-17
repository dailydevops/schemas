namespace NetEvolve.Schemas.XPath.Xdm.Nodes;

using System;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Model;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>The data model accessors over <see cref="XPathNode"/>: name, parent, children, attributes, base URI, string value, and typed value.</summary>
/// <remarks>
/// Navigation accessors return <see langword="default"/> for no node; test it with <see cref="IsEmpty"/>. Limitations: namespace
/// nodes are not exposed (#54), and the string value of a document or element is built as one string because the document model
/// offers no span accessor for it.
/// </remarks>
internal static class XPathNodeAccessors
{
    /// <summary>Determines whether the node is <see langword="default"/>, which stands for no node.</summary>
    /// <param name="node">The node.</param>
    /// <returns><see langword="true"/> for no node.</returns>
    public static bool IsEmpty(this in XPathNode node) => node.Node.IsEmpty;

    /// <summary>Gets the node name: element and attribute names, the target of a processing instruction in the local part.</summary>
    /// <param name="node">The node.</param>
    /// <returns>The name, <see langword="default"/> for the other kinds.</returns>
    public static XmlName Name(this in XPathNode node) => node.Node.Name;

    /// <summary>Gets the parent; the owner element for an attribute.</summary>
    /// <param name="node">The node.</param>
    /// <returns>The parent, <see langword="default"/> for the root.</returns>
    public static XPathNode Parent(this in XPathNode node) => Wrap(node.Node.Parent);

    /// <summary>Gets the first child.</summary>
    /// <param name="node">The node.</param>
    /// <returns>The child, <see langword="default"/> for none.</returns>
    public static XPathNode FirstChild(this in XPathNode node) => Wrap(node.Node.FirstChild);

    /// <summary>Gets the next sibling of a child node.</summary>
    /// <param name="node">The node.</param>
    /// <returns>The sibling, <see langword="default"/> for none.</returns>
    public static XPathNode NextSibling(this in XPathNode node) => Wrap(node.Node.NextSibling);

    /// <summary>Gets the first attribute of an element.</summary>
    /// <param name="node">The node.</param>
    /// <returns>The attribute, <see langword="default"/> for none.</returns>
    public static XPathNode FirstAttribute(this in XPathNode node) => Wrap(node.Node.FirstAttribute);

    /// <summary>Gets the next attribute of an attribute node.</summary>
    /// <param name="node">The node.</param>
    /// <returns>The attribute, <see langword="default"/> for none.</returns>
    public static XPathNode NextAttribute(this in XPathNode node) => Wrap(node.Node.NextAttribute);

    /// <summary>Gets the base URI.</summary>
    /// <param name="node">The node.</param>
    /// <returns>The base URI, <see langword="null"/> when there is none.</returns>
    public static Uri? BaseUri(this in XPathNode node) => node.Node.BaseUri;

    /// <summary>Gets the string value.</summary>
    /// <param name="node">The node.</param>
    /// <returns>The string value.</returns>
    public static string StringValue(this in XPathNode node) => node.Node.StringValue;

    /// <summary>Gets the string value of a text, attribute, comment, or processing instruction node without allocating.</summary>
    /// <param name="node">The node.</param>
    /// <param name="value">The characters, empty when no span is available.</param>
    /// <returns><see langword="true"/> when the node is such a leaf of an <see cref="XmlDocumentTree"/>; otherwise use <see cref="StringValue"/>.</returns>
    public static bool TryGetStringValueSpan(this in XPathNode node, out ReadOnlySpan<char> value)
    {
        if (
            node.Node.Tree is XmlDocumentTree tree
            && node.Kind
                is XdmNodeKind.Text
                    or XdmNodeKind.Attribute
                    or XdmNodeKind.Comment
                    or XdmNodeKind.ProcessingInstruction
        )
        {
            value = tree.GetValueSpan(node.Node.Index);
            return true;
        }

        value = default;
        return false;
    }

    /// <summary>Gets the type annotation from a source.</summary>
    /// <param name="node">The node.</param>
    /// <param name="source">The annotation source.</param>
    /// <returns>The annotation.</returns>
    public static XdmTypeId GetTypeAnnotation(this in XPathNode node, IXdmTypeAnnotationSource source) =>
        source.GetTypeAnnotation(node);

    /// <summary>Gets the typed value with the untyped default: <c>xs:untypedAtomic</c> for documents, elements, attributes, and text, and <c>xs:string</c> for the rest.</summary>
    /// <param name="node">The node.</param>
    /// <returns>The typed value.</returns>
    public static XdmItem TypedValue(this in XPathNode node) => node.TypedValue(UntypedAnnotationSource.Instance);

    /// <summary>Gets the typed value; the source wins where its slot is filled.</summary>
    /// <param name="node">The node.</param>
    /// <param name="source">The annotation source.</param>
    /// <returns>The typed value.</returns>
    public static XdmItem TypedValue(this in XPathNode node, IXdmTypeAnnotationSource source)
    {
        if (source.TryGetTypedValue(node, out var typed))
        {
            return typed;
        }

        var value = node.StringValue();
        return node.TypeAnnotation == XdmTypeId.XsString ? XdmItem.FromString(value) : XdmItem.FromUntypedAtomic(value);
    }

    private static XPathNode Wrap(XmlNode node) => node.IsEmpty ? default : new XPathNode(node);
}
