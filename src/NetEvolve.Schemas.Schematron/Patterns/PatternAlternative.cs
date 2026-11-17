namespace NetEvolve.Schemas.Schematron.Patterns;

using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Xdm.Nodes;

/// <summary>One alternative of a union pattern: the steps, their anchor, and the default priority (XSLT 2.0 section 6.4).</summary>
internal sealed class PatternAlternative
{
    private readonly PathRoot _root;
    private readonly PatternStep[] _steps;

    internal PatternAlternative(PathRoot root, PatternStep[] steps, decimal priority)
    {
        _root = root;
        _steps = steps;
        Priority = priority;
    }

    /// <summary>Gets the default priority of the alternative.</summary>
    internal decimal Priority { get; }

    /// <summary>Matches right to left: the last step tests the candidate, each step before it an ancestor.</summary>
    internal QueryStatus Matches(IQueryRun run, in XPathNode node, out bool matched)
    {
        if (_steps.Length == 0)
        {
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
}
