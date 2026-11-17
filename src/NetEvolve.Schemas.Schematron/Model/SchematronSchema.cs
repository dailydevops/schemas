namespace NetEvolve.Schemas.Schematron.Model;

using System.Collections.Generic;

/// <summary>The <c>schema</c> element: the effective model after includes are merged and abstract patterns are expanded.</summary>
public sealed class SchematronSchema : SchematronContainer
{
    internal SchematronSchema(
        DocumentLocation location,
        SchematronTitle? title,
        IReadOnlyList<SchematronParagraph> paragraphs,
        IReadOnlyList<SchematronParameter> parameters,
        IReadOnlyList<SchematronLet> lets,
        IReadOnlyList<SchematronPatternBase> patterns,
        SchematronDiagnostics? diagnostics,
        SchematronProperties? properties,
        string? id,
        string? schemaVersion,
        string? editionText,
        SchematronEdition edition,
        string? defaultPhase,
        string? queryBinding,
        IReadOnlyList<SchematronNamespace> namespaces,
        IReadOnlyList<SchematronPhase> phases,
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
        )
    {
        Id = id;
        SchemaVersion = schemaVersion;
        EditionText = editionText;
        Edition = edition;
        DefaultPhase = defaultPhase;
        QueryBinding = queryBinding;
        Namespaces = namespaces;
        Phases = phases;
    }

    /// <summary>Gets the <c>id</c> attribute, or <see langword="null"/> when absent.</summary>
    public string? Id { get; }

    /// <summary>Gets the <c>schemaVersion</c> attribute, or <see langword="null"/> when absent.</summary>
    public string? SchemaVersion { get; }

    /// <summary>Gets the <c>schematronEdition</c> attribute as written, or <see langword="null"/> when absent.</summary>
    public string? EditionText { get; }

    /// <summary>Gets the edition. Absent means <see cref="SchematronEdition.Iso2016"/>.</summary>
    public SchematronEdition Edition { get; }

    /// <summary>Gets the <c>defaultPhase</c> attribute, or <see langword="null"/> when absent.</summary>
    public string? DefaultPhase { get; }

    /// <summary>Gets the <c>queryBinding</c> attribute, or <see langword="null"/> when absent.</summary>
    public string? QueryBinding { get; }

    /// <summary>Gets the namespace declarations.</summary>
    public IReadOnlyList<SchematronNamespace> Namespaces { get; }

    /// <summary>Gets the phases.</summary>
    public IReadOnlyList<SchematronPhase> Phases { get; }
}
