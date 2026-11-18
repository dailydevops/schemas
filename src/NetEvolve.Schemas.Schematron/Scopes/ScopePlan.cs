namespace NetEvolve.Schemas.Schematron.Scopes;

using System.Collections.Generic;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Schematron.Seams;

/// <summary>
/// The scopes of one compiled schema and phase. Slots are numbered schema first (parameters, then variables), then the selected
/// phase, then a pattern block that every pattern reuses, then a rule block that every rule reuses, so a lookup is an array index.
/// </summary>
internal sealed class ScopePlan
{
    internal ScopePlan(
        IReadOnlyList<VariableSlot> globals,
        IReadOnlyList<VariableSlot> phaseLets,
        IReadOnlyList<PatternScope> patterns,
        QueryStaticScope globalScope,
        int patternStart,
        int ruleStart,
        int slotCount
    )
    {
        Argument.ThrowIfNull(globals);
        Argument.ThrowIfNull(phaseLets);
        Argument.ThrowIfNull(patterns);
        Argument.ThrowIfNull(globalScope);
        Argument.ThrowIfLessThan(patternStart, 0);
        Argument.ThrowIfLessThan(ruleStart, patternStart);
        Argument.ThrowIfLessThan(slotCount, ruleStart);

        Globals = globals;
        PhaseLets = phaseLets;
        Patterns = patterns;
        GlobalScope = globalScope;
        PatternStart = patternStart;
        RuleStart = ruleStart;
        SlotCount = slotCount;
    }

    /// <summary>Gets the schema-level parameters and variables.</summary>
    internal IReadOnlyList<VariableSlot> Globals { get; }

    /// <summary>Gets the variables of the selected phase; empty for <c>#ALL</c>.</summary>
    internal IReadOnlyList<VariableSlot> PhaseLets { get; }

    /// <summary>Gets the active patterns and groups in document order.</summary>
    internal IReadOnlyList<PatternScope> Patterns { get; }

    /// <summary>
    /// Gets the scope of function bodies and keys: the schema level only. Phase, pattern, and rule variables are not visible there,
    /// because a phase is chosen per run and a pattern variable belongs to its pattern (design #150 EP-4).
    /// </summary>
    internal QueryStaticScope GlobalScope { get; }

    /// <summary>Gets the first slot of the pattern block.</summary>
    internal int PatternStart { get; }

    /// <summary>Gets the first slot of the rule block.</summary>
    internal int RuleStart { get; }

    /// <summary>Gets the number of slots a run needs.</summary>
    internal int SlotCount { get; }
}
