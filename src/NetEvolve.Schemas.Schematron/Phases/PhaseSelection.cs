namespace NetEvolve.Schemas.Schematron.Phases;

using System.Collections.Generic;
using NetEvolve.Schemas.Schematron.Model;

/// <summary>The outcome of phase selection: the effective phase, the active patterns, and the phase-level variables.</summary>
internal sealed class PhaseSelection
{
    internal PhaseSelection(
        string effectivePhase,
        IReadOnlyList<SchematronPatternBase> patterns,
        IReadOnlyList<SchematronLet> lets
    )
    {
        EffectivePhase = effectivePhase;
        Patterns = patterns;
        Lets = lets;
    }

    /// <summary>Gets the resolved phase id, or <c>#ALL</c>.</summary>
    internal string EffectivePhase { get; }

    /// <summary>Gets the active, non-abstract patterns and groups in document order.</summary>
    internal IReadOnlyList<SchematronPatternBase> Patterns { get; }

    /// <summary>Gets the variables declared by the selected phase; empty for <c>#ALL</c>.</summary>
    internal IReadOnlyList<SchematronLet> Lets { get; }
}
