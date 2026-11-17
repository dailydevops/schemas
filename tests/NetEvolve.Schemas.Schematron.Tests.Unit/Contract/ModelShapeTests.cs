namespace NetEvolve.Schemas.Schematron.Tests.Unit.Contract;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NetEvolve.Schemas.Schematron.Model;
using NetEvolve.Schemas.Schematron.Tests.Unit.Fakes;

public class ModelShapeTests
{
    private static readonly string[] ObservableTypes =
    [
        "SchematronActive",
        "SchematronAssert",
        "SchematronAssertion",
        "SchematronContainer",
        "SchematronDiagnostic",
        "SchematronDiagnostics",
        "SchematronDirection",
        "SchematronEdition",
        "SchematronEmphasis",
        "SchematronGroup",
        "SchematronLet",
        "SchematronMessage",
        "SchematronMessagePart",
        "SchematronName",
        "SchematronNamespace",
        "SchematronNode",
        "SchematronParagraph",
        "SchematronParameter",
        "SchematronPattern",
        "SchematronPatternBase",
        "SchematronPhase",
        "SchematronProperties",
        "SchematronProperty",
        "SchematronReport",
        "SchematronRichText",
        "SchematronRule",
        "SchematronSchema",
        "SchematronSpan",
        "SchematronText",
        "SchematronTitle",
        "SchematronValueOf",
    ];

    private static readonly string[] InternalTypes =
    [
        "SchematronExtends",
        "SchematronForeignElement",
        "SchematronInclude",
        "SchematronLibrary",
        "SchematronRules",
    ];

    private static Type[] ModelTypes() =>
        [
            .. typeof(SchematronNode)
                .Assembly.GetTypes()
                .Where(type =>
                    string.Equals(type.Namespace, typeof(SchematronNode).Namespace, StringComparison.Ordinal)
                ),
        ];

    [Test]
    public async Task Model_PublicTypes_AreExactlyTheObservableSet()
    {
        var publicNames = ModelTypes()
            .Where(type => type.IsPublic)
            .Select(type => type.Name)
            .OrderBy(name => name, StringComparer.Ordinal);

        await Assert.That(publicNames).IsEquivalentTo(ObservableTypes);
    }

    [Test]
    public async Task Model_ParserIntermediates_AreInternal()
    {
        var internalNames = ModelTypes().Where(type => !type.IsPublic).Select(type => type.Name);

        await Assert.That(internalNames).IsEquivalentTo(InternalTypes);
    }

    [Test]
    public async Task Model_HasOneSealedTypePerElement()
    {
        var elements = new Dictionary<string, Type>(StringComparer.Ordinal)
        {
            ["schema"] = typeof(SchematronSchema),
            ["ns"] = typeof(SchematronNamespace),
            ["title"] = typeof(SchematronTitle),
            ["p"] = typeof(SchematronParagraph),
            ["param"] = typeof(SchematronParameter),
            ["let"] = typeof(SchematronLet),
            ["phase"] = typeof(SchematronPhase),
            ["active"] = typeof(SchematronActive),
            ["pattern"] = typeof(SchematronPattern),
            ["group"] = typeof(SchematronGroup),
            ["rule"] = typeof(SchematronRule),
            ["assert"] = typeof(SchematronAssert),
            ["report"] = typeof(SchematronReport),
            ["diagnostics"] = typeof(SchematronDiagnostics),
            ["diagnostic"] = typeof(SchematronDiagnostic),
            ["properties"] = typeof(SchematronProperties),
            ["property"] = typeof(SchematronProperty),
            ["name"] = typeof(SchematronName),
            ["value-of"] = typeof(SchematronValueOf),
            ["emph"] = typeof(SchematronEmphasis),
            ["dir"] = typeof(SchematronDirection),
            ["span"] = typeof(SchematronSpan),
        };

        foreach (var type in elements.Values)
        {
            await Assert.That(type.IsSealed).IsTrue();
        }

        await Assert.That(elements.Values.Distinct().Count()).IsEqualTo(elements.Count);
    }

    [Test]
    public async Task Model_EveryNode_DerivesFromSchematronNode_WithDocumentLocation()
    {
        foreach (
            var type in ModelTypes().Where(type => type != typeof(SchematronEdition) && !IsCompilerGenerated(type))
        )
        {
            await Assert.That(typeof(SchematronNode).IsAssignableFrom(type)).IsTrue();
        }

        var location = typeof(SchematronNode).GetProperty(nameof(SchematronNode.Location))!;

        await Assert.That(location.PropertyType).IsEqualTo(typeof(DocumentLocation));
        await Assert.That(typeof(SchematronNode).IsAbstract).IsTrue();
    }

    [Test]
    public async Task Model_ConstructorsAreInternal()
    {
        foreach (var type in ModelTypes().Where(type => type.IsClass && !IsCompilerGenerated(type)))
        {
            var publicConstructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
            var protectedConstructors = type.GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(constructor => constructor.IsFamily || constructor.IsFamilyOrAssembly);

            await Assert.That(publicConstructors.Length).IsEqualTo(0);
            await Assert.That(protectedConstructors.Count()).IsEqualTo(0);
        }
    }

    [Test]
    public async Task Model_ChildLists_AreReadOnly()
    {
        foreach (var type in ModelTypes().Where(type => type.IsPublic && type.IsClass))
        {
            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                await Assert.That(property.PropertyType.IsArray).IsFalse();
                if (property.PropertyType.IsGenericType)
                {
                    var definition = property.PropertyType.GetGenericTypeDefinition();

                    await Assert
                        .That(definition == typeof(IReadOnlyList<>) || definition == typeof(Nullable<>))
                        .IsTrue();
                }

                await Assert.That(property.CanWrite).IsFalse();
            }
        }
    }

    [Test]
    public async Task Model_NoTypeNamedAssert() =>
        await Assert
            .That(ModelTypes().Any(type => string.Equals(type.Name, "Assert", StringComparison.Ordinal)))
            .IsFalse();

    [Test]
    public async Task Model_PatternAndGroup_ShareBase()
    {
        await Assert.That(typeof(SchematronPattern).BaseType).IsEqualTo(typeof(SchematronPatternBase));
        await Assert.That(typeof(SchematronGroup).BaseType).IsEqualTo(typeof(SchematronPatternBase));
    }

    [Test]
    public async Task Model_SchemaAndLibrary_ShareContainer()
    {
        await Assert.That(typeof(SchematronSchema).BaseType).IsEqualTo(typeof(SchematronContainer));
        await Assert
            .That(
                typeof(SchematronNode)
                    .Assembly.GetType("NetEvolve.Schemas.Schematron.Model.SchematronLibrary")!
                    .BaseType
            )
            .IsEqualTo(typeof(SchematronContainer));
    }

    [Test]
    public async Task Model_RichTextOwners_ShareBase()
    {
        foreach (
            var type in new[]
            {
                typeof(SchematronTitle),
                typeof(SchematronParagraph),
                typeof(SchematronActive),
                typeof(SchematronMessage),
            }
        )
        {
            await Assert.That(type.BaseType).IsEqualTo(typeof(SchematronRichText));
        }

        await Assert.That(typeof(SchematronAssert).BaseType).IsEqualTo(typeof(SchematronAssertion));
        await Assert.That(typeof(SchematronReport).BaseType).IsEqualTo(typeof(SchematronAssertion));
    }

    [Test]
    public async Task Edition_EnumValues_AreIso2016Zero_Iso2025One()
    {
        var iso2016 = SchematronEdition.Iso2016;
        var iso2025 = SchematronEdition.Iso2025;
        await Assert.That((int)iso2016).IsEqualTo(0);
        await Assert.That((int)iso2025).IsEqualTo(1);
    }

    private static readonly DocumentLocation At = new("a.sch", 3, 4);
    private static readonly DocumentLocation ExpandedFrom = new("b.sch", 9, 1);

    private static SchematronRule BuildRule()
    {
        var message = FakeModel.Message("text");
        var assertion = new SchematronAssert(At, "x", "a1", ["f"], "warn", "high", "..", ["d1"], ["p1"], message);
        var report = new SchematronReport(At, "y", null, [], null, null, null, [], [], message);

        return new SchematronRule(
            At,
            "r1",
            "a",
            "b",
            ["f"],
            "role",
            "subj",
            false,
            new SchematronTitle(At, []),
            [],
            [assertion, report],
            ExpandedFrom
        );
    }

    private static SchematronPattern BuildPattern() =>
        new(
            At,
            "p",
            true,
            "base",
            "doc",
            "role",
            null,
            [],
            [new SchematronParameter(At, "n", "'v'")],
            [new SchematronLet(At, "l", "xs:string", "'x'")],
            [BuildRule()],
            ExpandedFrom
        );

    private static SchematronSchema BuildSchema()
    {
        var message = FakeModel.Message("text");
        var group = new SchematronGroup(At, "g", false, null, null, null, null, [], [], [], [BuildRule()], null);
        var diagnostics = new SchematronDiagnostics(At, [new SchematronDiagnostic(At, "d1", "r", message)]);
        var properties = new SchematronProperties(At, [new SchematronProperty(At, "p1", "r", "s", message)]);
        var phase = new SchematronPhase(
            At,
            "ph",
            "from",
            "when",
            [new SchematronParagraph(At, [], "pid", "cls", "ico")],
            [],
            [new SchematronActive(At, [new SchematronText(At, "t")], "p")]
        );

        return new SchematronSchema(
            At,
            new SchematronTitle(At, [new SchematronName(At, "path"), new SchematronValueOf(At, "sel")]),
            [],
            [],
            [],
            [BuildPattern(), group],
            diagnostics,
            properties,
            "id",
            "1.0",
            "2025",
            SchematronEdition.Iso2025,
            "ph",
            "xslt2",
            [new SchematronNamespace(At, "x", "urn:x")],
            [phase],
            [],
            []
        );
    }

    [Test]
    public async Task Model_Schema_RoundTripsItsMembers()
    {
        var schema = BuildSchema();

        await Assert.That(schema.Location).IsEqualTo(At);
        await Assert.That(schema.Edition).IsEqualTo(SchematronEdition.Iso2025);
        await Assert.That(schema.EditionText).IsEqualTo("2025");
        await Assert.That(schema.Id).IsEqualTo("id");
        await Assert.That(schema.SchemaVersion).IsEqualTo("1.0");
        await Assert.That(schema.DefaultPhase).IsEqualTo("ph");
        await Assert.That(schema.QueryBinding).IsEqualTo("xslt2");
        await Assert.That(schema.Namespaces[0].Prefix).IsEqualTo("x");
        await Assert.That(schema.Namespaces[0].NamespaceName).IsEqualTo("urn:x");
        await Assert.That(schema.Phases[0].From).IsEqualTo("from");
        await Assert.That(schema.Phases[0].When).IsEqualTo("when");
        await Assert.That(schema.Phases[0].Actives[0].Pattern).IsEqualTo("p");
        await Assert.That(schema.Phases[0].Paragraphs[0].Id).IsEqualTo("pid");
        await Assert.That(schema.Phases[0].Paragraphs[0].StyleClass).IsEqualTo("cls");
        await Assert.That(schema.Phases[0].Paragraphs[0].Icon).IsEqualTo("ico");
        await Assert.That(schema.Patterns.Count).IsEqualTo(2);
        await Assert.That(schema.Patterns[0].IsGroup).IsFalse();
        await Assert.That(schema.Patterns[1].IsGroup).IsTrue();
        await Assert.That(schema.Diagnostics!.Items[0].Id).IsEqualTo("d1");
        await Assert.That(schema.Diagnostics!.Items[0].Role).IsEqualTo("r");
        await Assert.That(schema.Properties!.Items[0].Scheme).IsEqualTo("s");
        await Assert.That(schema.Properties!.Items[0].Role).IsEqualTo("r");
        await Assert.That(schema.Title!.Parts.Count).IsEqualTo(2);
        await Assert.That(((SchematronName)schema.Title.Parts[0]).Path).IsEqualTo("path");
        await Assert.That(((SchematronValueOf)schema.Title.Parts[1]).Select).IsEqualTo("sel");
    }

    [Test]
    public async Task Model_Pattern_RoundTripsItsMembers()
    {
        var first = BuildPattern();

        await Assert.That(first.IsAbstract).IsTrue();
        await Assert.That(first.IsA).IsEqualTo("base");
        await Assert.That(first.Documents).IsEqualTo("doc");
        await Assert.That(first.Role).IsEqualTo("role");
        await Assert.That(first.Parameters[0].Name).IsEqualTo("n");
        await Assert.That(first.Parameters[0].Value).IsEqualTo("'v'");
        await Assert.That(first.Lets[0].DeclaredType).IsEqualTo("xs:string");
        await Assert.That(first.Lets[0].Value).IsEqualTo("'x'");
        await Assert.That(first.Lets[0].Name).IsEqualTo("l");
        await Assert.That(first.ExpandedFrom).IsEqualTo(ExpandedFrom);
    }

    [Test]
    public async Task Model_RuleAndAssertions_RoundTripTheirMembers()
    {
        var rule = BuildRule();

        await Assert.That(rule.Id).IsEqualTo("r1");
        await Assert.That(rule.Context).IsEqualTo("a");
        await Assert.That(rule.VisitEach).IsEqualTo("b");
        await Assert.That(rule.Flags[0]).IsEqualTo("f");
        await Assert.That(rule.Role).IsEqualTo("role");
        await Assert.That(rule.Subject).IsEqualTo("subj");
        await Assert.That(rule.IsAbstract).IsFalse();
        await Assert.That(rule.Title).IsNotNull();
        await Assert.That(rule.Items.Count).IsEqualTo(2);

        var assertion = (SchematronAssert)rule.Items[0];
        await Assert.That(assertion.Test).IsEqualTo("x");
        await Assert.That(assertion.Id).IsEqualTo("a1");
        await Assert.That(assertion.Severity).IsEqualTo("high");
        await Assert.That(assertion.Subject).IsEqualTo("..");
        await Assert.That(assertion.DiagnosticIds[0]).IsEqualTo("d1");
        await Assert.That(assertion.PropertyIds[0]).IsEqualTo("p1");
        await Assert.That(assertion.Role).IsEqualTo("warn");
        await Assert.That(assertion.Message.Parts.Count).IsEqualTo(1);
        await Assert.That(((SchematronText)assertion.Message.Parts[0]).Text).IsEqualTo("text");
        await Assert.That(rule.Items[1]).IsTypeOf<SchematronReport>();
    }

    [Test]
    public async Task Model_InlineParts_RoundTripTheirMembers()
    {
        var at = FakeModel.Origin;
        var inner = new SchematronText(at, "x");
        var emphasis = new SchematronEmphasis(at, [inner]);
        var direction = new SchematronDirection(at, "rtl", [inner]);
        var span = new SchematronSpan(at, "cls", [inner]);

        await Assert.That(emphasis.Parts[0]).IsSameReferenceAs(inner);
        await Assert.That(direction.Value).IsEqualTo("rtl");
        await Assert.That(direction.Parts[0]).IsSameReferenceAs(inner);
        await Assert.That(span.StyleClass).IsEqualTo("cls");
        await Assert.That(span.Parts[0]).IsSameReferenceAs(inner);
    }

    [Test]
    public async Task Model_ParserIntermediates_RoundTripTheirMembers()
    {
        var at = FakeModel.Origin;
        var include = new SchematronInclude(at, "lib.sch");
        var extends = new SchematronExtends(at, "r1", "lib.sch");
        var rules = new SchematronRules(at, "rs", null, [], []);
        var foreign = new SchematronForeignElement(
            at,
            "urn:x",
            "e",
            [new KeyValuePair<string, string>("a", "1")],
            [new SchematronText(at, "t")]
        );
        var library = new SchematronLibrary(at, null, [], [], [], [], null, null, [rules], [foreign]);

        await Assert.That(include.Href).IsEqualTo("lib.sch");
        await Assert.That(extends.Rule).IsEqualTo("r1");
        await Assert.That(extends.Href).IsEqualTo("lib.sch");
        await Assert.That(rules.Id).IsEqualTo("rs");
        await Assert.That(rules.Title).IsNull();
        await Assert.That(rules.Paragraphs.Count).IsEqualTo(0);
        await Assert.That(rules.Rules.Count).IsEqualTo(0);
        await Assert.That(library.Patterns.Count).IsEqualTo(0);
        await Assert.That(library.Rules[0]).IsSameReferenceAs(rules);
        await Assert.That(library.ForeignElements[0]).IsSameReferenceAs(foreign);
        await Assert.That(foreign.NamespaceUri).IsEqualTo("urn:x");
        await Assert.That(foreign.LocalName).IsEqualTo("e");
        await Assert.That(foreign.Attributes[0].Value).IsEqualTo("1");
        await Assert.That(foreign.Children.Count).IsEqualTo(1);
    }

    private static bool IsCompilerGenerated(Type type) =>
        type.GetCustomAttribute<System.Runtime.CompilerServices.CompilerGeneratedAttribute>() is not null;
}
