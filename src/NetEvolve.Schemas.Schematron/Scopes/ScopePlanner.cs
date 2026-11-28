namespace NetEvolve.Schemas.Schematron.Scopes;

using System.Collections.Generic;
using NetEvolve.Schemas.Schematron.Model;
using NetEvolve.Schemas.Schematron.Phases;
using NetEvolve.Schemas.Schematron.Seams;

/// <summary>
/// Plans the scopes of a schema for the selected phase (design #150 EP-4): schema (with its parameters), phase, pattern, and rule
/// each get a block of variable slots, an inner declaration shadows an outer one of the same name (#106 Q2), two declarations of one
/// name at one level are <c>SCH-LET-DUPLICATE</c>, and a variable is visible only after its own declaration and below its level. The
/// value of every variable is compiled in the scope of its position, so a forward or self reference and a variable of another
/// pattern are reported by the binding as <c>SCH-VAR-UNDEFINED</c>. No value is evaluated here: values are bound lazily by
/// <see cref="SlotBinder"/>.
/// </summary>
/// <remarks>
/// Limitations: by default a pattern <see langword="let"/> is lexical, visible only inside its pattern and not to later patterns or to function
/// bodies. This deviates from ISO/IEC 19757-3:2016 clause 5.4.5 items 1 and 3 as the conformance suite reads them, and it is
/// binding because of decision Q2 of #106 and #150 EP-4; the suite cases <c>let-pattern-global-01</c>,
/// <c>let-name-collision-error-05</c>, and <c>let-name-collision-error-06</c> are deviations of the default. The opt-in mode (#615, option B of
/// #150 EP-4, <c>hoistPatternLets</c>) hoists the pattern lets of the active patterns into the schema-level block after the schema lets, so they
/// are visible everywhere the schema level is, and a collision with a schema-level name or another pattern let is <c>SCH-LET-DUPLICATE</c>; a
/// phase or rule <see langword="let"/> still shadows. Only the patterns active in
/// the phase are planned, so a problem in another pattern surfaces when that phase is built. The global <c>xsl:variable</c> and
/// <c>xsl:param</c> declarations (SCH-66) are part of the schema-level block: the slots are numbered parameters first, then the
/// globals, then the lets, and the names of all of them are entered before a global value is compiled, so a global may read any
/// parameter, let, or global wherever it is declared and a let may read any global (XSLT 2.0 section 9.5, design #150 EP-4). A global
/// is matched by the name as written, so two prefixes of one namespace are two names. The <see langword="as"/> types are limited to
/// <c>xs:string</c>, <c>xs:boolean</c>, <c>xs:double</c>, and <c>xs:integer</c> (#392); a constant is checked here, any other value
/// by the binding through <see cref="QueryStaticScope.DeclaredType"/>, which the XPath binding does not do until it is implemented.
/// A constructor of a type derived from a supported type, such as <c>xs:long(5)</c> or <c>xs:token('a')</c>, is not treated as a
/// constant: it is left to the binding with the other values (#392, #227), so it is never a false Build problem.
/// Each variable value is compiled against its own copy of the visible names, so the compile memory grows with the square of the
/// number of variables declared at one level.
/// </remarks>
internal static class ScopePlanner
{
    /// <summary>Plans the scopes.</summary>
    /// <param name="schema">The effective schema.</param>
    /// <param name="selection">The phase selection: the active patterns and the phase variables.</param>
    /// <param name="binding">The binding that compiles the variable values.</param>
    /// <param name="problems">Receives every problem; planning continues after one.</param>
    /// <param name="plan">The plan; complete only when the method returns <see langword="true"/>.</param>
    /// <param name="hoistPatternLets"><see langword="true"/> to plan the pattern-level lets as schema-level lets (#615).</param>
    /// <returns><see langword="true"/> when no problem was reported.</returns>
    internal static bool TryPlan(
        SchematronSchema schema,
        PhaseSelection selection,
        IQueryBinding binding,
        IProblemSink problems,
        out ScopePlan plan,
        bool hoistPatternLets = false
    ) => TryPlan(schema, selection, [], binding, problems, out plan, hoistPatternLets);

    /// <summary>Plans the scopes with the global <c>xsl:variable</c> and <c>xsl:param</c> declarations of the stylesheet.</summary>
    /// <param name="schema">The effective schema.</param>
    /// <param name="selection">The phase selection: the active patterns and the phase variables.</param>
    /// <param name="globals">The global declarations that won the import precedence, in declaration order.</param>
    /// <param name="binding">The binding that compiles the variable values.</param>
    /// <param name="problems">Receives every problem; planning continues after one.</param>
    /// <param name="plan">The plan; complete only when the method returns <see langword="true"/>.</param>
    /// <param name="hoistPatternLets"><see langword="true"/> to plan the pattern-level lets as schema-level lets (#615).</param>
    /// <returns><see langword="true"/> when no problem was reported.</returns>
    internal static bool TryPlan(
        SchematronSchema schema,
        PhaseSelection selection,
        IReadOnlyList<GlobalDeclaration> globals,
        IQueryBinding binding,
        IProblemSink problems,
        out ScopePlan plan,
        bool hoistPatternLets = false
    )
    {
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(selection);
        ArgumentNullException.ThrowIfNull(globals);
        ArgumentNullException.ThrowIfNull(binding);
        ArgumentNullException.ThrowIfNull(problems);

        return new PlanRun(schema, binding, problems, globals, hoistPatternLets).Execute(selection, out plan);
    }
}
