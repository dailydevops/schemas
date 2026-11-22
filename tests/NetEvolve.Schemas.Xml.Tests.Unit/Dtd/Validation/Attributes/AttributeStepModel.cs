namespace NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Validation.Attributes;

using NetEvolve.Schemas.Xml.Dtd.Model;

/// <summary>A hand-built declaration model written in declaration order, so a test needs no declaration parser.</summary>
internal sealed class AttributeStepModel
{
    private readonly DtdDeclarationBuilder _builder;

    internal AttributeStepModel(string name = "r") => _builder = new DtdDeclarationBuilder(name, null, null);

    internal AttributeStepModel Element(string name, DtdContentKind kind = DtdContentKind.Any)
    {
        _builder.AddElement(new DtdElementDeclaration(name, kind, null));

        return this;
    }

    internal AttributeStepModel Attlist(string element, params DtdAttributeDefinition[] definitions)
    {
        foreach (var definition in definitions)
        {
            _builder.AddAttribute(element, definition);
        }

        return this;
    }

    /// <summary>A general entity with an internal replacement text.</summary>
    internal AttributeStepModel Entity(string name, string value)
    {
        _builder.AddEntity(new DtdEntityDeclaration(name, false, value, null, null, null));

        return this;
    }

    /// <summary>An unparsed general entity.</summary>
    internal AttributeStepModel Unparsed(string name)
    {
        _builder.AddEntity(new DtdEntityDeclaration(name, false, null, null, "image.gif", "gif"));

        return this;
    }

    /// <summary>An external parsed general entity.</summary>
    internal AttributeStepModel External(string name)
    {
        _builder.AddEntity(new DtdEntityDeclaration(name, false, null, null, "text.ent", null));

        return this;
    }

    internal DtdDeclarations Build() => _builder.Build();

    internal static DtdAttributeDefinition Attr(
        string name,
        DtdAttributeType type,
        DtdDefaultKind kind = DtdDefaultKind.Implied,
        string? value = null,
        params string[] values
    ) => new(name, type, values, kind, value);

    internal static DtdAttributeDefinition Cdata(
        string name,
        DtdDefaultKind kind = DtdDefaultKind.Implied,
        string? value = null
    ) => Attr(name, DtdAttributeType.Cdata, kind, value);

    internal static DtdAttributeDefinition NmToken(
        string name,
        DtdDefaultKind kind = DtdDefaultKind.Implied,
        string? value = null
    ) => Attr(name, DtdAttributeType.NmToken, kind, value);

    internal static DtdAttributeDefinition NmTokens(
        string name,
        DtdDefaultKind kind = DtdDefaultKind.Implied,
        string? value = null
    ) => Attr(name, DtdAttributeType.NmTokens, kind, value);

    internal static DtdAttributeDefinition Enumeration(
        string name,
        DtdDefaultKind kind,
        string? value,
        params string[] values
    ) => Attr(name, DtdAttributeType.Enumeration, kind, value, values);

    internal static DtdAttributeDefinition Enumeration(string name, params string[] values) =>
        Attr(name, DtdAttributeType.Enumeration, DtdDefaultKind.Implied, null, values);

    internal static DtdAttributeDefinition Required(string name, DtdAttributeType type = DtdAttributeType.Cdata) =>
        Attr(name, type, DtdDefaultKind.Required);
}
