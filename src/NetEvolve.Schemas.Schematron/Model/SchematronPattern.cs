namespace NetEvolve.Schemas.Schematron.Model;

using System.Collections.Generic;

/// <summary>The <c>pattern</c> element.</summary>
public sealed class SchematronPattern : SchematronPatternBase
{
    internal SchematronPattern(
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
        DocumentLocation? expandedFrom
    )
        : base(location, id, isAbstract, isA, documents, role, title, paragraphs, parameters, lets, rules, expandedFrom)
    { }
}
