namespace NetEvolve.Schemas.Xsd.Override;

using System;
using System.Collections.Generic;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xsd.SchemaDocuments;

/// <summary>
/// The components an <see langword="override"/> element offers to the schema documents of its target set: the definitions and
/// declarations that replace a component of the same kind and name (Structures 1.1, 4.2.5 and F.2).
/// </summary>
/// <remarks>
/// <para>
/// A set holds syntax nodes of the overriding documents and never copies them, so the identity of a set is the identity of
/// its nodes (<see cref="Key"/>); a schema document reached under the same set is the same unit, which ends cycles.
/// Where a target contains an <see langword="override"/> of its own, both sets merge and the outer one wins, with the
/// children of the target first and the unmatched children of the outer set after them (F.2, clause 4).
/// </para>
/// <para>
/// Limitations: the same document overridden by two <see langword="override"/> elements with equivalent children is two units,
/// not one, because equivalence is not compared deeply; the specification calls such overrides allowed. A target that includes
/// another document is never treated as left unchanged by a set, since its <see langword="include"/> becomes an
/// <see langword="override"/> (F.2, clause 3), even when nothing below it matches.
/// </para>
/// </remarks>
internal sealed class OverrideSet
{
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
        Dictionary<SchemaSyntaxNode, int> identities
    )
    {
        Argument.ThrowIfNull(element);
        Argument.ThrowIfNull(identities);

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
            if (!identities.TryGetValue(child, out var id))
            {
                id = identities.Count + 1;
                identities.Add(child, id);
            }

            ids.Add(id);
        }

        ids.Sort();
        return new OverrideSet(children, string.Join(",", ids));
    }

    /// <summary>Gives the component that takes the place of a top-level component of a target.</summary>
    /// <param name="original">The component of the target.</param>
    /// <returns>The overriding component of the same kind and name, otherwise <paramref name="original"/>.</returns>
    internal SchemaSyntaxNode Replace(SchemaSyntaxNode original)
    {
        Argument.ThrowIfNull(original);

        return Find(original) ?? original;
    }

    /// <summary>Tells whether overriding a document with this set changes nothing in it (F.2, with the cycle note of 4.2.5).</summary>
    /// <param name="schema">The <c>schema</c> element of the document.</param>
    /// <returns>
    /// <see langword="true"/> when no component, no component of a <c>redefine</c>, and no child of an
    /// <see langword="override"/> of the document is replaced or added, and the document includes nothing.
    /// </returns>
    internal bool LeavesUnchanged(SchemaSyntaxNode schema)
    {
        Argument.ThrowIfNull(schema);

        for (var i = 0; i < schema.ChildCount; i++)
        {
            var child = schema.GetChildNode(i);
            if (!string.Equals(child.Name.NamespaceName, SchemaGrammar.Namespace, StringComparison.Ordinal))
            {
                continue;
            }

            switch (child.Name.LocalName)
            {
                case "include":
                    return false;
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
