namespace NetEvolve.Schemas.Xml.Contracts;

using System;
using System.Collections.Generic;
using System.Threading;

/// <summary>
/// An immutable tree of nodes addressed by index; <see cref="XmlNode"/> is the allocation-free handle over it. The immutable
/// arena of XML-12 derives from this class; only this assembly can derive, so immutability and safe concurrent reads hold.
/// </summary>
/// <remarks>
/// Index 0 is the document node. Accessors return <see cref="NoNode"/> where a node does not exist. Attribute and namespace
/// nodes are reached through their own first and next accessors; they are never children. A namespace node's name holds its
/// prefix in <see cref="XmlName.Local"/>, and its string value is the namespace URI.
/// </remarks>
public abstract class XmlTree
{
    /// <summary>The index that stands for no node.</summary>
    public const int NoNode = -1;

    private static long s_nextTreeId;

    /// <summary>Initializes a new instance of the <see cref="XmlTree"/> class.</summary>
    private protected XmlTree() => TreeId = Interlocked.Increment(ref s_nextTreeId);

    /// <summary>Gets the key that orders the nodes of different trees: stable, implementation-defined, and unique per tree.</summary>
    public long TreeId { get; }

    /// <summary>Gets the document node.</summary>
    public XmlNode Document => new(this, 0);

    /// <summary>Gets the name table that resolves the handles of every name in this tree.</summary>
    public abstract XmlNameTable Names { get; }

    /// <summary>Gets the unparsed entity declarations of the document, empty without a doctype handler.</summary>
    public abstract IReadOnlyList<XmlUnparsedEntity> UnparsedEntities { get; }

    /// <summary>Gets the notation declarations of the document, empty without a doctype handler.</summary>
    public abstract IReadOnlyList<XmlNotation> Notations { get; }

    /// <summary>Gets the kind of a node.</summary>
    /// <param name="index">The node index.</param>
    /// <returns>The kind.</returns>
    protected internal abstract XmlNodeKind GetKind(int index);

    /// <summary>Gets the name of a node: element and attribute names, the target of a processing instruction in <see cref="XmlName.Local"/>, the prefix of a namespace node in <see cref="XmlName.Local"/>, and <see langword="default"/> otherwise.</summary>
    /// <param name="index">The node index.</param>
    /// <returns>The name.</returns>
    protected internal abstract XmlName GetName(int index);

    /// <summary>Gets the parent of a node; the owner element for an attribute or namespace node.</summary>
    /// <param name="index">The node index.</param>
    /// <returns>The parent index, <see cref="NoNode"/> for the root.</returns>
    protected internal abstract int GetParent(int index);

    /// <summary>Gets the first child of a document or element node.</summary>
    /// <param name="index">The node index.</param>
    /// <returns>The child index, <see cref="NoNode"/> for none.</returns>
    protected internal abstract int GetFirstChild(int index);

    /// <summary>Gets the next sibling of a child node.</summary>
    /// <param name="index">The node index.</param>
    /// <returns>The sibling index, <see cref="NoNode"/> for none.</returns>
    protected internal abstract int GetNextSibling(int index);

    /// <summary>Gets the first attribute of an element node.</summary>
    /// <param name="index">The node index.</param>
    /// <returns>The attribute index, <see cref="NoNode"/> for none.</returns>
    protected internal abstract int GetFirstAttribute(int index);

    /// <summary>Gets the next attribute of an attribute node, in source order.</summary>
    /// <param name="index">The node index.</param>
    /// <returns>The attribute index, <see cref="NoNode"/> for none.</returns>
    protected internal abstract int GetNextAttribute(int index);

    /// <summary>Gets the first in-scope namespace node of an element node.</summary>
    /// <param name="index">The node index.</param>
    /// <returns>The namespace node index, <see cref="NoNode"/> for none.</returns>
    protected internal abstract int GetFirstNamespace(int index);

    /// <summary>Gets the next in-scope namespace node of a namespace node.</summary>
    /// <param name="index">The node index.</param>
    /// <returns>The namespace node index, <see cref="NoNode"/> for none.</returns>
    protected internal abstract int GetNextNamespace(int index);

    /// <summary>Gets the string value of a node as defined by the data model.</summary>
    /// <param name="index">The node index.</param>
    /// <returns>The string value.</returns>
    protected internal abstract string GetStringValue(int index);

    /// <summary>Gets the base URI of a node.</summary>
    /// <param name="index">The node index.</param>
    /// <returns>The base URI, <see langword="null"/> when there is none.</returns>
    protected internal abstract Uri? GetBaseUri(int index);

    /// <summary>Gets the document URI of a document node.</summary>
    /// <param name="index">The node index.</param>
    /// <returns>The document URI, <see langword="null"/> when there is none.</returns>
    protected internal abstract Uri? GetDocumentUri(int index);

    /// <summary>Compares two nodes of this tree in document order; an element precedes its namespace nodes, attributes, and children.</summary>
    /// <param name="first">The index of the first node.</param>
    /// <param name="second">The index of the second node.</param>
    /// <returns>Less than zero when the first node precedes the second, zero when they are the same node, greater than zero otherwise.</returns>
    protected internal abstract int CompareDocumentOrder(int first, int second);
}
