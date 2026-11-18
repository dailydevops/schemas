namespace NetEvolve.Schemas.XPath.Functions.Sequences;

using System;
using NetEvolve.Arguments;
using NetEvolve.Schemas.XPath.Xdm.Nodes;

/// <summary>
/// The comparison of two nodes by <c>fn:deep-equal</c> (Functions and Operators 1.0, 15.3.1): nodes are equal when they have the same
/// kind and, by kind, the same name and value, the same attributes in any order, and equal children in order. Comments and processing
/// instructions among the children are not looked at; a comment or processing instruction that is itself compared is equal to another
/// of the same value (and target).
/// </summary>
/// <remarks>
/// <para>
/// The walk is iterative: it goes down both trees in step and holds no stack, so the depth of a tree costs nothing. Text and
/// attribute values are compared as spans when the tree offers them, so an equal pair allocates nothing.
/// </para>
/// <para>
/// Limitations: the data model has no typed values yet (DEF-40, #52), so an element and an attribute are compared by the characters of
/// their content, as <c>xs:untypedAtomic</c> compares them, and namespace nodes are not exposed (#54), so the in-scope namespaces of
/// two elements are not compared, which the specification leaves out too. Two elements whose children differ only in how adjacent
/// text is split into text nodes are different, as the data model never splits it.
/// </para>
/// </remarks>
internal static class NodeEquality
{
    /// <summary>Determines whether two nodes are deep-equal.</summary>
    /// <param name="left">The first node.</param>
    /// <param name="right">The second node.</param>
    /// <returns><see langword="true"/> when the nodes are equal.</returns>
    /// <exception cref="ArgumentNullException">A node is <see langword="default"/>.</exception>
    public static bool DeepEqual(in XPathNode left, in XPathNode right)
    {
        Argument.ThrowIfNull(left.Tree);
        Argument.ThrowIfNull(right.Tree);

        if (left.IsSameNode(in right))
        {
            return true;
        }

        if (!SelfEquals(in left, in right))
        {
            return false;
        }

        var a = left;
        var b = right;
        while (true)
        {
            // Down: the first children that count must both exist or both be missing.
            var childA = FirstChild(in a);
            var childB = FirstChild(in b);
            if (!PairEquals(in childA, in childB))
            {
                return false;
            }

            if (!childA.IsEmpty())
            {
                a = childA;
                b = childB;
                continue;
            }

            // Across and up: the next pair, or the end of the walk, or a difference.
            switch (MoveToNextPair(in left, ref a, ref b))
            {
                case Walk.Finished:
                    return true;
                case Walk.Different:
                    return false;
                default:
                    break;
            }
        }
    }

    // The next sibling pair, or the parents once there is none, until the pair that started the walk.
    private static Walk MoveToNextPair(in XPathNode root, ref XPathNode a, ref XPathNode b)
    {
        while (true)
        {
            if (a.IsSameNode(in root))
            {
                return Walk.Finished;
            }

            var nextA = FirstSignificant(a.NextSibling());
            var nextB = FirstSignificant(b.NextSibling());
            if (!PairEquals(in nextA, in nextB))
            {
                return Walk.Different;
            }

            if (!nextA.IsEmpty())
            {
                a = nextA;
                b = nextB;
                return Walk.Moved;
            }

            a = a.Parent();
            b = b.Parent();
        }
    }

    private static XPathNode FirstChild(in XPathNode node) =>
        node.Kind is XdmNodeKind.Document or XdmNodeKind.Element ? FirstSignificant(node.FirstChild()) : default;

    // Both nodes are missing, or both exist and are equal as themselves.
    private enum Walk
    {
        Moved,
        Finished,
        Different,
    }

    private static bool PairEquals(in XPathNode a, in XPathNode b) =>
        a.IsEmpty() == b.IsEmpty() && (a.IsEmpty() || SelfEquals(in a, in b));

    // The nodes themselves, without their children: kind, name, value, and for an element its attributes.
    private static bool SelfEquals(in XPathNode a, in XPathNode b)
    {
        if (a.Kind != b.Kind)
        {
            return false;
        }

        switch (a.Kind)
        {
            case XdmNodeKind.Document:
                return true;
            case XdmNodeKind.Element:
                return NameEquals(in a, in b) && AttributesEqual(in a, in b);
            case XdmNodeKind.Text:
            case XdmNodeKind.Comment:
                return ValueEquals(in a, in b);
            default:
                // Attributes, processing instructions, and namespace nodes: a name and a value.
                return NameEquals(in a, in b) && ValueEquals(in a, in b);
        }
    }

    private static bool IsSignificant(in XPathNode node) =>
        node.Kind is not (XdmNodeKind.Comment or XdmNodeKind.ProcessingInstruction);

    private static XPathNode FirstSignificant(XPathNode node)
    {
        while (!node.IsEmpty() && !IsSignificant(in node))
        {
            node = node.NextSibling();
        }

        return node;
    }

    private static bool NameEquals(in XPathNode a, in XPathNode b)
    {
        var x = a.Name();
        var y = b.Name();
        var namesA = a.Tree!.Names;
        var namesB = b.Tree!.Names;

        // Handles compare only within one name table; trees of two documents compare by text.
        return ReferenceEquals(namesA, namesB)
            ? x.Local == y.Local && x.NamespaceUri == y.NamespaceUri
            : string.Equals(namesA.GetString(x.Local), namesB.GetString(y.Local), StringComparison.Ordinal)
                && string.Equals(
                    namesA.GetString(x.NamespaceUri),
                    namesB.GetString(y.NamespaceUri),
                    StringComparison.Ordinal
                );
    }

    private static bool ValueEquals(in XPathNode a, in XPathNode b) =>
        a.TryGetStringValueSpan(out var x) && b.TryGetStringValueSpan(out var y)
            ? x.SequenceEqual(y)
            : string.Equals(a.StringValue(), b.StringValue(), StringComparison.Ordinal);

    // The same number of attributes, and each attribute of the first has one of the same name and value in the second.
    private static bool AttributesEqual(in XPathNode a, in XPathNode b)
    {
        var countA = 0;
        var countB = 0;
        for (var attribute = a.FirstAttribute(); !attribute.IsEmpty(); attribute = attribute.NextAttribute())
        {
            countA++;
        }

        for (var attribute = b.FirstAttribute(); !attribute.IsEmpty(); attribute = attribute.NextAttribute())
        {
            countB++;
        }

        if (countA != countB)
        {
            return false;
        }

        for (var attribute = a.FirstAttribute(); !attribute.IsEmpty(); attribute = attribute.NextAttribute())
        {
            if (!HasMatch(in attribute, in b))
            {
                return false;
            }
        }

        return true;
    }

    private static bool HasMatch(in XPathNode attribute, in XPathNode element)
    {
        for (var other = element.FirstAttribute(); !other.IsEmpty(); other = other.NextAttribute())
        {
            if (NameEquals(in attribute, in other))
            {
                return ValueEquals(in attribute, in other);
            }
        }

        return false;
    }
}
