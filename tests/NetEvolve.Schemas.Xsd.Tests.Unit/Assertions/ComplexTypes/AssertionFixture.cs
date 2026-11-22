namespace NetEvolve.Schemas.Xsd.Tests.Unit.Assertions.ComplexTypes;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Reader.Pipeline;
using NetEvolve.Schemas.Xsd.Assertions.ComplexTypes;
using NetEvolve.Schemas.Xsd.Attributes.Declarations;
using NetEvolve.Schemas.Xsd.ComplexTypes.Definitions;
using NetEvolve.Schemas.Xsd.Components;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.Elements;
using NetEvolve.Schemas.Xsd.SchemaDocuments;
using NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Core;
using NetEvolve.Schemas.Xsd.Validation.Core;

/// <summary>
/// Schema documents with the assertions of one complex type <c>T</c>, and instances of a schema whose root <c>r</c> has the
/// children <c>a</c> and <c>b</c> (int), <c>s</c> (string), <c>e</c> (string with the default <c>x</c>), <c>c</c> (a type with the
/// child <c>n</c> and its own assertions), <c>m</c> (mixed), and <c>sc</c> (simple content of int), and the attributes <c>k</c>
/// (string) and <c>d</c> (int with the default 9). The datatype fake takes digits for int.
/// </summary>
internal static class AssertionFixture
{
    internal const string Tns = "urn:t";

    internal static readonly SchemaNamespaceScope Scope = new(Tns, false);

    /// <summary>A schema document with one complex type <c>T</c> that holds the given <c>assert</c> elements.</summary>
    internal static string Schema(string schemaAttributes, params string[] asserts) =>
        "<xs:schema xmlns:xs=\""
        + SchemaFixture.Xs
        + "\" targetNamespace=\""
        + Tns
        + "\""
        + schemaAttributes
        + "><xs:complexType name=\"T\"><xs:sequence/>"
        + string.Concat(asserts)
        + "</xs:complexType></xs:schema>";

    /// <summary>An <c>assert</c> element with the given attributes.</summary>
    internal static string Assert(string test, string attributes = "") =>
        "<xs:assert test=\"" + test.Replace("\"", "&quot;", StringComparison.Ordinal) + "\"" + attributes + "/>";

    /// <summary>The <c>xpathDefaultNamespace</c> attribute with the given value.</summary>
    internal static string Default(string value) => " xpathDefaultNamespace=\"" + value + "\"";

    internal static IReadOnlyList<SchemaSyntaxNode> AssertNodes(string schema)
    {
        var reporter = new XsdIssueReporter(new ValidationIssueCollector(ValidationLimits.Default));
        var type = SchemaDocumentParser.Parse(schema.AsSpan(), "file:///t.xsd", reporter).Root!.GetChildNode(0);
        var nodes = new List<SchemaSyntaxNode>();
        for (var i = 0; i < type.ChildCount; i++)
        {
            if (string.Equals(type.GetChildNode(i).Name.LocalName, "assert", StringComparison.Ordinal))
            {
                nodes.Add(type.GetChildNode(i));
            }
        }

        return nodes;
    }

    /// <summary>Compiles every <c>assert</c> of a schema document, which must compile.</summary>
    internal static ComplexTypeAssertion[] Compile(string schema)
    {
        var reporter = new XsdIssueReporter(new ValidationIssueCollector(ValidationLimits.Default));
        var result = new List<ComplexTypeAssertion>();
        foreach (var node in AssertNodes(schema))
        {
            result.Add(ComplexTypeAssertion.TryCreate(node, Scope, XsdVersion.V11, reporter)!);
        }

        return [.. result];
    }

    /// <summary>Compiles the tests of a type, with the given attributes on the schema element and on each <c>assert</c>.</summary>
    internal static ComplexTypeAssertion[] Compile(
        string[] tests,
        string schemaAttributes = "",
        string assertAttributes = ""
    )
    {
        var asserts = new string[tests.Length];
        for (var i = 0; i < tests.Length; i++)
        {
            asserts[i] = Assert(tests[i], assertAttributes);
        }

        return Compile(Schema(schemaAttributes, asserts));
    }

    /// <summary>Compiles one <c>assert</c> and returns the issues of the attempt.</summary>
    internal static (ComplexTypeAssertion? Assertion, IReadOnlyList<ValidationIssue> Issues) TryCompile(
        string test,
        string schemaAttributes = "",
        string assertAttributes = ""
    )
    {
        var collector = new ValidationIssueCollector(ValidationLimits.Default);
        var node = AssertNodes(Schema(schemaAttributes, Assert(test, assertAttributes)))[0];
        var assertion = ComplexTypeAssertion.TryCreate(node, Scope, XsdVersion.V11, new XsdIssueReporter(collector));
        return (assertion, collector.ToResult().Issues);
    }

    /// <summary>The instance schema: the type <c>r</c> carries <paramref name="rootTests"/>, the type of <c>c</c> carries <paramref name="childTests"/>.</summary>
    internal static InstanceSchema Instance(
        string[] rootTests,
        string[] childTests,
        string ns = "",
        string schemaAttributes = "",
        string assertAttributes = "",
        string[]? simpleTests = null
    )
    {
        var schema = new SchemaFixture();
        var simple = schema.SimpleContent("int");
        var child = ComplexTypeDefinition.Implicit(
            SchemaFixture.Header("C"),
            false,
            SchemaFixture.Seq(Part("n", SchemaFixture.Simple("int"), ns)),
            [],
            null,
            schema.Reporter
        );
        var root = RootType(schema, child, simple, ns);
        schema.Global("r", root, namespaceName: ns);
        var own = new Dictionary<ComplexTypeDefinition, IReadOnlyList<ComplexTypeAssertion>>();
        if (rootTests.Length > 0)
        {
            own[root] = Compile(rootTests, schemaAttributes, assertAttributes);
        }

        if (childTests.Length > 0)
        {
            own[child] = Compile(childTests, schemaAttributes, assertAttributes);
        }

        if (simpleTests is { Length: > 0 })
        {
            own[simple] = Compile(simpleTests, schemaAttributes, assertAttributes);
        }

        return new InstanceSchema(schema, new ComplexTypeAssertions(own));
    }

    private static ComplexTypeDefinition RootType(
        SchemaFixture schema,
        ComplexTypeDefinition child,
        ComplexTypeDefinition simple,
        string ns
    ) =>
        ComplexTypeDefinition.Implicit(
            SchemaFixture.Header("R"),
            false,
            SchemaFixture.Group(
                ModelGroupCompositor.Choice,
                0,
                long.MaxValue,
                Part("a", SchemaFixture.Simple("int"), ns),
                Part("b", SchemaFixture.Simple("int"), ns),
                Part("s", SchemaFixture.Simple("string"), ns),
                SchemaFixture.Part(
                    SchemaFixture.Element(
                        "e",
                        SchemaFixture.Simple("string"),
                        constraint: new ElementValueConstraint(ElementValueConstraintKind.Default, "x"),
                        namespaceName: ns
                    )
                ),
                Part("c", child, ns),
                Part("m", schema.Mixed(SchemaFixture.Seq()), ns),
                Part("sc", simple, ns)
            ),
            [
                schema.Use("k"),
                schema.Use(
                    "d",
                    "int",
                    constraint: new AttributeValueConstraint(AttributeValueConstraintKind.Default, "9")
                ),
            ],
            null,
            schema.Reporter
        );

    /// <summary>
    /// The root <c>r</c> has a type <c>D</c> that extends <c>B</c>, whose content allows any number of <c>a</c> and <c>b</c>;
    /// <paramref name="baseTests"/> belong to <c>B</c> and <paramref name="derivedTests"/> to <c>D</c>.
    /// </summary>
    internal static InstanceSchema Derived(string[] baseTests, string[] derivedTests)
    {
        var schema = new SchemaFixture();
        var baseType = ComplexTypeDefinition.Implicit(
            SchemaFixture.Header("B"),
            false,
            SchemaFixture.Group(
                ModelGroupCompositor.Choice,
                0,
                long.MaxValue,
                Part("a", SchemaFixture.Simple("int"), string.Empty),
                Part("b", SchemaFixture.Simple("int"), string.Empty)
            ),
            [],
            null,
            schema.Reporter
        );
        var derived = ComplexTypeDefinition.FromComplexContent(
            SchemaFixture.Header("D"),
            XsdDerivationMethod.Extension,
            baseType,
            false,
            null,
            [],
            null,
            schema.Reporter
        )!;
        schema.Global("r", derived);
        var own = new Dictionary<ComplexTypeDefinition, IReadOnlyList<ComplexTypeAssertion>>();
        if (baseTests.Length > 0)
        {
            own[baseType] = Compile(baseTests);
        }

        if (derivedTests.Length > 0)
        {
            own[derived] = Compile(derivedTests);
        }

        return new InstanceSchema(schema, new ComplexTypeAssertions(own));
    }

    internal static Particle Part(string name, IXsdComponent type, string ns) =>
        SchemaFixture.Part(SchemaFixture.Element(name, type, namespaceName: ns));

    /// <summary>Validates an instance with the listener and returns the issues; the instance starts with a line break, so <c>r</c> is on line 2.</summary>
    internal static IReadOnlyList<ValidationIssue> Run(
        InstanceSchema instance,
        string xml,
        ValidationLimits? limits = null
    )
    {
        limits ??= ValidationLimits.Default;
        var assessor = new ScriptedAssessor();
        var collector = new ValidationIssueCollector(limits);
        var validator = new StreamingValidator(
            instance.Schema.Model(),
            assessor,
            new XsdIssueReporter(collector),
            limits,
            ValidationHarness.Source,
            [new AssertionListener(instance.Assertions, assessor)]
        );
        using var reader = XmlPipelineReader.Create(xml.AsSpan());
        return reader.Pump(ref validator) == XmlReadStatus.Completed
            ? collector.ToResult().Issues
            : throw new InvalidOperationException("The instance is not well-formed.");
    }

    /// <summary>Runs <paramref name="rootTests"/> against the root of an instance without namespaces.</summary>
    internal static IReadOnlyList<ValidationIssue> Run(string xml, params string[] rootTests) =>
        Run(Instance(rootTests, []), xml);

    /// <summary>A schema and the assertions of its types.</summary>
    internal sealed class InstanceSchema(SchemaFixture schema, ComplexTypeAssertions assertions)
    {
        internal SchemaFixture Schema { get; } = schema;

        internal ComplexTypeAssertions Assertions { get; } = assertions;
    }
}
