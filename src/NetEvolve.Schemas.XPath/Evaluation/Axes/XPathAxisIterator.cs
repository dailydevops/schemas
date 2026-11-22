namespace NetEvolve.Schemas.XPath.Evaluation.Axes;

using System.Runtime.InteropServices;
using NetEvolve.Arguments;
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
        while (Advance())
        {
            if (_matcher.Matches(_current))
            {
                return true;
            }
        }

        return false;
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
