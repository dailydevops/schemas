namespace NetEvolve.Schemas.Schematron.Patterns;

using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.XPath.Xdm.Nodes;

/// <summary>
/// A parenthesized pattern with predicates (XSLT 3.0 section 5.5): the node matches the inner pattern and every predicate
/// holds with the node as the context item.
/// </summary>
/// <remarks>
/// Limitation: a predicate that reads the position or size of the node among the nodes of the inner pattern is reported as
/// unsupported when the pattern is compiled, so every predicate here is a plain boolean test. The default priority is 0.5.
/// </remarks>
internal sealed class FilteredPatternAlternative(PatternAlternative inner, QueryExpression[] predicates)
    : PatternAlternative(0.5m)
{
    /// <inheritdoc />
    internal override QueryStatus Matches(IQueryRun run, in XPathNode node, out bool matched)
    {
        var status = inner.Matches(run, in node, out matched);
        if (status != QueryStatus.Ok || !matched)
        {
            return status;
        }

        var focus = new QueryFocus(node, 1, -1);
        for (var i = 0; i < predicates.Length; i++)
        {
            status = run.TryEvaluateBoolean(predicates[i], in focus, out matched);
            if (status != QueryStatus.Ok || !matched)
            {
                matched = false;
                return status;
            }
        }

        return QueryStatus.Ok;
    }
}
