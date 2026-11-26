namespace NetEvolve.Schemas.Xsd.SchemaDocuments;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.Contracts;

/// <summary>An element of a schema document with its attributes, children, and namespace declarations.</summary>
/// <remarks>Text is not part of the model.</remarks>
internal sealed class SchemaSyntaxNode : ISchemaSyntaxElement
{
    // Most elements have few or no children and no namespace declarations; the lists exist only once they are needed, and the
    // attributes are one exactly sized array (the sink collects them and sets them at the end of the start tag).
    private static readonly List<SchemaSyntaxNode> NoChildren = [];
    private static readonly List<KeyValuePair<string, string>> NoNamespaces = [];

    private XsdSyntaxAttribute[] _attributes = [];
    private List<SchemaSyntaxNode>? _children;
    private List<KeyValuePair<string, string>>? _namespaces;

    internal SchemaSyntaxNode(XPathQName name, DocumentLocation location, SchemaSyntaxNode? parent)
    {
        Name = name;
        Location = location;
        Parent = parent;
        parent?.AddChild(this);
    }

    /// <inheritdoc />
    public XPathQName Name { get; }

    /// <inheritdoc />
    public DocumentLocation Location { get; }

    /// <inheritdoc />
    public int AttributeCount => _attributes.Length;

    /// <inheritdoc />
    public int ChildCount => _children?.Count ?? 0;

    /// <summary>Gets the parent element, <see langword="null"/> for the root.</summary>
    internal SchemaSyntaxNode? Parent { get; }

    /// <inheritdoc />
    public XsdSyntaxAttribute GetAttribute(int index) => _attributes[index];

    /// <inheritdoc />
    public ISchemaSyntaxElement GetChild(int index) => (_children ?? NoChildren)[index];

    /// <summary>Gets a child element.</summary>
    /// <param name="index">The zero-based index.</param>
    /// <returns>The child.</returns>
    internal SchemaSyntaxNode GetChildNode(int index) => (_children ?? NoChildren)[index];

    /// <summary>Finds an attribute that has no namespace.</summary>
    /// <param name="localName">The local name.</param>
    /// <returns>The value, <see langword="null"/> when absent.</returns>
    internal string? FindAttribute(string localName)
    {
        for (var i = 0; i < _attributes.Length; i++)
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
        var children = _children ?? NoChildren;
        var count = 0;
        for (var i = 0; i < children.Count; i++)
        {
            if (string.Equals(children[i].Name.LocalName, localName, StringComparison.Ordinal))
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
            var namespaces = node._namespaces ?? NoNamespaces;
            for (var i = namespaces.Count - 1; i >= 0; i--)
            {
                if (string.Equals(namespaces[i].Key, prefix, StringComparison.Ordinal))
                {
                    namespaceUri = namespaces[i].Value;
                    return true;
                }
            }
        }

        namespaceUri = string.Empty;
        return false;
    }

    /// <summary>Sets the attributes of the start tag; the array is sized exactly.</summary>
    /// <param name="attributes">The attributes in document order.</param>
    internal void SetAttributes(List<XsdSyntaxAttribute> attributes) =>
        _attributes = attributes.Count == 0 ? [] : attributes.ToArray();

    /// <summary>Replaces the value of an attribute that has no namespace, keeping its location; composition uses it for the redefine rewrite.</summary>
    /// <param name="localName">The local name.</param>
    /// <param name="value">The new value.</param>
    internal void SetAttribute(string localName, string value)
    {
        for (var i = 0; i < _attributes.Length; i++)
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
        (_namespaces ??= []).Add(new KeyValuePair<string, string>(prefix, uri));

    private void AddChild(SchemaSyntaxNode child) => (_children ??= []).Add(child);
}
