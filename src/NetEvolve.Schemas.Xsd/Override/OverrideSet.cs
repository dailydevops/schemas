namespace NetEvolve.Schemas.Xsd.Override;

using System;
using System.Collections.Generic;
using System.Text;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.SchemaDocuments;

/// <summary>
/// The components an <see langword="override"/> element offers to the schema documents of its target set: the definitions and
/// declarations that replace a component of the same kind and name (Structures 1.1, 4.2.5 and F.2).
/// </summary>
/// <remarks>
/// <para>
/// A set holds syntax nodes of the overriding documents and never copies them. The identity of a set (<see cref="Key"/>) is
/// the identity of its components by content: components of one document with the same name, the same attributes, the
/// same namespaces behind the prefixes their values use, and the same children are one component, so two
/// <see langword="override"/> elements with equivalent children give one unit of the target (Structures 1.1, 4.2.5). A
/// schema document reached under the same set is the same unit, which ends cycles.
/// Where a target contains an <see langword="override"/> of its own, both sets merge and the outer one wins, with the
/// children of the target first and the unmatched children of the outer set after them (F.2, clause 4).
/// </para>
/// <para>
/// Limitations: components of different documents are never equivalent, since the defaults of their document may differ.
/// A node of the set keeps its ancestors in the overriding document, so the builders read its document-level defaults
/// from the unit (<c>SchemaUnit.Root</c>), which is the overridden document (#757).
/// </para>
/// </remarks>
internal sealed class OverrideSet
{
    private static readonly char[] Separators = [' ', '\t', '\r', '\n'];

    private readonly List<SchemaSyntaxNode> _children;

    private OverrideSet(List<SchemaSyntaxNode> children, string key)
    {
        _children = children;
        Key = key;
    }

    /// <summary>Gets the identity of the set, the sorted identities of its nodes.</summary>
    internal string Key { get; }

    /// <summary>Builds the set of an <see langword="override"/> element, merged with the set that applies to its document.</summary>
    /// <param name="element">The <see langword="override"/> element.</param>
    /// <param name="outer">The set that overrides the document of <paramref name="element"/>, <see langword="null"/> for none.</param>
    /// <param name="identities">The identities handed out to nodes, shared by all sets of one load.</param>
    /// <returns>The set, <see langword="null"/> when it holds no component.</returns>
    internal static OverrideSet? Create(
        SchemaSyntaxNode element,
        OverrideSet? outer,
        Dictionary<object, int> identities
    )
    {
        ArgumentNullException.ThrowIfNull(element);
        ArgumentNullException.ThrowIfNull(identities);

        var children = new List<SchemaSyntaxNode>();
        for (var i = 0; i < element.ChildCount; i++)
        {
            var child = element.GetChildNode(i);
            if (!IsComponent(child) || Find(children, child) is not null)
            {
                continue;
            }

            children.Add(outer?.Find(child) ?? child);
        }

        if (outer is not null)
        {
            foreach (var child in outer._children)
            {
                if (Find(children, child) is null)
                {
                    children.Add(child);
                }
            }
        }

        if (children.Count == 0)
        {
            return null;
        }

        var ids = new List<int>(children.Count);
        foreach (var child in children)
        {
            ids.Add(Identify(child, identities));
        }

        ids.Sort();
        return new OverrideSet(children, string.Join(",", ids));
    }

    /// <summary>Gives a component the identity of its content, so equivalent components of one document are one node of a key.</summary>
    private static int Identify(SchemaSyntaxNode node, Dictionary<object, int> identities)
    {
        var root = node;
        while (root.Parent is not null)
        {
            root = root.Parent;
        }

        if (!identities.TryGetValue(root, out var document))
        {
            document = identities.Count + 1;
            identities.Add(root, document);
        }

        var content = new StringBuilder();
        _ = content.Append(document).Append('|');
        Describe(node, content);
        var fingerprint = content.ToString();
        if (!identities.TryGetValue(fingerprint, out var id))
        {
            id = identities.Count + 1;
            identities.Add(fingerprint, id);
        }

        return id;
    }

    /// <summary>Writes the name, the attributes in name order, the namespaces their values use, and the children of a node.</summary>
    private static void Describe(SchemaSyntaxNode node, StringBuilder content)
    {
        _ = content.Append('{').Append(node.Name.NamespaceName).Append('}').Append(node.Name.LocalName).Append('(');
        var attributes = new List<XsdSyntaxAttribute>(node.AttributeCount);
        for (var i = 0; i < node.AttributeCount; i++)
        {
            attributes.Add(node.GetAttribute(i));
        }

        attributes.Sort(CompareByName);
        foreach (var attribute in attributes)
        {
            _ = content
                .Append('{')
                .Append(attribute.Name.NamespaceName)
                .Append('}')
                .Append(attribute.Name.LocalName)
                .Append('=')
                .Append(attribute.Value.Length)
                .Append(':')
                .Append(attribute.Value);
            foreach (var token in attribute.Value.Split(Separators, StringSplitOptions.RemoveEmptyEntries))
            {
                var colon = token.AsSpan().IndexOf(':');
                var prefix = colon < 0 ? string.Empty : token.Substring(0, colon);
                _ = content.Append('[').Append(node.TryResolvePrefix(prefix, out var uri) ? uri : "?").Append(']');
            }
        }

        _ = content.Append(')');
        for (var i = 0; i < node.ChildCount; i++)
        {
            Describe(node.GetChildNode(i), content);
        }

        _ = content.Append(';');
    }

    private static int CompareByName(XsdSyntaxAttribute x, XsdSyntaxAttribute y)
    {
        var space = string.CompareOrdinal(x.Name.NamespaceName, y.Name.NamespaceName);
        return space != 0 ? space : string.CompareOrdinal(x.Name.LocalName, y.Name.LocalName);
    }

    /// <summary>Gives the component that takes the place of a top-level component of a target.</summary>
    /// <param name="original">The component of the target.</param>
    /// <returns>The overriding component of the same kind and name, otherwise <paramref name="original"/>.</returns>
    internal SchemaSyntaxNode Replace(SchemaSyntaxNode original)
    {
        ArgumentNullException.ThrowIfNull(original);

        return Find(original) ?? original;
    }

    /// <summary>Tells whether overriding a document with this set changes nothing in the document itself (F.2, with the cycle note of 4.2.5); the documents it includes, redefines, or overrides are the business of the loader.</summary>
    /// <param name="schema">The <c>schema</c> element of the document.</param>
    /// <returns>
    /// <see langword="true"/> when no component, no component of a <c>redefine</c>, and no child of an
    /// <see langword="override"/> of the document is replaced or added.
    /// </returns>
    internal bool LeavesUnchanged(SchemaSyntaxNode schema)
    {
        ArgumentNullException.ThrowIfNull(schema);

        for (var i = 0; i < schema.ChildCount; i++)
        {
            var child = schema.GetChildNode(i);
            if (!string.Equals(child.Name.NamespaceName, SchemaGrammar.Namespace, StringComparison.Ordinal))
            {
                continue;
            }

            switch (child.Name.LocalName)
            {
                case "redefine":
                    if (ChangesChildren(child))
                    {
                        return false;
                    }

                    break;
                case "override":
                    if (!ContainsAll(child))
                    {
                        return false;
                    }

                    break;
                default:
                    if (IsComponent(child) && !ReferenceEquals(Replace(child), child))
                    {
                        return false;
                    }

                    break;
            }
        }

        return true;
    }

    private bool ChangesChildren(SchemaSyntaxNode parent)
    {
        for (var i = 0; i < parent.ChildCount; i++)
        {
            var child = parent.GetChildNode(i);
            if (IsComponent(child) && !ReferenceEquals(Replace(child), child))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Tells whether an <see langword="override"/> element of the target already holds every component of the set.</summary>
    private bool ContainsAll(SchemaSyntaxNode element)
    {
        var own = new List<SchemaSyntaxNode>(element.ChildCount);
        for (var i = 0; i < element.ChildCount; i++)
        {
            var child = element.GetChildNode(i);
            if (IsComponent(child))
            {
                own.Add(child);
            }
        }

        foreach (var child in _children)
        {
            if (!ReferenceEquals(Find(own, child), child))
            {
                return false;
            }
        }

        return true;
    }

    private SchemaSyntaxNode? Find(SchemaSyntaxNode original) => Find(_children, original);

    private static SchemaSyntaxNode? Find(List<SchemaSyntaxNode> nodes, SchemaSyntaxNode like)
    {
        if (!IsComponent(like))
        {
            return null;
        }

        var name = like.FindAttribute("name");
        foreach (var node in nodes)
        {
            if (
                string.Equals(node.Name.LocalName, like.Name.LocalName, StringComparison.Ordinal)
                && string.Equals(node.FindAttribute("name"), name, StringComparison.Ordinal)
            )
            {
                return node;
            }
        }

        return null;
    }

    private static bool IsComponent(SchemaSyntaxNode node) =>
        string.Equals(node.Name.NamespaceName, SchemaGrammar.Namespace, StringComparison.Ordinal)
        && node.FindAttribute("name") is not null
        && node.Name.LocalName
            is "simpleType"
                or "complexType"
                or "group"
                or "attributeGroup"
                or "element"
                or "attribute"
                or "notation";
}
