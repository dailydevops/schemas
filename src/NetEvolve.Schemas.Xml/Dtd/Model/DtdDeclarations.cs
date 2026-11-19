namespace NetEvolve.Schemas.Xml.Dtd.Model;

using System.Collections.Generic;
using NetEvolve.Schemas.Xml.Contracts;

/// <summary>The declarations of one document type declaration; the first declaration of an element, attribute, entity, or notation binds.</summary>
/// <remarks>Limitation: the declarations of the external subset and of the external parameter entities join the model only when the reader has external access enabled (XML-20); otherwise only the internal subset is read.</remarks>
internal sealed class DtdDeclarations : IDtdDeclarations
{
    internal DtdDeclarations(
        string name,
        string? publicId,
        string? systemId,
        bool hasParameterEntityReferences,
        IReadOnlyDictionary<string, DtdElementDeclaration> elements,
        IReadOnlyDictionary<string, IReadOnlyList<DtdAttributeDefinition>> attributeLists,
        IReadOnlyDictionary<string, DtdEntityDeclaration> generalEntities,
        IReadOnlyDictionary<string, DtdEntityDeclaration> parameterEntities,
        IReadOnlyDictionary<string, XmlNotation> notations
    )
    {
        Name = name;
        PublicId = publicId;
        SystemId = systemId;
        HasParameterEntityReferences = hasParameterEntityReferences;
        Elements = elements;
        AttributeLists = attributeLists;
        GeneralEntities = generalEntities;
        ParameterEntities = parameterEntities;
        Notations = notations;
    }

    /// <summary>Gets the root element name of the declaration.</summary>
    public string Name { get; }

    /// <summary>Gets the public identifier, <see langword="null"/> when absent.</summary>
    public string? PublicId { get; }

    /// <summary>Gets the system identifier, <see langword="null"/> when absent.</summary>
    public string? SystemId { get; }

    /// <summary>Gets a value indicating whether the declaration names an external subset (read only when the reader has external access enabled).</summary>
    public bool HasExternalSubset => SystemId is not null;

    /// <summary>Gets a value indicating whether the internal subset contains a parameter-entity reference (section 4.1, WFC Entity Declared).</summary>
    public bool HasParameterEntityReferences { get; }

    /// <summary>Gets the element declarations by element type name.</summary>
    public IReadOnlyDictionary<string, DtdElementDeclaration> Elements { get; }

    /// <summary>Gets the attribute definitions by element type name, in declaration order.</summary>
    public IReadOnlyDictionary<string, IReadOnlyList<DtdAttributeDefinition>> AttributeLists { get; }

    /// <summary>Gets the general entities by name.</summary>
    public IReadOnlyDictionary<string, DtdEntityDeclaration> GeneralEntities { get; }

    /// <summary>Gets the parameter entities by name.</summary>
    public IReadOnlyDictionary<string, DtdEntityDeclaration> ParameterEntities { get; }

    /// <summary>Gets the notations by name.</summary>
    public IReadOnlyDictionary<string, XmlNotation> Notations { get; }
}
