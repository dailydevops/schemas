namespace NetEvolve.Schemas.Schematron.Patterns;

using System;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.XPath.Evaluation.Axes;
using NetEvolve.Schemas.XPath.Xdm.Nodes;

/// <summary>
/// One step of a pattern alternative: an axis (child or attribute), a node test, and predicates. A predicate that reads
/// the position or size of its step is positional and sees the node among the siblings the step selects from the parent.
/// </summary>
internal sealed class PatternStep
{
    private readonly XPathNodeTest _test;
    private readonly bool _attributeAxis;
    private readonly bool _documentTest;
    private readonly QueryExpression[] _predicates;
    private readonly bool[] _usesLast;
    private readonly bool _positional;

    internal PatternStep(
        XPathNodeTest test,
        bool attributeAxis,
        bool documentTest,
        bool descendantBefore,
        QueryExpression[] predicates,
        bool[] positional,
        bool[] usesLast
    )
    {
        _test = test;
        _attributeAxis = attributeAxis;
        _documentTest = documentTest;
        DescendantBefore = descendantBefore;
        _predicates = predicates;
        _usesLast = usesLast;
        _positional = Array.IndexOf(positional, true) >= 0;
    }

    /// <summary>Gets a value indicating whether the step is reached through <c>//</c> from the step before it.</summary>
    internal bool DescendantBefore { get; }

    /// <summary>Determines whether the node is selected by the axis and the node test, without the predicates.</summary>
    private bool Selects(in XPathNodeMatcher matcher, in XPathNode node)
    {
        var kind = node.Kind;
        if (_attributeAxis)
        {
            if (kind != XdmNodeKind.Attribute)
            {
                return false;
            }
        }
        else if (
            kind is XdmNodeKind.Attribute or XdmNodeKind.Namespace
            || (kind == XdmNodeKind.Document && !_documentTest)
        )
        {
            return false;
        }

        return matcher.Matches(in node);
    }

    /// <summary>Determines whether the node passes the axis, the node test, and every predicate.</summary>
    internal QueryStatus Matches(IQueryRun run, in XPathNode node, out bool matched)
    {
        matched = false;
        var matcher = _test.Bind(node.Tree!.Names);
        if (!Selects(in matcher, in node))
        {
            return QueryStatus.Ok;
        }

        if (_predicates.Length == 0)
        {
            matched = true;
            return QueryStatus.Ok;
        }

        return _positional
            ? MatchesPositional(run, in matcher, in node, out matched)
            : MatchesPlain(run, in node, out matched);
    }

    private QueryStatus MatchesPlain(IQueryRun run, in XPathNode node, out bool matched)
    {
        matched = false;
        var focus = new QueryFocus(node, 1, -1);
        for (var i = 0; i < _predicates.Length; i++)
        {
            var status = run.TryEvaluateBoolean(_predicates[i], in focus, out var value);
            if (status != QueryStatus.Ok || !value)
            {
                return status;
            }
        }

        matched = true;
        return QueryStatus.Ok;
    }

    private QueryStatus MatchesPositional(
        IQueryRun run,
        in XPathNodeMatcher matcher,
        in XPathNode node,
        out bool matched
    )
    {
        matched = false;
        var parent = node.Parent();
        var count = _predicates.Length;
        Span<int> sizes = stackalloc int[count];
        Span<int> counters = stackalloc int[count];
        sizes.Fill(-1);

        // The size at level k is the number of siblings that pass the node test and the predicates before k.
        for (var level = 0; level < count; level++)
        {
            if (!_usesLast[level])
            {
                continue;
            }

            counters.Clear();
            var sizeStatus = Walk(
                run,
                in matcher,
                in parent,
                in node,
                false,
                level,
                counters,
                sizes,
                out var reached,
                out _
            );
            if (sizeStatus != QueryStatus.Ok)
            {
                return sizeStatus;
            }

            sizes[level] = reached;
        }

        counters.Clear();
        return Walk(run, in matcher, in parent, in node, true, count, counters, sizes, out _, out matched);
    }

    // Visits the siblings in document order; counters[j] is the position of a sibling at predicate j. With a target the
    // walk stops after it and reports whether it passed the first `levels` predicates.
    private QueryStatus Walk(
        IQueryRun run,
        in XPathNodeMatcher matcher,
        in XPathNode parent,
        in XPathNode target,
        bool hasTarget,
        int levels,
        Span<int> counters,
        ReadOnlySpan<int> sizes,
        out int reached,
        out bool targetPassed
    )
    {
        reached = 0;
        targetPassed = false;
        var detached = parent.IsEmpty();
        var sibling =
            detached ? target
            : _attributeAxis ? parent.FirstAttribute()
            : parent.FirstChild();
        while (!sibling.IsEmpty())
        {
            if (Selects(in matcher, in sibling))
            {
                var passes = true;
                for (var level = 0; level < levels && passes; level++)
                {
                    counters[level]++;
                    var status = run.TryEvaluateBoolean(
                        _predicates[level],
                        new QueryFocus(sibling, counters[level], sizes[level]),
                        out passes
                    );
                    if (status != QueryStatus.Ok)
                    {
                        return status;
                    }
                }

                if (passes)
                {
                    reached++;
                }

                if (hasTarget && sibling.IsSameNode(in target))
                {
                    targetPassed = passes;
                    return QueryStatus.Ok;
                }
            }

            sibling =
                detached ? default
                : _attributeAxis ? sibling.NextAttribute()
                : sibling.NextSibling();
        }

        return QueryStatus.Ok;
    }
}
