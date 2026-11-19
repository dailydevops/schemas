namespace NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Validation.Notations;

using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Model;

/// <summary>A hand-built declaration model written in declaration order, so a test needs no declaration parser.</summary>
internal sealed class NotationModel
{
    private readonly DtdDeclarationBuilder _builder = new("r", publicId: null, systemId: null);

    internal NotationModel Element(string name, DtdContentKind kind = DtdContentKind.Any)
    {
        _builder.AddElement(new DtdElementDeclaration(name, kind, null));

        return this;
    }

    internal NotationModel Attlist(string element, params DtdAttributeDefinition[] definitions)
    {
        foreach (var definition in definitions)
        {
            _builder.AddAttribute(element, definition);
        }

        return this;
    }

    internal NotationModel Entity(DtdEntityDeclaration entity)
    {
        _builder.AddEntity(entity);

        return this;
    }

    internal NotationModel Notation(string name)
    {
        _builder.AddNotation(new XmlNotation(name, "system", null));

        return this;
    }

    internal DtdDeclarations Build() => _builder.Build();

    internal static DtdAttributeDefinition NotationAttribute(string name, params string[] notations) =>
        new(name, DtdAttributeType.Notation, notations, DtdDefaultKind.Implied, null);

    internal static DtdAttributeDefinition EntityAttribute(string name) =>
        new(name, DtdAttributeType.Entity, null, DtdDefaultKind.Implied, null);

    internal static DtdAttributeDefinition EntitiesAttribute(string name) =>
        new(name, DtdAttributeType.Entities, null, DtdDefaultKind.Implied, null);

    internal static DtdAttributeDefinition CdataAttribute(string name) =>
        new(name, DtdAttributeType.Cdata, null, DtdDefaultKind.Implied, null);

    /// <summary>An unparsed general entity: a system identifier and a notation name.</summary>
    internal static DtdEntityDeclaration Unparsed(string name, string notation) =>
        new(name, isParameter: false, value: null, publicId: null, systemId: "image.gif", notationName: notation);

    /// <summary>A parsed internal general entity.</summary>
    internal static DtdEntityDeclaration Parsed(string name) =>
        new(name, isParameter: false, value: "text", publicId: null, systemId: null, notationName: null);

    /// <summary>A parameter entity; it is never a general entity, whatever its name.</summary>
    internal static DtdEntityDeclaration Parameter(string name) =>
        new(name, isParameter: true, value: "text", publicId: null, systemId: null, notationName: null);
}
