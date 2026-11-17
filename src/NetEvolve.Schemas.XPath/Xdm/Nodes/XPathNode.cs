#pragma warning disable CA1815 // Approved shape (#123): identity is IsSameNode, not value equality.

namespace NetEvolve.Schemas.XPath.Xdm.Nodes;

using System.Runtime.InteropServices;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>A thin adapter over a node of the document model that adds the data model accessors XPath needs.</summary>
/// <remarks>Limitation: nothing is annotated until schema-aware processing exists, so the annotation is the untyped default.</remarks>
[StructLayout(LayoutKind.Auto)]
public readonly struct XPathNode
{
    /// <summary>Initializes a new instance of the <see cref="XPathNode"/> struct.</summary>
    /// <param name="node">The node of the document model.</param>
    /// <exception cref="System.ArgumentNullException"><paramref name="node"/> is <see langword="default"/>.</exception>
    public XPathNode(XmlNode node)
    {
        Argument.ThrowIfNull(node.Tree);

        Node = node;
    }

    /// <summary>Gets the node of the document model.</summary>
    public XmlNode Node { get; }

    /// <summary>Gets the tree the node belongs to, <see langword="null"/> for <see langword="default"/>.</summary>
    public XmlTree? Tree => Node.Tree;

    /// <summary>Gets the node kind.</summary>
    public XdmNodeKind Kind => (XdmNodeKind)(int)Node.Kind;

    /// <summary>
    /// Gets the type annotation: <c>xs:untyped</c> for documents and elements, <c>xs:untypedAtomic</c> for attributes and text,
    /// and <c>xs:string</c> for the remaining kinds, whose typed value is a string.
    /// </summary>
    public XdmTypeId TypeAnnotation =>
        Kind is XdmNodeKind.Document or XdmNodeKind.Element ? XdmTypeId.Untyped
        : Kind is XdmNodeKind.Attribute or XdmNodeKind.Text ? XdmTypeId.UntypedAtomic
        : XdmTypeId.String;

    /// <summary>Determines whether another node is the same node (the <see langword="is"/> operator).</summary>
    /// <param name="other">The other node.</param>
    /// <returns><see langword="true"/> when both are the same node.</returns>
    public bool IsSameNode(in XPathNode other) => Node == other.Node;

    /// <summary>Compares this node with another in document order; nodes of different trees order stably by tree.</summary>
    /// <param name="other">The other node.</param>
    /// <returns>Less than zero when this node comes first, zero when both are the same node, greater than zero otherwise.</returns>
    public int CompareDocumentOrder(in XPathNode other) => Node.CompareDocumentOrder(other.Node);
}
