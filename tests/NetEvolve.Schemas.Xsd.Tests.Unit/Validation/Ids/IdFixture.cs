namespace NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Ids;

using System.Collections.Generic;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xsd.Attributes.Declarations;
using NetEvolve.Schemas.Xsd.Components;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.Elements;
using NetEvolve.Schemas.Xsd.SimpleTypes.Definitions;
using NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Core;
using NetEvolve.Schemas.Xsd.Validation.Core;
using NetEvolve.Schemas.Xsd.Validation.Ids;

/// <summary>
/// A root <c>r</c> that holds any number of <c>e</c> (the attributes id, ref, refs, and note), and of the
/// elements <c>i</c>, <c>f</c>, <c>fs</c>, and <c>n</c>, whose simple content is an ID, an IDREF, an IDREFS, and a NOTATION.
/// </summary>
internal static class IdFixture
{
    public static SchemaFixture Schema(
        SimpleTypeDefinition? id = null,
        SimpleTypeDefinition? idref = null,
        SimpleTypeDefinition? idrefs = null,
        SimpleTypeDefinition? notation = null,
        AttributeValueConstraint idConstraint = default
    )
    {
        id ??= SchemaFixture.Simple("ID");
        idref ??= SchemaFixture.Simple("IDREF");
        idrefs ??= SchemaFixture.Simple("IDREFS");
        notation ??= SchemaFixture.Simple("NOTATION");

        var schema = new SchemaFixture();
        var e = schema.Empty(
            Use(schema, "id", id, idConstraint),
            Use(schema, "ref", idref),
            Use(schema, "refs", idrefs),
            Use(schema, "note", notation)
        );
        schema.Global(
            "r",
            schema.ElementOnly(
                SchemaFixture.Group(
                    ModelGroupCompositor.Choice,
                    0,
                    long.MaxValue,
                    SchemaFixture.Part("e", e),
                    SchemaFixture.Part("i", id),
                    SchemaFixture.Part("f", idref),
                    SchemaFixture.Part("fs", idrefs),
                    SchemaFixture.Part("n", notation)
                )
            )
        );
        return schema;
    }

    /// <summary>Runs the instance with an <see cref="IdListener"/> that knows the notations in no namespace.</summary>
    public static IReadOnlyList<ValidationIssue> Run(SchemaFixture schema, string xml, params string[] notations) =>
        ValidationHarness.Run(schema, xml, null, Listener(new ScriptedAssessor(), notations));

    public static IdListener Listener(ISimpleValueAssessor assessor, params string[] notations) =>
        Listener(assessor, XsdVersion.V11, notations);

    public static IdListener Listener(ISimpleValueAssessor assessor, XsdVersion version, params string[] notations)
    {
        var registry = new ComponentRegistry(
            new XsdIssueReporter(new ValidationIssueCollector(ValidationLimits.Default))
        );
        foreach (var notation in notations)
        {
            registry.Add(new NotationFake(notation));
        }

        return new IdListener(assessor, registry.Freeze(version));
    }

    public static SimpleTypeDefinition Restriction(SimpleTypeDefinition baseType, SchemaFixture schema) =>
        SimpleTypeDefinition.Restrict(
            default,
            SchemaFixture.Where,
            XsdDerivationMethod.None,
            baseType,
            [],
            schema.Reporter
        )!;

    public static SimpleTypeDefinition ListOf(SimpleTypeDefinition item, SchemaFixture schema) =>
        SimpleTypeDefinition.List(default, SchemaFixture.Where, XsdDerivationMethod.None, item, [], schema.Reporter)!;

    public static SimpleTypeDefinition UnionOf(SchemaFixture schema, params SimpleTypeDefinition[] members) =>
        SimpleTypeDefinition.Union(
            default,
            SchemaFixture.Where,
            XsdDerivationMethod.None,
            members,
            [],
            schema.Reporter
        )!;

    private static AttributeUse Use(
        SchemaFixture schema,
        string name,
        SimpleTypeDefinition type,
        AttributeValueConstraint constraint = default
    ) =>
        AttributeUse.Create(
            SchemaFixture.Where,
            AttributeUseKind.Optional,
            schema.AttrDecl(name, type),
            constraint,
            schema.Reporter
        )!;
}
