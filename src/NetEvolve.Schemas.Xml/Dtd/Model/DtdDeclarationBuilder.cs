namespace NetEvolve.Schemas.Xml.Dtd.Model;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Xml.Contracts;

/// <summary>Collects declarations and applies the precedence rule: the first declaration of a name binds, later ones are ignored.</summary>
internal sealed class DtdDeclarationBuilder
{
    private readonly string _name;
    private readonly string? _publicId;
    private readonly string? _systemId;
    private readonly Dictionary<string, DtdElementDeclaration> _elements = new(StringComparer.Ordinal);
    private readonly Dictionary<string, List<DtdAttributeDefinition>> _attributes = new(StringComparer.Ordinal);
    private readonly Dictionary<string, DtdEntityDeclaration> _general = new(StringComparer.Ordinal);
    private readonly Dictionary<string, DtdEntityDeclaration> _parameter = new(StringComparer.Ordinal);
    private readonly Dictionary<string, XmlNotation> _notations = new(StringComparer.Ordinal);
    private List<string>? _duplicateNotations;
    private List<DtdEntityDeclaration>? _duplicateEntities;

    internal DtdDeclarationBuilder(string name, string? publicId, string? systemId)
    {
        _name = name;
        _publicId = publicId;
        _systemId = systemId;
    }

    /// <summary>Gets a value indicating whether the declaration names an external subset.</summary>
    internal bool HasExternalSubset => _systemId is not null;

    /// <summary>Gets or sets a value indicating whether a parameter-entity reference was seen.</summary>
    internal bool HasParameterEntityReferences { get; set; }

    /// <summary>Adds an element declaration unless the element is already declared.</summary>
    /// <param name="declaration">The declaration.</param>
    internal void AddElement(DtdElementDeclaration declaration) => AddFirst(_elements, declaration.Name, declaration);

    /// <summary>Adds an attribute definition unless the element already defines the attribute.</summary>
    /// <param name="element">The element type name.</param>
    /// <param name="definition">The definition.</param>
    internal void AddAttribute(string element, DtdAttributeDefinition definition)
    {
        if (!_attributes.TryGetValue(element, out var list))
        {
            list = [];
            _attributes.Add(element, list);
        }

        // ponytail: linear duplicate scan per attribute list; a hash set per list if one element declares thousands.
        foreach (var existing in list)
        {
            if (string.Equals(existing.Name, definition.Name, StringComparison.Ordinal))
            {
                return;
            }
        }

        list.Add(definition);
    }

    /// <summary>Adds an entity declaration unless the entity of that kind is already declared.</summary>
    /// <param name="declaration">The declaration.</param>
    internal void AddEntity(DtdEntityDeclaration declaration)
    {
        var map = declaration.IsParameter ? _parameter : _general;
        if (!map.TryGetValue(declaration.Name, out _))
        {
            map.Add(declaration.Name, declaration);
        }
        else
        {
            // The optional warning XML5904 is decided from this record; it stays null for a document without a repeat.
            (_duplicateEntities ??= []).Add(declaration);
        }
    }

    /// <summary>Adds a notation unless it is already declared.</summary>
    /// <param name="notation">The notation.</param>
    internal void AddNotation(XmlNotation notation)
    {
        if (!_notations.TryGetValue(notation.Name, out _))
        {
            _notations.Add(notation.Name, notation);
        }
        else
        {
            // Unique Notation Name (section 4.7) is decided from this record; it stays null for a document without a repeat.
            (_duplicateNotations ??= []).Add(notation.Name);
        }
    }

    // Dictionary.TryAdd does not exist on netstandard2.0.
    private static void AddFirst<T>(Dictionary<string, T> map, string key, T value)
    {
        if (!map.TryGetValue(key, out _))
        {
            map.Add(key, value);
        }
    }

    /// <summary>Gets a value indicating whether a general entity is already declared.</summary>
    /// <param name="name">The entity name.</param>
    /// <returns><see langword="true"/> when declared.</returns>
    internal bool IsGeneralEntityDeclared(string name) => _general.ContainsKey(name);

    /// <summary>Gets the declared parameter entity, if any.</summary>
    /// <param name="name">The entity name.</param>
    /// <param name="entity">The declaration.</param>
    /// <returns><see langword="true"/> when declared.</returns>
    internal bool TryGetParameterEntity(string name, out DtdEntityDeclaration entity) =>
        _parameter.TryGetValue(name, out entity!);

    /// <summary>Creates the model.</summary>
    /// <returns>The declarations.</returns>
    internal DtdDeclarations Build()
    {
        var lists = new Dictionary<string, IReadOnlyList<DtdAttributeDefinition>>(
            _attributes.Count,
            StringComparer.Ordinal
        );
        foreach (var pair in _attributes)
        {
            lists.Add(pair.Key, pair.Value);
        }

        return new DtdDeclarations(
            _name,
            _publicId,
            _systemId,
            HasParameterEntityReferences,
            _elements,
            lists,
            _general,
            _parameter,
            _notations,
            _duplicateNotations,
            _duplicateEntities
        );
    }
}
