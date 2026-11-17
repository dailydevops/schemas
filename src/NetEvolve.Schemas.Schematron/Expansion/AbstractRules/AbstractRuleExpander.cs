namespace NetEvolve.Schemas.Schematron.Expansion.AbstractRules;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Schematron.Model;
using NetEvolve.Schemas.Schematron.Seams;

/// <summary>Expands <c>extends</c> by rule id (stage Expand, abstract rules).</summary>
internal static class AbstractRuleExpander
{
    internal static bool TryExpand(
        IReadOnlyList<SchematronRules> containers,
        IReadOnlyList<SchematronPatternBase> patterns,
        IProblemSink problems,
        out IReadOnlyList<SchematronPatternBase> expanded
    ) => throw new NotSupportedException();
}
