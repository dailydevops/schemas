namespace NetEvolve.Schemas.Schematron.Expansion.AbstractRules;

using System.Collections.Generic;
using NetEvolve.Schemas.Schematron.Model;
using NetEvolve.Schemas.Schematron.Seams;

/// <summary>
/// Expands <c>extends</c> by rule id (stage Expand, abstract rules): the assertions, reports, paragraphs, and variables of the
/// abstract rule are copied into the extending rule, and abstract rules never reach the effective model, so they cannot fire.
/// </summary>
/// <remarks>
/// Lookup scope: a rule in a <c>rules</c> container is found from any pattern, an abstract rule in a pattern only from its own
/// pattern (decision OQ-2 of design #137: an abstract rule may also sit in a pattern). Copied nodes are shared, not cloned: the
/// model is immutable and a copy keeps the location of its definition. <c>extends</c> with <c>href</c> belongs to the inclusion
/// stage and is passed through unchanged.
/// </remarks>
internal static class AbstractRuleExpander
{
    internal static bool TryExpand(
        IReadOnlyList<SchematronRules> containers,
        IReadOnlyList<SchematronPatternBase> patterns,
        IProblemSink problems,
        out IReadOnlyList<SchematronPatternBase> expanded
    )
    {
        ArgumentNullException.ThrowIfNull(containers);
        ArgumentNullException.ThrowIfNull(patterns);
        ArgumentNullException.ThrowIfNull(problems);

        var run = new ExpansionRun(containers, patterns, problems);
        var result = new List<SchematronPatternBase>(patterns.Count);
        foreach (var pattern in patterns)
        {
            result.Add(run.ExpandPattern(pattern));
        }

        expanded = run.Failed ? patterns : result;
        return !run.Failed;
    }
}
