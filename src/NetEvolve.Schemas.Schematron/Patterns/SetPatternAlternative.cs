namespace NetEvolve.Schemas.Schematron.Patterns;

using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Xdm.Nodes;

/// <summary>
/// A node set operation of two patterns (XSLT 3.0 section 5.5): <c>union</c> matches either, <c>intersect</c> both, and
/// <c>except</c> the left but not the right pattern.
/// </summary>
/// <remarks>The default priority is 0.5, as for every pattern that is not a union at its top level.</remarks>
internal sealed class SetPatternAlternative(SetOperator op, PatternAlternative left, PatternAlternative right)
    : PatternAlternative(0.5m)
{
    /// <inheritdoc />
    internal override QueryStatus Matches(IQueryRun run, in XPathNode node, out bool matched)
    {
        var status = left.Matches(run, in node, out matched);
        if (status != QueryStatus.Ok)
        {
            return status;
        }

        // Union stops at a match of the left; intersect and except stop at a miss of it.
        if (matched == (op == SetOperator.Union) || (!matched && op == SetOperator.Except))
        {
            return QueryStatus.Ok;
        }

        status = right.Matches(run, in node, out var rightMatched);
        if (status != QueryStatus.Ok)
        {
            matched = false;
            return status;
        }

        matched = op == SetOperator.Except ? !rightMatched : rightMatched;
        return QueryStatus.Ok;
    }
}
