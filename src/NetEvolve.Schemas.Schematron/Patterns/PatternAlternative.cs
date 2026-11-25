namespace NetEvolve.Schemas.Schematron.Patterns;

using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.XPath.Xdm.Nodes;

/// <summary>One alternative of a union pattern: a path, or one of the XSLT 3.0 forms built on paths.</summary>
/// <param name="priority">The default priority of the alternative.</param>
internal abstract class PatternAlternative(decimal priority)
{
    /// <summary>Gets the default priority of the alternative (XSLT 2.0 section 6.4).</summary>
    internal decimal Priority { get; } = priority;

    /// <summary>Determines whether the node matches the alternative.</summary>
    /// <param name="run">The run that evaluates the predicates.</param>
    /// <param name="node">The candidate node.</param>
    /// <param name="matched">Whether the node matches.</param>
    /// <returns>The status of the evaluation.</returns>
    internal abstract QueryStatus Matches(IQueryRun run, in XPathNode node, out bool matched);
}
