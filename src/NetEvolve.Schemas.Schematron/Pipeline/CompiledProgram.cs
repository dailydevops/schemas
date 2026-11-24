namespace NetEvolve.Schemas.Schematron.Pipeline;

using System.Collections.Generic;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Schematron.Evaluation.Patterns;
using NetEvolve.Schemas.Schematron.Model;
using NetEvolve.Schemas.Schematron.Scopes;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Xslt.Modules;

/// <summary>
/// The compiled schema of the pipeline: the effective model with the selected phase, the binding that compiled it, the plan of the
/// variables, and the active patterns with their compiled rules. Immutable; every run of the validator reads it and writes nothing.
/// </summary>
/// <remarks>
/// Limitation: that nothing a run does is visible to another run holds for what the pipeline composes (the compiled rule sets, the
/// plan, and the expressions are never written after the build), and a test pins concurrent runs; the proof across the hot path
/// and the compile-once cache are SCH-71 (#340).
/// </remarks>
internal sealed class CompiledProgram : CompiledSchema
{
    internal CompiledProgram(
        SchematronSchema schema,
        string effectivePhase,
        string title,
        IQueryBinding binding,
        IPatternMatcher matcher,
        QueryResources resources,
        ScopePlan plan,
        IReadOnlyList<CompiledRuleSet> sets,
        XsltModuleTables? xsltTables = null
    )
        : base(schema, effectivePhase)
    {
        ArgumentNullException.ThrowIfNull(title);
        ArgumentNullException.ThrowIfNull(binding);
        ArgumentNullException.ThrowIfNull(matcher);
        ArgumentNullException.ThrowIfNull(resources);
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(sets);

        Title = title;
        Binding = binding;
        Matcher = matcher;
        Resources = resources;
        Plan = plan;
        Sets = sets;
        XsltTables = xsltTables;
    }

    /// <summary>Gets the text of the title of the schema, empty when it has none.</summary>
    internal string Title { get; }

    internal IQueryBinding Binding { get; }

    internal IPatternMatcher Matcher { get; }

    /// <summary>Gets the resolver adapter that every run of the validator hands to the binding.</summary>
    internal QueryResources Resources { get; }

    internal ScopePlan Plan { get; }

    /// <summary>Gets the active patterns and groups in document order; empty when the phase activates none.</summary>
    internal IReadOnlyList<CompiledRuleSet> Sets { get; }

    /// <summary>
    /// Gets the tables of the XSLT declarations (functions, globals, keys, and decimal formats) that the schema links, or
    /// <see langword="null"/> when it links none. Limitation: a run reads the globals through the plan, but not the functions and keys.
    /// </summary>
    internal XsltModuleTables? XsltTables { get; }
}
