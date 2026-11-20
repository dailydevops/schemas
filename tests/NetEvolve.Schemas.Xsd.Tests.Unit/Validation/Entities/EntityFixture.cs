namespace NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Entities;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Reader.Pipeline;
using NetEvolve.Schemas.Xsd.Attributes.Declarations;
using NetEvolve.Schemas.Xsd.Components;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.Elements;
using NetEvolve.Schemas.Xsd.SimpleTypes.Definitions;
using NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Core;
using NetEvolve.Schemas.Xsd.Validation.Core;
using NetEvolve.Schemas.Xsd.Validation.Entities;

/// <summary>
/// A root <c>r</c> that holds any number of <c>e</c> (the attributes ent and ents), and of the elements <c>n</c> and
/// <c>ns</c>, whose simple content is an ENTITY and an ENTITIES. The declarations of the document type reach the
/// listener the way the validator will hand them over: the doctype event of the reader.
/// </summary>
internal static class EntityFixture
{
    public const string Declarations =
        "<!NOTATION gif SYSTEM \"image/gif\">"
        + "<!ENTITY pic SYSTEM \"pic.gif\" NDATA gif>"
        + "<!ENTITY logo PUBLIC \"-//X//Logo\" \"logo.gif\" NDATA gif>"
        + "<!ENTITY text \"replacement\">"
        + "<!ENTITY ext SYSTEM \"ext.xml\">"
        + "<!ENTITY % pe \"x\">"
        + "<!ENTITY % pext SYSTEM \"pext.ent\">";

    public static SchemaFixture Schema(
        SimpleTypeDefinition? entity = null,
        SimpleTypeDefinition? entities = null,
        AttributeValueConstraint entConstraint = default
    )
    {
        entity ??= SchemaFixture.Simple("ENTITY");
        entities ??= SchemaFixture.Simple("ENTITIES");

        var schema = new SchemaFixture();
        var e = schema.Empty(Use(schema, "ent", entity, entConstraint), Use(schema, "ents", entities));
        schema.Global(
            "r",
            schema.ElementOnly(
                SchemaFixture.Group(
                    ModelGroupCompositor.Choice,
                    0,
                    long.MaxValue,
                    SchemaFixture.Part("e", e),
                    SchemaFixture.Part("n", entity),
                    SchemaFixture.Part("ns", entities)
                )
            )
        );
        return schema;
    }

    /// <summary>Wraps the content of the root element into a document with the given internal subset, none for <see langword="null"/>.</summary>
    public static string Document(string? subset, string content) =>
        (subset is null ? string.Empty : "<!DOCTYPE r [" + subset + "]>\n") + "<r>\n" + content + "</r>";

    /// <summary>Runs the instance with an <see cref="EntityListener"/>, with DTD processing on.</summary>
    public static IReadOnlyList<ValidationIssue> Run(SchemaFixture schema, string xml) =>
        Run(schema, xml, new ScriptedAssessor(), null);

    public static IReadOnlyList<ValidationIssue> Run(
        SchemaFixture schema,
        string xml,
        ISimpleValueAssessor assessor,
        EntityListener? listener
    )
    {
        listener ??= new EntityListener(assessor);
        var collector = new ValidationIssueCollector(ValidationLimits.Default);
        var sink = new DoctypeSink(
            new StreamingValidator(
                schema.Model(),
                assessor,
                new XsdIssueReporter(collector),
                ValidationLimits.Default,
                ValidationHarness.Source,
                [listener]
            ),
            listener
        );
        using var reader = XmlPipelineReader.Create(xml.AsSpan(), XmlReaderOptions.Default.WithDtdProcessing(true));
        var status = reader.Pump(ref sink);
        if (status != XmlReadStatus.Completed)
        {
            throw new InvalidOperationException("The instance is not well-formed: " + reader.Diagnostic?.Message);
        }

        return collector.ToResult().Issues;
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
