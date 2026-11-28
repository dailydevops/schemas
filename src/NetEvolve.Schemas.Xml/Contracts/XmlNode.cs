namespace NetEvolve.Schemas.Xml.Contracts;

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using NetEvolve.Schemas.Xml.Dtd.Model;

/// <summary>A handle for one node of an <see cref="XmlTree"/>: the tree and an index. Every member delegates to the tree.</summary>
/// <remarks>
/// Two handles are equal when they have the same tree instance and the same index. Members of <see langword="default"/> throw
/// <see cref="NullReferenceException"/>; check <see cref="IsEmpty"/> first. Hot paths that walk many nodes of one tree should
/// call the tree's accessors directly through <see cref="FirstChild"/> and <see cref="NextSibling"/> batches rather than re-enter.
/// </remarks>
[StructLayout(LayoutKind.Auto)]
public readonly struct XmlNode : IEquatable<XmlNode>
{
    private readonly XmlTree? _tree;

    /// <summary>Initializes a new instance of the <see cref="XmlNode"/> struct.</summary>
    /// <param name="tree">The tree.</param>
    /// <param name="index">The node index, zero or greater.</param>
    /// <exception cref="ArgumentNullException"><paramref name="tree"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is negative.</exception>
    public XmlNode(XmlTree tree, int index)
    {
        ArgumentNullException.ThrowIfNull(tree);
        ArgumentOutOfRangeException.ThrowIfLessThan(index, 0);
        _tree = tree;
        Index = index;
    }

    /// <summary>Gets the tree, <see langword="null"/> for <see langword="default"/>.</summary>
    public XmlTree? Tree => _tree;

    /// <summary>Gets the node index.</summary>
    public int Index { get; }

    /// <summary>Gets a value indicating whether this is <see langword="default"/>, which stands for no node.</summary>
    public bool IsEmpty => _tree is null;

    /// <summary>Gets the node kind.</summary>
    public XmlNodeKind Kind => _tree!.GetKind(Index);

    /// <summary>Gets the node name.</summary>
    public XmlName Name => _tree!.GetName(Index);

    /// <summary>Gets the parent; empty for the root.</summary>
    public XmlNode Parent => Wrap(_tree!.GetParent(Index));

    /// <summary>Gets the first child; empty for none.</summary>
    public XmlNode FirstChild => Wrap(_tree!.GetFirstChild(Index));

    /// <summary>Gets the next sibling; empty for none.</summary>
    public XmlNode NextSibling => Wrap(_tree!.GetNextSibling(Index));

    /// <summary>Gets the first attribute; empty for none.</summary>
    public XmlNode FirstAttribute => Wrap(_tree!.GetFirstAttribute(Index));

    /// <summary>Gets the next attribute; empty for none.</summary>
    public XmlNode NextAttribute => Wrap(_tree!.GetNextAttribute(Index));

    /// <summary>Gets the first in-scope namespace node; empty for none.</summary>
    public XmlNode FirstNamespace => Wrap(_tree!.GetFirstNamespace(Index));

    /// <summary>Gets the next in-scope namespace node; empty for none.</summary>
    public XmlNode NextNamespace => Wrap(_tree!.GetNextNamespace(Index));

    /// <summary>Gets the declared type of an attribute node (including <see cref="DtdAttributeType.Id"/>); <see langword="null"/> for other nodes, an undeclared attribute, and a tree built without DTD processing.</summary>
    public DtdAttributeType? AttributeType => _tree!.GetAttributeType(Index);

    /// <summary>Gets a value indicating whether the document specified the node; <see langword="false"/> only for an attribute that was added from the default of its attribute definition.</summary>
    public bool IsSpecified => _tree!.GetIsSpecified(Index);

    /// <summary>Gets the string value.</summary>
    public string StringValue => _tree!.GetStringValue(Index);

    /// <summary>Gets the base URI, <see langword="null"/> when there is none.</summary>
    public Uri? BaseUri => _tree!.GetBaseUri(Index);

    /// <summary>Gets the document URI, <see langword="null"/> when there is none.</summary>
    public Uri? DocumentUri => _tree!.GetDocumentUri(Index);

    /// <summary>Compares this node with another in document order; nodes of different trees order by <see cref="XmlTree.TreeId"/>.</summary>
    /// <param name="other">The other node.</param>
    /// <returns>Less than zero when this node comes first, zero when both are the same node, greater than zero otherwise.</returns>
    public int CompareDocumentOrder(XmlNode other) =>
        ReferenceEquals(_tree, other._tree)
            ? _tree!.CompareDocumentOrder(Index, other.Index)
            : _tree!.TreeId.CompareTo(other._tree!.TreeId);

    /// <inheritdoc />
    public bool Equals(XmlNode other) => ReferenceEquals(_tree, other._tree) && Index == other.Index;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is XmlNode other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => unchecked((RuntimeHelpers.GetHashCode(_tree) * 31) + Index);

    /// <summary>Determines whether two handles denote the same node.</summary>
    /// <param name="left">The first handle.</param>
    /// <param name="right">The second handle.</param>
    /// <returns><see langword="true"/> when equal.</returns>
    public static bool operator ==(XmlNode left, XmlNode right) => left.Equals(right);

    /// <summary>Determines whether two handles denote different nodes.</summary>
    /// <param name="left">The first handle.</param>
    /// <param name="right">The second handle.</param>
    /// <returns><see langword="true"/> when different.</returns>
    public static bool operator !=(XmlNode left, XmlNode right) => !left.Equals(right);

    private XmlNode Wrap(int index) => index < 0 ? default : new XmlNode(_tree!, index);
}
