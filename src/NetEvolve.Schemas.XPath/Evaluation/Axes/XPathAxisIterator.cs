namespace NetEvolve.Schemas.XPath.Evaluation.Axes;

using System.Runtime.InteropServices;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Model;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Xdm.Nodes;

/// <summary>
/// Walks one axis from a context node and yields the nodes that pass a node test, in the order the specification requires for
/// positions: document order for forward axes, reverse document order (nearest first) for reverse axes. The iterator is a struct that
/// keeps all state in fields, so a walk allocates nothing.
/// </summary>
/// <remarks>
/// Use it with <see langword="foreach"/> or <see cref="MoveNext"/>; copying it copies the position. Limitations: the namespace axis raises XPST0010
/// (#54), and because the document model links siblings forward only, finding the previous sibling scans the siblings from the first
/// one, which makes the preceding-sibling and preceding axes quadratic in the number of siblings of a walked level.
/// </remarks>
[StructLayout(LayoutKind.Auto)]
internal struct XPathAxisIterator
{
    private readonly XPathNode _context;
    private readonly Axis _axis;
    private readonly XPathNodeMatcher _matcher;
    private XPathNode _current;
    private XPathNode _sibling;
    private XPathNode _subtreeRoot;
    private bool _started;
    private bool _inSubtree;

    // The tree and the rows of a walk over a document tree that reads the arrays of the tree instead of making a node per hop; null for
    // every other tree and axis.
    private readonly XmlDocumentTree? _rows;
    private int _row;

    /// <summary>Initializes a new instance of the <see cref="XPathAxisIterator"/> struct.</summary>
    /// <param name="context">The context node.</param>
    /// <param name="axis">The axis.</param>
    /// <param name="matcher">The node test, bound to the name table of the tree of <paramref name="context"/>.</param>
    /// <exception cref="XPathException"><paramref name="axis"/> is the namespace axis (XPST0010).</exception>
    public XPathAxisIterator(in XPathNode context, Axis axis, in XPathNodeMatcher matcher)
    {
        _ = XPathAxes.GetProperties(axis);
        ArgumentNullException.ThrowIfNull(context.Tree);

        _context = context;
        _axis = axis;
        _matcher = matcher;
        _current = default;
        _sibling = default;
        _subtreeRoot = default;
        _started = false;
        _inSubtree = false;
        _rows =
            axis is Axis.Child or Axis.Attribute or Axis.Descendant or Axis.DescendantOrSelf
            && context.Node.Tree is XmlDocumentTree tree
                ? tree
                : null;
        _row = context.Node.Index;
    }

    /// <summary>Gets the current node.</summary>
    public readonly XPathNode Current => _current;

    /// <summary>Returns the iterator itself so that it can be used with <see langword="foreach"/>.</summary>
    /// <returns>A copy of this iterator.</returns>
    public readonly XPathAxisIterator GetEnumerator() => this;

    /// <summary>Moves to the next node that passes the node test.</summary>
    /// <returns><see langword="true"/> when there is one.</returns>
    public bool MoveNext()
    {
        if (_rows is not null)
        {
            return MoveNextRow(_rows);
        }

        while (Advance())
        {
            if (_matcher.Matches(_current))
            {
                return true;
            }
        }

        return false;
    }

    // The walks that read the rows of a document tree in document order. A row is a node of the tree: the descendants of the context
    // node are the rows after it whose parent is not before it, because a subtree is a run of rows and the row that follows the run has a
    // parent before the context; attributes are rows too and belong to the axis of the attributes only. A context node that cannot
    // have such nodes ends the walk at once.
    private bool MoveNextRow(XmlDocumentTree tree)
    {
        var context = _context.Node.Index;
        var contextKind = tree.GetKind(context);
        var first = !_started;
        _started = true;
        var row = _row;
        if (!first && row >= tree.Count)
        {
            return false;
        }

        if (_axis == Axis.Attribute)
        {
            row = first
                ? (contextKind == XmlNodeKind.Element ? tree.GetFirstAttribute(context) : XmlTree.NoNode)
                : tree.GetNextAttribute(row);
            while (row != XmlTree.NoNode && !_matcher.Matches(tree, row))
            {
                row = tree.GetNextAttribute(row);
            }
        }
        else if (_axis == Axis.Child)
        {
            row = first
                ? (
                    contextKind is XmlNodeKind.Document or XmlNodeKind.Element
                        ? tree.GetFirstChild(context)
                        : XmlTree.NoNode
                )
                : tree.GetNextSibling(row);
            while (row != XmlTree.NoNode && !_matcher.Matches(tree, row))
            {
                row = tree.GetNextSibling(row);
            }
        }
        else if (first && _axis == Axis.DescendantOrSelf && _matcher.Matches(tree, context))
        {
            row = context;
        }
        else
        {
            var count = tree.Count;
            row = first ? context + 1 : row + 1;
            if (contextKind is not (XmlNodeKind.Document or XmlNodeKind.Element))
            {
                row = count;
            }

            for (; row < count; row++)
            {
                if (tree.GetParent(row) < context)
                {
                    row = count;
                    break;
                }

                if (
                    tree.GetKind(row) is not (XmlNodeKind.Attribute or XmlNodeKind.Namespace)
                    && _matcher.Matches(tree, row)
                )
                {
                    break;
                }
            }

            if (row >= count)
            {
                row = XmlTree.NoNode;
            }
        }

        _row = row == XmlTree.NoNode ? tree.Count : row;
        _current = row == XmlTree.NoNode ? default : new XPathNode(new XmlNode(tree, row));
        return row != XmlTree.NoNode;
    }

    private bool Advance()
    {
        var first = !_started;
        _started = true;

        switch (_axis)
        {
            case Axis.Self:
                return first && Set(_context);
            case Axis.Child:
                return Set(
                    first
                        ? (
                            _context.Kind is XdmNodeKind.Document or XdmNodeKind.Element
                                ? _context.FirstChild()
                                : default
                        )
                        : _current.NextSibling()
                );
            case Axis.Attribute:
                return Set(
                    first
                        ? (_context.Kind == XdmNodeKind.Element ? _context.FirstAttribute() : default)
                        : _current.NextAttribute()
                );
            case Axis.Parent:
                return first && Set(_context.Parent());
            case Axis.Ancestor:
                return Set(first ? _context.Parent() : _current.Parent());
            case Axis.AncestorOrSelf:
                return Set(first ? _context : _current.Parent());
            case Axis.FollowingSibling:
                return Set(
                    first
                        ? (_context.Kind == XdmNodeKind.Attribute ? default : _context.NextSibling())
                        : _current.NextSibling()
                );
            case Axis.PrecedingSibling:
                return Set(
                    first
                        ? (_context.Kind == XdmNodeKind.Attribute ? default : PreviousSibling(_context))
                        : PreviousSibling(_current)
                );
            case Axis.Descendant:
            case Axis.DescendantOrSelf:
                return AdvanceDescendant(first);
            case Axis.Following:
                return AdvanceFollowing(first);
            case Axis.Preceding:
                return AdvancePreceding(first);
            default:
                return false;
        }
    }

    private bool Set(in XPathNode node)
    {
        _current = node;
        return !node.IsEmpty();
    }

    // Preorder below the context; the climb stops at the context, which also ends the walk of a context without children.
    private bool AdvanceDescendant(bool first)
    {
        if (first)
        {
            if (_axis == Axis.DescendantOrSelf)
            {
                return Set(_context);
            }

            return Set(_context.FirstChild());
        }

        var child = _current.FirstChild();
        if (!child.IsEmpty())
        {
            return Set(child);
        }

        var node = _current;
        while (node != _context)
        {
            var sibling = node.NextSibling();
            if (!sibling.IsEmpty())
            {
                return Set(sibling);
            }

            node = node.Parent();
            if (node.IsEmpty())
            {
                break;
            }
        }

        return Set(default);
    }

    // Everything after the context in document order that is not a descendant; an attribute is followed by the content of its element.
    private bool AdvanceFollowing(bool first)
    {
        XPathNode next;
        if (!first)
        {
            next = _current.FirstChild();
            return Set(next.IsEmpty() ? SkipSubtree(_current) : next);
        }

        if (_context.Kind != XdmNodeKind.Attribute)
        {
            return Set(SkipSubtree(_context));
        }

        var owner = _context.Parent();
        next = owner.FirstChild();
        return Set(next.IsEmpty() ? SkipSubtree(owner) : next);
    }

    // Reverse document order without ancestors: for each ancestor-or-self, its previous siblings nearest first, each with its
    // subtree in reverse preorder (the deepest last descendant first, the sibling itself last). An attribute precedes like its element.
    private bool AdvancePreceding(bool first)
    {
        if (first)
        {
            _sibling = _context.Kind == XdmNodeKind.Attribute ? _context.Parent() : _context;
        }
        else if (_inSubtree)
        {
            if (_current != _subtreeRoot)
            {
                var previous = PreviousSibling(_current);
                return Set(previous.IsEmpty() ? _current.Parent() : LastDescendantOrSelf(previous));
            }

            _inSubtree = false;
            _sibling = _subtreeRoot;
        }

        while (!_sibling.IsEmpty())
        {
            var previous = PreviousSibling(_sibling);
            if (!previous.IsEmpty())
            {
                _subtreeRoot = previous;
                _inSubtree = true;
                return Set(LastDescendantOrSelf(previous));
            }

            _sibling = _sibling.Parent();
        }

        return Set(default);
    }

    private static XPathNode SkipSubtree(XPathNode node)
    {
        while (!node.IsEmpty())
        {
            var sibling = node.NextSibling();
            if (!sibling.IsEmpty())
            {
                return sibling;
            }

            node = node.Parent();
        }

        return default;
    }

    // ponytail: the document model has no previous-sibling link, so this scans from the first child; an accessor in the Xml package would make it O(1).
    private static XPathNode PreviousSibling(in XPathNode node)
    {
        var parent = node.Parent();
        if (parent.IsEmpty())
        {
            return default;
        }

        var candidate = parent.FirstChild();
        while (!candidate.IsEmpty())
        {
            var next = candidate.NextSibling();
            if (next == node)
            {
                return candidate;
            }

            candidate = next;
        }

        return default;
    }

    private static XPathNode LastDescendantOrSelf(XPathNode node)
    {
        while (true)
        {
            var last = node.FirstChild();
            if (last.IsEmpty())
            {
                return node;
            }

            for (var next = last.NextSibling(); !next.IsEmpty(); next = last.NextSibling())
            {
                last = next;
            }

            node = last;
        }
    }
}
