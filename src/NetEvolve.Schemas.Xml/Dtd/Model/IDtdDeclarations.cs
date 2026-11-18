namespace NetEvolve.Schemas.Xml.Dtd.Model;

using System.Collections.Generic;
using NetEvolve.Schemas.Xml.Contracts;

/// <summary>
/// The read-only view of the declarations of one document type declaration that a validation step sees (design #122, D2).
/// The first declaration of an element, attribute, entity, or notation binds.
/// </summary>
/// <remarks>
/// Limitation: only the internal subset is read (the external subset is XML-20), and the view records neither the position
/// of each declaration nor the anomalies that only the declaration parser can see (#608). The parser model behind the
/// view stays internal; consumers depend on this interface only. Not intended for implementation by consumers.
/// Adding a member to this interface later is a breaking change for every implementer; #608 (per-declaration locations
/// and parse-time anomalies) may therefore need a second interface instead of new members here.
/// </remarks>
public interface IDtdDeclarations
{
    /// <summary>Gets the root element name of the declaration.</summary>
    string Name { get; }

    /// <summary>Gets the public identifier, <see langword="null"/> when absent.</summary>
    string? PublicId { get; }

    /// <summary>Gets the system identifier, <see langword="null"/> when absent.</summary>
    string? SystemId { get; }

    /// <summary>Gets a value indicating whether the declaration names an external subset.</summary>
    bool HasExternalSubset { get; }

    /// <summary>Gets a value indicating whether the internal subset contains a parameter-entity reference (section 4.1, WFC Entity Declared).</summary>
    bool HasParameterEntityReferences { get; }

    /// <summary>Gets the element declarations by element type name.</summary>
    IReadOnlyDictionary<string, DtdElementDeclaration> Elements { get; }

    /// <summary>Gets the attribute definitions by element type name, in declaration order.</summary>
    IReadOnlyDictionary<string, IReadOnlyList<DtdAttributeDefinition>> AttributeLists { get; }

    /// <summary>Gets the general entities by name.</summary>
    IReadOnlyDictionary<string, DtdEntityDeclaration> GeneralEntities { get; }

    /// <summary>Gets the parameter entities by name.</summary>
    IReadOnlyDictionary<string, DtdEntityDeclaration> ParameterEntities { get; }

    /// <summary>Gets the notations by name.</summary>
    IReadOnlyDictionary<string, XmlNotation> Notations { get; }
}
