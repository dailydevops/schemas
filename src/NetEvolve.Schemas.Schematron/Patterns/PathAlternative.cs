namespace NetEvolve.Schemas.Schematron.Patterns;

using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Xslt.Construction;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Xdm.Nodes;

/// <summary>
/// A path alternative of a union pattern: the steps, their anchor, and the default priority (XSLT 2.0 section 6.4). With a
/// key start (<c>key('name', 'value')</c>, XSLT 2.0 section 5.5) the anchor is the node set of that call: the first step must
/// be a child (or, after <c>//</c>, a descendant) of one of its nodes, and without steps the node itself must be one. With a
/// parenthesized prefix pattern (#1285) the parent of the first step, or after <c>//</c> one of its ancestors, must match it.
/// </summary>
internal sealed class PathAlternative : PatternAlternative
{
    private readonly PathRoot _root;
    private readonly PatternStep[] _steps;
    private readonly QueryExpression? _keyStart;
    private readonly PatternAlternative? _prefix;

    internal PathAlternative(
        PathRoot root,
        PatternStep[] steps,
        decimal priority,
        QueryExpression? keyStart = null,
        PatternAlternative? prefix = null
    )
        : base(priority)
    {
        _prefix = prefix;
        _root = root;
        _steps = steps;
        _keyStart = keyStart;
    }

    /// <summary>Matches right to left: the last step tests the candidate, each step before it an ancestor.</summary>
    internal override QueryStatus Matches(IQueryRun run, in XPathNode node, out bool matched)
    {
        if (_steps.Length == 0)
        {
            if (_keyStart is not null)
            {
                return KeyAnchored(run, in node, node, false, out matched);
            }

            matched = node.Kind == XdmNodeKind.Document;
            return QueryStatus.Ok;
        }

        return MatchFrom(run, _steps.Length - 1, in node, out matched);
    }

    private QueryStatus MatchFrom(IQueryRun run, int index, in XPathNode node, out bool matched)
    {
        var step = _steps[index];
        var status = step.Matches(run, in node, out matched);
        if (status != QueryStatus.Ok || !matched)
        {
            matched = false;
            return status;
        }

        matched = false;
        if (index == 0)
        {
            if (_keyStart is not null)
            {
                return KeyAnchored(run, in node, node.Parent(), step.DescendantBefore, out matched);
            }

            if (_prefix is not null)
            {
                return PrefixAnchored(run, node.Parent(), step.DescendantBefore, out matched);
            }

            matched = Anchored(in node);
            return QueryStatus.Ok;
        }

        var ancestor = node.Parent();
        if (!step.DescendantBefore)
        {
            return ancestor.IsEmpty() ? QueryStatus.Ok : MatchFrom(run, index - 1, in ancestor, out matched);
        }

        for (; !ancestor.IsEmpty(); ancestor = ancestor.Parent())
        {
            status = MatchFrom(run, index - 1, in ancestor, out matched);
            if (status != QueryStatus.Ok || matched)
            {
                return status;
            }
        }

        return QueryStatus.Ok;
    }

    // The first step matched: its parent, or after "//" any ancestor, must match the parenthesized prefix pattern.
    private QueryStatus PrefixAnchored(IQueryRun run, XPathNode candidate, bool ancestors, out bool matched)
    {
        matched = false;
        for (; !candidate.IsEmpty(); candidate = candidate.Parent())
        {
            var status = _prefix!.Matches(run, in candidate, out matched);
            if (status != QueryStatus.Ok || matched || !ancestors)
            {
                return status;
            }
        }

        return QueryStatus.Ok;
    }

    // The first step matched: a leading "/" needs the document node as parent, "//" a document node as root.
    private bool Anchored(in XPathNode first)
    {
        switch (_root)
        {
            case PathRoot.Root:
                var parent = first.Parent();
                return !parent.IsEmpty() && parent.Kind == XdmNodeKind.Document;
            case PathRoot.RootDescendants:
                var top = first;
                for (var ancestor = first.Parent(); !ancestor.IsEmpty(); ancestor = ancestor.Parent())
                {
                    top = ancestor;
                }

                return top.Kind == XdmNodeKind.Document;
            default:
                return true;
        }
    }

    // Evaluates the key call with the matched node as the focus: the document of that node is searched. The candidate is
    // the node itself (no steps), its parent, or, after "//", any of its ancestors.
    private QueryStatus KeyAnchored(
        IQueryRun run,
        in XPathNode focusNode,
        XPathNode candidate,
        bool ancestors,
        out bool matched
    )
    {
        matched = false;
        var sink = new ItemSequenceSink(focusNode.Tree!.Names);
        var status = run.TryEvaluate(_keyStart!, new QueryFocus(focusNode, 1, 1), sink);
        if (status != QueryStatus.Ok)
        {
            return status;
        }

        var nodes = sink.Result;
        for (; !candidate.IsEmpty(); candidate = candidate.Parent())
        {
            for (var i = 0; i < nodes.Count; i++)
            {
                if (nodes[i].TryGetNode(out var found) && found == candidate)
                {
                    matched = true;
                    return QueryStatus.Ok;
                }
            }

            if (!ancestors)
            {
                break;
            }
        }

        return QueryStatus.Ok;
    }
}
