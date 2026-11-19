namespace NetEvolve.Schemas.Schematron.Expansion.AbstractPatterns;

using System.Collections.Generic;
using NetEvolve.Schemas.Schematron.Diagnostics;
using NetEvolve.Schemas.Schematron.Model;
using NetEvolve.Schemas.Schematron.Seams;

/// <summary>Stub of the abstract pattern step of stage Expand.</summary>
internal static class AbstractPatternExpander
{
    internal static bool TryExpand(
        IReadOnlyList<SchematronPatternBase> patterns,
        IProblemSink problems,
        out IReadOnlyList<SchematronPatternBase> expanded
    ) => throw NotImplementedFailure.For("expand abstract patterns");
}
