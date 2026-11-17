namespace NetEvolve.Schemas.Xsd.Tests.Unit.Attributes.Declarations;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.Attributes.Declarations;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.SimpleTypes.Definitions;
using NetEvolve.Schemas.Xsd.Tests.Unit.Fakes;

public class AttributeDeclarationTests
{
    private const string Tns = "urn:test";
    private static readonly DocumentLocation Where = new("file:///a.xsd", 4, 2);

    private static readonly SimpleTypeDefinition StringType = SimpleTypeDefinition.FromDatatype(
        new XPathQName("http://www.w3.org/2001/XMLSchema", "string"),
        Where,
        new SimpleTypes.Definitions.FakeDatatype("string", DatatypeVariety.Atomic)
    );

    private readonly ValidationIssueCollector _collector = new(ValidationLimits.Default);

    private XsdIssueReporter Reporter => new(_collector);

    private AttributeDeclaration Global(string name = "a", AttributeValueConstraint constraint = default) =>
        AttributeDeclaration.Global(name, Tns, Where, StringType, constraint, Reporter)!;

    private AttributeUse Use(
        AttributeUseKind kind,
        AttributeDeclaration declaration,
        AttributeValueConstraint constraint = default
    ) => AttributeUse.Create(Where, kind, declaration, constraint, Reporter)!;

    [Test]
    public async Task Global_HasTheTargetNamespaceAndTheType()
    {
        var declaration = Global("a", new AttributeValueConstraint(AttributeValueConstraintKind.Default, "x"));

        await Assert.That(declaration.Kind).IsEqualTo(XsdComponentKind.AttributeDeclaration);
        await Assert.That(declaration.Name).IsEqualTo(new XPathQName(Tns, "a"));
        await Assert.That(declaration.IsGlobal).IsTrue();
        await Assert.That(declaration.TypeDefinition).IsSameReferenceAs(StringType);
        await Assert.That(declaration.ValueConstraint.Kind).IsEqualTo(AttributeValueConstraintKind.Default);
        await Assert.That(declaration.ValueConstraint.Value).IsEqualTo("x");
        await Assert.That(declaration.Location.Line).IsEqualTo(4);
    }

    [Test]
    public async Task Local_Qualified_PutsTheNameInTheTargetNamespace()
    {
        var declaration = AttributeDeclaration.Local(
            "a",
            Tns,
            AttributeForm.Qualified,
            Where,
            StringType,
            default,
            Reporter
        )!;

        await Assert.That(declaration.Name).IsEqualTo(new XPathQName(Tns, "a"));
        await Assert.That(declaration.IsGlobal).IsFalse();
    }

    [Test]
    public async Task Local_Unqualified_PutsTheNameInNoNamespace()
    {
        var declaration = AttributeDeclaration.Local(
            "a",
            Tns,
            AttributeForm.Unqualified,
            Where,
            StringType,
            default,
            Reporter
        )!;

        await Assert.That(declaration.Name).IsEqualTo(new XPathQName(string.Empty, "a"));
    }

    [Test]
    public async Task Global_NamedXmlns_IsAnIssue()
    {
        var declaration = AttributeDeclaration.Global("xmlns", Tns, Where, StringType, default, Reporter);

        await Assert.That(declaration).IsNull();
        await AssertSingleError(XsdRuleCodes.NoXmlns, 4);
    }

    [Test]
    public async Task Global_InTheInstanceNamespace_IsAnIssue()
    {
        var declaration = AttributeDeclaration.Global(
            "type",
            "http://www.w3.org/2001/XMLSchema-instance",
            Where,
            StringType,
            default,
            Reporter
        );

        await Assert.That(declaration).IsNull();
        await AssertSingleError(XsdRuleCodes.NoXsi, 4);
    }

    [Test]
    public async Task Use_Required_IsRequiredAndNotProhibited()
    {
        var use = Use(AttributeUseKind.Required, Global());

        await Assert.That(use.Kind).IsEqualTo(XsdComponentKind.AttributeUse);
        await Assert.That(use.Required).IsTrue();
        await Assert.That(use.Prohibited).IsFalse();
        await Assert.That(use.Name).IsEqualTo(new XPathQName(Tns, "a"));
    }

    [Test]
    public async Task Use_Optional_IsNeitherRequiredNorProhibited()
    {
        var use = Use(AttributeUseKind.Optional, Global());

        await Assert.That(use.Required).IsFalse();
        await Assert.That(use.Prohibited).IsFalse();
    }

    [Test]
    public async Task Use_Prohibited_IsProhibited()
    {
        var use = Use(AttributeUseKind.Prohibited, Global());

        await Assert.That(use.Required).IsFalse();
        await Assert.That(use.Prohibited).IsTrue();
    }

    [Test]
    public async Task Use_Default_IsTheEffectiveValueConstraint()
    {
        var use = Use(
            AttributeUseKind.Optional,
            Global(),
            new AttributeValueConstraint(AttributeValueConstraintKind.Default, "d")
        );

        await Assert.That(use.ValueConstraint.Kind).IsEqualTo(AttributeValueConstraintKind.Default);
        await Assert.That(use.ValueConstraint.Value).IsEqualTo("d");
    }

    [Test]
    public async Task Use_Fixed_IsTheEffectiveValueConstraint()
    {
        var use = Use(
            AttributeUseKind.Required,
            Global(),
            new AttributeValueConstraint(AttributeValueConstraintKind.Fixed, "f")
        );

        await Assert.That(use.ValueConstraint.Kind).IsEqualTo(AttributeValueConstraintKind.Fixed);
        await Assert.That(use.ValueConstraint.Value).IsEqualTo("f");
    }

    [Test]
    public async Task Use_WithoutOwnConstraint_TakesTheDeclarationConstraint()
    {
        var declaration = Global("a", new AttributeValueConstraint(AttributeValueConstraintKind.Fixed, "f"));

        var use = Use(AttributeUseKind.Optional, declaration);

        await Assert.That(use.ValueConstraint.Kind).IsEqualTo(AttributeValueConstraintKind.Fixed);
        await Assert.That(use.ValueConstraint.Value).IsEqualTo("f");
    }

    [Test]
    public async Task Use_FixedDifferentFromTheDeclarationFixed_IsAnIssue()
    {
        var declaration = Global("a", new AttributeValueConstraint(AttributeValueConstraintKind.Fixed, "f"));

        var use = AttributeUse.Create(
            Where,
            AttributeUseKind.Optional,
            declaration,
            new AttributeValueConstraint(AttributeValueConstraintKind.Fixed, "g"),
            Reporter
        );

        await Assert.That(use).IsNull();
        await AssertSingleError(XsdRuleCodes.AuPropsCorrect, 4);
    }

    [Test]
    public async Task Use_DefaultOnAFixedDeclaration_IsAnIssue()
    {
        var declaration = Global("a", new AttributeValueConstraint(AttributeValueConstraintKind.Fixed, "f"));

        var use = AttributeUse.Create(
            Where,
            AttributeUseKind.Optional,
            declaration,
            new AttributeValueConstraint(AttributeValueConstraintKind.Default, "f"),
            Reporter
        );

        await Assert.That(use).IsNull();
        await AssertSingleError(XsdRuleCodes.AuPropsCorrect, 4);
    }

    [Test]
    public async Task Group_SameDeclarationTwiceDirectly_IsASchemaError()
    {
        var shared = Global("a");
        var group = AttributeGroupDefinition.Create(
            new XPathQName(Tns, "g"),
            Where,
            [Use(AttributeUseKind.Optional, shared), Use(AttributeUseKind.Optional, shared)],
            [],
            null
        );

        group.Resolve(new FakeComponentRegistry([]), Reporter);

        await AssertSingleError(XsdRuleCodes.AgPropsCorrect, 4);
        await Assert.That(group.Uses).Count().IsEqualTo(1);
    }

    [Test]
    public async Task Issues_UnknownCode_Throws() =>
        await Assert
            .That(() => Reporter.Report(new DiagnosticCode(9999), Where, "m"))
            .Throws<InvalidOperationException>();

    [Test]
    public async Task Use_DefaultWithRequired_IsAnIssue()
    {
        var use = AttributeUse.Create(
            Where,
            AttributeUseKind.Required,
            Global(),
            new AttributeValueConstraint(AttributeValueConstraintKind.Default, "d"),
            Reporter
        );

        await Assert.That(use).IsNull();
        await AssertSingleError(XsdRuleCodes.SrcAttribute, 4);
    }

    [Test]
    public async Task Use_DefaultWithProhibited_IsAnIssue()
    {
        var use = AttributeUse.Create(
            Where,
            AttributeUseKind.Prohibited,
            Global(),
            new AttributeValueConstraint(AttributeValueConstraintKind.Default, "d"),
            Reporter
        );

        await Assert.That(use).IsNull();
        await AssertSingleError(XsdRuleCodes.SrcAttribute, 4);
    }

    [Test]
    public async Task Group_CollectsDirectUsesAndTheUsesOfNestedGroups()
    {
        var registry = new FakeComponentRegistry([]);
        var inner = AttributeGroupDefinition.Create(
            new XPathQName(Tns, "inner"),
            Where,
            [Use(AttributeUseKind.Optional, Global("b"))],
            [],
            null
        );
        registry.Add(inner);
        var outer = AttributeGroupDefinition.Create(
            new XPathQName(Tns, "outer"),
            Where,
            [Use(AttributeUseKind.Optional, Global("a"))],
            [new AttributeGroupReference(new XPathQName(Tns, "inner"), Where)],
            null
        );

        outer.Resolve(registry, Reporter);

        await Assert.That(outer.Kind).IsEqualTo(XsdComponentKind.AttributeGroup);
        await Assert.That(outer.Uses).Count().IsEqualTo(2);
        await Assert.That(outer.Uses[0].Name.LocalName).IsEqualTo("a");
        await Assert.That(outer.Uses[1].Name.LocalName).IsEqualTo("b");
        await Assert.That(_collector.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Group_SelfReference_IsALocatedSchemaError()
    {
        var registry = new FakeComponentRegistry([]);
        var self = AttributeGroupDefinition.Create(
            new XPathQName(Tns, "g"),
            Where,
            [],
            [new AttributeGroupReference(new XPathQName(Tns, "g"), new DocumentLocation("file:///a.xsd", 9, 3))],
            null
        );
        registry.Add(self);

        self.Resolve(registry, Reporter);

        await AssertSingleError(XsdRuleCodes.SrcAttributeGroup, 9);
    }

    [Test]
    public async Task Group_IndirectCycle_IsReportedOnceAndBothGroupsResolve()
    {
        var registry = new FakeComponentRegistry([]);
        var a = AttributeGroupDefinition.Create(
            new XPathQName(Tns, "a"),
            Where,
            [Use(AttributeUseKind.Optional, Global("x"))],
            [new AttributeGroupReference(new XPathQName(Tns, "b"), new DocumentLocation("file:///a.xsd", 7, 1))],
            null
        );
        var b = AttributeGroupDefinition.Create(
            new XPathQName(Tns, "b"),
            Where,
            [],
            [new AttributeGroupReference(new XPathQName(Tns, "a"), new DocumentLocation("file:///a.xsd", 8, 1))],
            null
        );
        registry.Add(a);
        registry.Add(b);

        a.Resolve(registry, Reporter);
        b.Resolve(registry, Reporter);

        await AssertSingleError(XsdRuleCodes.SrcAttributeGroup, 8);
        await Assert.That(a.Uses).Count().IsEqualTo(1);
        await Assert.That(b.Uses).Count().IsEqualTo(0);
    }

    [Test]
    public async Task Group_UnknownReference_IsAnUnresolvedReferenceIssue()
    {
        var group = AttributeGroupDefinition.Create(
            new XPathQName(Tns, "g"),
            Where,
            [],
            [new AttributeGroupReference(new XPathQName(Tns, "missing"), new DocumentLocation("file:///a.xsd", 6, 1))],
            null
        );

        group.Resolve(new FakeComponentRegistry([]), Reporter);

        await AssertSingleError(XsdRuleCodes.SrcResolve, 6);
    }

    [Test]
    public async Task Group_DuplicateUse_IsASchemaError()
    {
        var group = AttributeGroupDefinition.Create(
            new XPathQName(Tns, "g"),
            Where,
            [Use(AttributeUseKind.Optional, Global("a")), Use(AttributeUseKind.Required, Global("a"))],
            [],
            null
        );

        group.Resolve(new FakeComponentRegistry([]), Reporter);

        await AssertSingleError(XsdRuleCodes.AgPropsCorrect, 4);
        await Assert.That(group.Uses).Count().IsEqualTo(1);
    }

    [Test]
    public async Task Group_SameDeclarationThroughTwoReferences_IsNotADuplicate()
    {
        var registry = new FakeComponentRegistry([]);
        var shared = Global("a");
        foreach (var name in new[] { "g1", "g2" })
        {
            registry.Add(
                AttributeGroupDefinition.Create(
                    new XPathQName(Tns, name),
                    Where,
                    [Use(AttributeUseKind.Optional, shared)],
                    [],
                    null
                )
            );
        }

        var group = AttributeGroupDefinition.Create(
            new XPathQName(Tns, "g"),
            Where,
            [],
            [
                new AttributeGroupReference(new XPathQName(Tns, "g1"), Where),
                new AttributeGroupReference(new XPathQName(Tns, "g2"), Where),
            ],
            null
        );

        group.Resolve(registry, Reporter);

        await Assert.That(_collector.Count).IsEqualTo(0);
        await Assert.That(group.Uses).Count().IsEqualTo(1);
    }

    [Test]
    public async Task Group_Wildcards_AreIntersectedInDocumentOrderThroughTheSeam()
    {
        var log = new List<string>();
        var registry = new FakeComponentRegistry([]);
        registry.Add(Wildcard("g1", new FakeWildcard(log, XsdProcessContents.Strict, "urn:a", "urn:b", "urn:c")));
        registry.Add(Wildcard("g2", new FakeWildcard(log, XsdProcessContents.Lax, "urn:a", "urn:b")));
        var own = new FakeWildcard(log, XsdProcessContents.Skip, "urn:b", "urn:c");
        var group = AttributeGroupDefinition.Create(
            new XPathQName(Tns, "g"),
            Where,
            [],
            [
                new AttributeGroupReference(new XPathQName(Tns, "g1"), Where),
                new AttributeGroupReference(new XPathQName(Tns, "g2"), Where),
            ],
            own
        );

        group.Resolve(registry, Reporter);

        // The fake keeps the process contents of the left operand: g1 is the left operand of both intersections.
        await Assert.That(log).IsEquivalentTo(["Intersect", "Intersect"]);
        await Assert.That(group.AttributeWildcard).IsNotNull();
        await Assert.That(group.AttributeWildcard!.ProcessContents).IsEqualTo(XsdProcessContents.Strict);
        await Assert.That(group.AttributeWildcard.Allows(new XPathQName("urn:b", "x"))).IsTrue();
        await Assert.That(group.AttributeWildcard.Allows(new XPathQName("urn:a", "x"))).IsFalse();
        await Assert.That(_collector.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Group_SingleWildcard_IsUsedWithoutIntersection()
    {
        var log = new List<string>();
        var own = new FakeWildcard(log, XsdProcessContents.Lax, "urn:a");
        var group = AttributeGroupDefinition.Create(new XPathQName(Tns, "g"), Where, [], [], own);

        group.Resolve(new FakeComponentRegistry([]), Reporter);

        await Assert.That(group.AttributeWildcard).IsSameReferenceAs(own);
        await Assert.That(log).Count().IsEqualTo(0);
    }

    [Test]
    public async Task Group_InexpressibleIntersection_IsASchemaError()
    {
        var log = new List<string>();
        var registry = new FakeComponentRegistry([]);
        registry.Add(Wildcard("g1", new FakeWildcard(log, XsdProcessContents.Strict, "urn:a")));
        var group = AttributeGroupDefinition.Create(
            new XPathQName(Tns, "g"),
            new DocumentLocation("file:///a.xsd", 3, 1),
            [],
            [new AttributeGroupReference(new XPathQName(Tns, "g1"), Where)],
            new FakeWildcard(log, XsdProcessContents.Lax, "urn:z")
        );

        group.Resolve(registry, Reporter);

        await AssertSingleError(XsdRuleCodes.CosAwIntersect, 3);
        await Assert.That(group.AttributeWildcard).IsNull();
    }

    [Test]
    public async Task Resolve_RunsOnce()
    {
        var log = new List<string>();
        var registry = new FakeComponentRegistry([]);
        registry.Add(Wildcard("g1", new FakeWildcard(log, XsdProcessContents.Strict, "urn:a")));
        var group = AttributeGroupDefinition.Create(
            new XPathQName(Tns, "g"),
            Where,
            [],
            [new AttributeGroupReference(new XPathQName(Tns, "g1"), Where)],
            new FakeWildcard(log, XsdProcessContents.Lax, "urn:a")
        );

        group.Resolve(registry, Reporter);
        group.Resolve(registry, Reporter);

        await Assert.That(log).Count().IsEqualTo(1);
    }

    private static AttributeGroupDefinition Wildcard(string name, IWildcard wildcard) =>
        AttributeGroupDefinition.Create(new XPathQName(Tns, name), Where, [], [], wildcard);

    private async Task AssertSingleError(DiagnosticCode code, int line)
    {
        var issues = _collector.ToResult().Issues;
        await Assert.That(issues).Count().IsEqualTo(1);
        await Assert.That(issues[0].Code).IsEqualTo(code);
        await Assert.That(issues[0].Location.Line).IsEqualTo(line);
    }
}
