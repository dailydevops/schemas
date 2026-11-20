namespace NetEvolve.Schemas.Xsd.Tests.Unit.IdentityConstraints.Evaluation;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xsd.Attributes.Declarations;
using NetEvolve.Schemas.Xsd.Components;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.Elements;
using NetEvolve.Schemas.Xsd.IdentityConstraints.Definitions;
using NetEvolve.Schemas.Xsd.IdentityConstraints.Evaluation;
using NetEvolve.Schemas.Xsd.SchemaDocuments;
using NetEvolve.Schemas.Xsd.SimpleTypes.Definitions;
using NetEvolve.Schemas.Xsd.Tests.Unit.Fakes;
using NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Core;
using NetEvolve.Schemas.Xsd.Validation.Core;

/// <summary>
/// A root <c>r</c> that holds any number of <c>e</c> and <c>s</c>; <c>s</c> holds any number of <c>e</c> and <c>s</c>; <c>e</c>
/// holds any number of <c>k</c>, <c>m</c>, and <c>q</c> and has the attributes <c>a</c>, <c>b</c>, and <c>c</c>. The leaf <c>k</c> and the attributes
/// have the simple type of the fixture, <c>m</c> is a built-in string, <c>q</c> has simple content, and <c>c</c> may have a default value.
/// </summary>
internal static class IdcFixture
{
    public const string Ns = "urn:t";

    public static SchemaFixture Schema(
        SimpleTypeDefinition? leaf = null,
        string ns = "",
        AttributeValueConstraint attributeC = default
    )
    {
        leaf ??= SchemaFixture.Simple("string");

        var schema = new SchemaFixture();
        var e = SchemaFixture.Element(
            "e",
            schema.ElementOnly(
                SchemaFixture.Group(
                    ModelGroupCompositor.Choice,
                    0,
                    long.MaxValue,
                    SchemaFixture.Part(SchemaFixture.Element("k", leaf, namespaceName: ns)),
                    SchemaFixture.Part(SchemaFixture.Element("m", BuiltIn("string"), namespaceName: ns)),
                    SchemaFixture.Part(SchemaFixture.Element("q", schema.SimpleContent("string"), namespaceName: ns))
                ),
                Use(schema, "a", leaf),
                Use(schema, "b", leaf),
                Use(schema, "c", leaf, attributeC)
            ),
            namespaceName: ns
        );
        var s = SchemaFixture.Element("s", null, namespaceName: ns);
        s.BindInlineType(
            schema.ElementOnly(
                SchemaFixture.Group(
                    ModelGroupCompositor.Choice,
                    0,
                    long.MaxValue,
                    SchemaFixture.Part(e),
                    SchemaFixture.Part(s)
                )
            )
        );
        schema.Global(
            "r",
            schema.ElementOnly(
                SchemaFixture.Group(
                    ModelGroupCompositor.Choice,
                    0,
                    long.MaxValue,
                    SchemaFixture.Part(e),
                    SchemaFixture.Part(s)
                )
            ),
            namespaceName: ns
        );
        return schema;
    }

    public static IXsdComponent BuiltIn(string name)
    {
        var types = XsdBuiltInTypes.For(XsdVersion.V11);
        for (var i = 0; i < types.Count; i++)
        {
            if (string.Equals(types[i].Name.LocalName, name, StringComparison.Ordinal))
            {
                return types[i];
            }
        }

        throw new InvalidOperationException(name);
    }

    public static string Unique(string name, string selector, params string[] fields) =>
        Constraint("unique", name, selector, null, fields);

    public static string Key(string name, string selector, params string[] fields) =>
        Constraint("key", name, selector, null, fields);

    public static string Keyref(string name, string refer, string selector, params string[] fields) =>
        Constraint("keyref", name, selector, refer, fields);

    public static IReadOnlyList<ValidationIssue> Run(
        SchemaFixture schema,
        string xml,
        IdentityConstraintListener listener,
        ISimpleValueAssessor? assessor = null
    ) => ValidationHarness.Run(schema, xml, assessor, listener);

    public static DiagnosticCode Code(string ruleId) => ValidationHarness.Code(ruleId);

    private static string Constraint(string kind, string name, string selector, string? refer, string[] fields)
    {
        var text = "<xs:" + kind + " name=\"" + name + "\"" + (refer is null ? "" : " refer=\"" + refer + "\"") + ">";
        text += "<xs:selector xpath=\"" + selector + "\"/>";
        foreach (var field in fields)
        {
            text += "<xs:field xpath=\"" + field + "\"/>";
        }

        return text + "</xs:" + kind + ">";
    }

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
