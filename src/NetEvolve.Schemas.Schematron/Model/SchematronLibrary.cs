namespace NetEvolve.Schemas.Schematron.Model;

using System.Collections.Generic;

// Internal parser intermediate: an include target consumed by stage Resolve, never part of the effective model.
internal sealed class SchematronLibrary : SchematronContainer
{
    internal SchematronLibrary(
        DocumentLocation location,
        SchematronTitle? title,
        IReadOnlyList<SchematronParagraph> paragraphs,
        IReadOnlyList<SchematronParameter> parameters,
        IReadOnlyList<SchematronLet> lets,
        IReadOnlyList<SchematronPatternBase> patterns,
        SchematronDiagnostics? diagnostics,
        SchematronProperties? properties,
        IReadOnlyList<SchematronRules> rules,
        IReadOnlyList<SchematronForeignElement> foreignElements,
        IReadOnlyList<SchematronInclude>? includes = null
    )
        : base(
            location,
            title,
            paragraphs,
            parameters,
            lets,
            patterns,
            diagnostics,
            properties,
            rules,
            foreignElements,
            includes
        ) { }
}
