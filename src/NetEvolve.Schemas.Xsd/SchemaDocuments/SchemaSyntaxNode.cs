namespace NetEvolve.Schemas.Xsd.SchemaDocuments;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.Contracts;

/// <summary>An element of a schema document with its attributes, children, and namespace declarations.</summary>
/// <remarks>
/// Limitation: an attribute carries the location of the start tag of its element, because the reader contract reports no
/// attribute position (design #108 AP9). Text is not part of the model.
/// </remarks>
internal sealed class SchemaSyntaxNode : ISchemaSyntaxElement
{
    private readonly List<XsdSyntaxAttribute> _attributes = [];
    private readonly List<SchemaSyntaxNode> _children = [];
    private readonly List<KeyValuePair<string, string>> _namespaces = [];

    internal SchemaSyntaxNode(XPathQName name, DocumentLocation location, SchemaSyntaxNode? parent)
    {
        Name = name;
        Location = location;
        Parent = parent;
        parent?._children.Add(this);
    }

    /// <inheritdoc />
    public XPathQName Name { get; }

    /// <inheritdoc />
    public DocumentLocation Location { get; }

    /// <inheritdoc />
    public int AttributeCount => _attributes.Count;

    /// <inheritdoc />
    public int ChildCount => _children.Count;

    /// <summary>Gets the parent element, <see langword="null"/> for the root.</summary>
    internal SchemaSyntaxNode? Parent { get; }

    /// <inheritdoc />
    public XsdSyntaxAttribute GetAttribute(int index) => _attributes[index];

    /// <inheritdoc />
    public ISchemaSyntaxElement GetChild(int index) => _children[index];

    /// <summary>Gets a child element.</summary>
    /// <param name="index">The zero-based index.</param>
    /// <returns>The child.</returns>
    internal SchemaSyntaxNode GetChildNode(int index) => _children[index];

    /// <summary>Finds an attribute that has no namespace.</summary>
    /// <param name="localName">The local name.</param>
    /// <returns>The value, <see langword="null"/> when absent.</returns>
    internal string? FindAttribute(string localName)
    {
        for (var i = 0; i < _attributes.Count; i++)
        {
            var name = _attributes[i].Name;
            if (name.NamespaceName.Length == 0 && string.Equals(name.LocalName, localName, StringComparison.Ordinal))
            {
                return _attributes[i].Value;
            }
        }

        return null;
    }

    /// <summary>Counts the children with a local name.</summary>
    /// <param name="localName">The local name.</param>
    /// <returns>The number of children.</returns>
    internal int CountChildren(string localName)
    {
        var count = 0;
        for (var i = 0; i < _children.Count; i++)
        {
            if (string.Equals(_children[i].Name.LocalName, localName, StringComparison.Ordinal))
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>Resolves a prefix through the declarations of this element and its ancestors.</summary>
    /// <param name="prefix">The prefix, empty for the default namespace.</param>
    /// <param name="namespaceUri">Receives the namespace URI.</param>
    /// <returns><see langword="true"/> when the prefix is declared.</returns>
    internal bool TryResolvePrefix(string prefix, out string namespaceUri)
    {
        for (var node = this; node is not null; node = node.Parent)
        {
            for (var i = node._namespaces.Count - 1; i >= 0; i--)
            {
                if (string.Equals(node._namespaces[i].Key, prefix, StringComparison.Ordinal))
                {
                    namespaceUri = node._namespaces[i].Value;
                    return true;
                }
            }
        }

        namespaceUri = string.Empty;
        return false;
    }

    internal void AddAttribute(in XsdSyntaxAttribute attribute) => _attributes.Add(attribute);

    /// <summary>Replaces the value of an attribute that has no namespace, keeping its location; composition uses it for the redefine rewrite.</summary>
    /// <param name="localName">The local name.</param>
    /// <param name="value">The new value.</param>
    internal void SetAttribute(string localName, string value)
    {
        for (var i = 0; i < _attributes.Count; i++)
        {
            var attribute = _attributes[i];
            if (
                attribute.Name.NamespaceName.Length == 0
                && string.Equals(attribute.Name.LocalName, localName, StringComparison.Ordinal)
            )
            {
                _attributes[i] = new XsdSyntaxAttribute(attribute.Name, value, attribute.Location);
                return;
            }
        }
    }

    internal void AddNamespace(string prefix, string uri) =>
        _namespaces.Add(new KeyValuePair<string, string>(prefix, uri));
}
