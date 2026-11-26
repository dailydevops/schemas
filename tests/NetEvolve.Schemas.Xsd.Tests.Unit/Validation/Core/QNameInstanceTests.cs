namespace NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Core;

using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Xsd.Components;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.Elements;
using NetEvolve.Schemas.Xsd.SimpleTypes.Definitions;

/// <summary>The prefixes of QName and NOTATION instance values are resolved with the namespace declarations in scope.</summary>
public class QNameInstanceTests
{
    private static readonly XsdIssueReporter Reporter = new(new ValidationIssueCollector(ValidationLimits.Default));

    private static SchemaFixture ElementOf(SimpleTypeDefinition type)
    {
        var schema = new SchemaFixture();
        schema.Global("r", type);
        return schema;
    }

    private static SimpleTypeDefinition Derived(string local) =>
        SimpleTypeDefinition.Restrict(
            default,
            SchemaFixture.Where,
            XsdDerivationMethod.None,
            SchemaFixture.Simple(local),
            [],
            Reporter
        )!;

    private static SimpleTypeDefinition ListOf(SimpleTypeDefinition item) =>
        SimpleTypeDefinition.List(default, SchemaFixture.Where, XsdDerivationMethod.None, item, [], Reporter)!;

    private static SimpleTypeDefinition UnionOf(params SimpleTypeDefinition[] members) =>
        SimpleTypeDefinition.Union(default, SchemaFixture.Where, XsdDerivationMethod.None, members, [], Reporter)!;

    private static async Task AssertResolveIssue(System.Collections.Generic.IReadOnlyList<ValidationIssue> issues)
    {
        await Assert.That(issues.Count).IsEqualTo(1);
        await Assert.That(issues[0].Code).IsEqualTo(ValidationHarness.Code("cvc-resolve-instance"));
        await Assert.That(issues[0].Message).Contains("not bound");
    }

    [Test]
    [Arguments("QName", "<r xmlns:p=\"urn:a\">p:x</r>")]
    [Arguments("QName", "<r xmlns:p=\"urn:a\"> p:x\n</r>")]
    [Arguments("QName", "<r>x</r>")]
    [Arguments("QName", "<r xmlns=\"urn:a\">x</r>")]
    [Arguments("QName", "<r>xml:lang</r>")]
    [Arguments("NOTATION", "<r xmlns:p=\"urn:a\">p:n</r>")]
    public async Task BoundOrAbsentPrefix_IsValid(string local, string xml)
    {
        var issues = ValidationHarness.Run(ElementOf(SchemaFixture.Simple(local)), xml);

        await Assert.That(issues.Count).IsEqualTo(0);
    }

    [Test]
    [Arguments("QName", "<r>p:x</r>")]
    [Arguments("NOTATION", "<r>p:n</r>")]
    [Arguments("QName", "<r xmlns:p=\"\">p:x</r>")]
    public async Task UnboundPrefix_IsReportedByCvcResolveInstance(string local, string xml)
    {
        var issues = ValidationHarness.Run(ElementOf(SchemaFixture.Simple(local)), xml);

        await AssertResolveIssue(issues);
    }

    [Test]
    public async Task Prefix_DeclaredOnAnAncestor_IsInScopeAndOutOfScopeAfterItsElement()
    {
        var schema = new SchemaFixture();
        var q = SchemaFixture.Simple("QName");
        schema.Global("r", schema.ElementOnly(SchemaFixture.Seq(SchemaFixture.Part("c", q, 0, long.MaxValue))));

        var inScope = ValidationHarness.Run(schema, "<r xmlns:p=\"urn:a\"><c>p:x</c></r>");
        var outOfScope = ValidationHarness.Run(schema, "<r><c xmlns:p=\"urn:a\">p:x</c><c>p:x</c></r>");

        await Assert.That(inScope.Count).IsEqualTo(0);
        await AssertResolveIssue(outOfScope);
    }

    [Test]
    public async Task TypeDerivedFromQName_ResolvesItsValue()
    {
        var schema = ElementOf(Derived("QName"));

        var bound = ValidationHarness.Run(schema, "<r xmlns:p=\"urn:a\">p:x</r>");
        var unbound = ValidationHarness.Run(schema, "<r>p:x</r>");

        await Assert.That(bound.Count).IsEqualTo(0);
        await AssertResolveIssue(unbound);
    }

    [Test]
    public async Task List_ResolvesEveryItem()
    {
        var schema = ElementOf(ListOf(SchemaFixture.Simple("QName")));

        var bound = ValidationHarness.Run(schema, "<r xmlns:p=\"urn:a\">p:x  y p:z</r>");
        var unbound = ValidationHarness.Run(schema, "<r xmlns:p=\"urn:a\">p:x q:y</r>");

        await Assert.That(bound.Count).IsEqualTo(0);
        await AssertResolveIssue(unbound);
    }

    [Test]
    public async Task Union_IsValidWhenAnyMemberAcceptsTheValue()
    {
        var schema = ElementOf(UnionOf(SchemaFixture.Simple("QName"), SchemaFixture.Simple("int")));

        var qname = ValidationHarness.Run(schema, "<r xmlns:p=\"urn:a\">p:x</r>");
        var number = ValidationHarness.Run(schema, "<r>42</r>");
        var neither = ValidationHarness.Run(schema, "<r>p:x</r>");

        await Assert.That(qname.Count).IsEqualTo(0);
        await Assert.That(number.Count).IsEqualTo(0);
        await AssertResolveIssue(neither);
    }

    [Test]
    public async Task Attribute_ResolvesItsValueWithTheDeclarationsOfItsElement()
    {
        var schema = new SchemaFixture();
        schema.Global("r", schema.Empty(schema.Use("a", "QName")));

        var bound = ValidationHarness.Run(schema, "<r a=\"p:x\" xmlns:p=\"urn:a\"/>");
        var unbound = ValidationHarness.Run(schema, "<r a=\"p:x\"/>");

        await Assert.That(bound.Count).IsEqualTo(0);
        await AssertResolveIssue(unbound);
    }

    [Test]
    public async Task DefaultValue_IsNotResolvedAgainstTheInstance()
    {
        var schema = new SchemaFixture();
        schema.Global("r", SchemaFixture.Simple("QName"), new(ElementValueConstraintKind.Default, "p:x"));

        var issues = ValidationHarness.Run(schema, "<r/>");

        await Assert.That(issues.Count).IsEqualTo(0);
    }
}
