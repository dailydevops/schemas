namespace NetEvolve.Schemas.Schematron.Model;

using System.Collections.Generic;

/// <summary>The <see langword="group"/> element of the 2025 edition.</summary>
public sealed class SchematronGroup : SchematronPatternBase
{
    internal SchematronGroup(
        DocumentLocation location,
        string? id,
        bool isAbstract,
        string? isA,
        string? documents,
        string? role,
        SchematronTitle? title,
        IReadOnlyList<SchematronParagraph> paragraphs,
        IReadOnlyList<SchematronParameter> parameters,
        IReadOnlyList<SchematronLet> lets,
        IReadOnlyList<SchematronRule> rules,
        DocumentLocation? expandedFrom,
        IReadOnlyList<SchematronInclude>? includes = null
    )
        : base(
            location,
            id,
            isAbstract,
            isA,
            documents,
            role,
            title,
            paragraphs,
            parameters,
            lets,
            rules,
            expandedFrom,
            includes
        ) { }
}
