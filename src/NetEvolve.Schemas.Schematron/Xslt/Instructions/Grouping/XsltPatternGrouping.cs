namespace NetEvolve.Schemas.Schematron.Xslt.Instructions.Grouping;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Xslt.Instructions.Flow;

/// <summary>
/// <c>group-starting-with</c> and <c>group-ending-with</c>: every item must be a node (XTTE1120). A node that matches the pattern, or
/// the first node, starts a group when the pattern starts groups; a node that follows a match, or the first node, starts a group when
/// the pattern ends groups. Matching is done by the shared pattern matcher (design #193) and by nothing else. A group has no grouping key.
/// </summary>
internal sealed class XsltPatternGrouping : XsltGrouping
{
    private readonly IPatternMatcher _matcher;
    private readonly CompiledPattern _pattern;
    private readonly bool _ending;

    internal XsltPatternGrouping(
        IPatternMatcher matcher,
        CompiledPattern pattern,
        bool ending,
        DocumentLocation location
    )
        : base(location)
    {
        ArgumentNullException.ThrowIfNull(matcher);
        ArgumentNullException.ThrowIfNull(pattern);

        _matcher = matcher;
        _pattern = pattern;
        _ending = ending;
    }

    /// <inheritdoc />
    internal override ReadOnlySpan<int> ReferencedSlots => _pattern.ReferencedSlots;

    /// <inheritdoc />
    internal override QueryStatus TryGroup(XsltFrame frame, ItemBuffer items, List<XsltGroup> groups)
    {
        ArgumentNullException.ThrowIfNull(frame);
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(groups);

        var previousMatched = false;
        for (var i = 0; i < items.Count; i++)
        {
            frame.CancellationToken.ThrowIfCancellationRequested();
            if (!items.Get(i).TryGetNode(out var node))
            {
                return Fail(
                    frame,
                    SchematronDiagnosticCodes.EvaluationFailed,
                    "The select expression of a grouping by pattern gave an item that is not a node (XTTE1120).",
                    "XTTE1120"
                );
            }

            // The matcher reports its problem through the run, which the frame reads when this method returns the problem.
            if (_matcher.Matches(_pattern, frame.Run, in node, out var matched) == QueryStatus.Problem)
            {
                return QueryStatus.Problem;
            }

            if (i == 0 || (_ending ? previousMatched : matched))
            {
                groups.Add(new XsltGroup());
            }

            groups[groups.Count - 1].Members.Add(i);
            previousMatched = matched;
        }

        return QueryStatus.Ok;
    }
}
