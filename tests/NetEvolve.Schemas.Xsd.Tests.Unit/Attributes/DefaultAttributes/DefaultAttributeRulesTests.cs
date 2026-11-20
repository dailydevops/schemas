namespace NetEvolve.Schemas.Xsd.Tests.Unit.Attributes.DefaultAttributes;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.Attributes.Declarations;
using NetEvolve.Schemas.Xsd.Attributes.DefaultAttributes;
using NetEvolve.Schemas.Xsd.ComplexTypes.Definitions;
using NetEvolve.Schemas.Xsd.Components;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.SchemaDocuments;
using NetEvolve.Schemas.Xsd.SimpleTypes.Definitions;
using NetEvolve.Schemas.Xsd.Tests.Unit.Fakes;
using NetEvolve.Schemas.Xsd.Tests.Unit.SchemaDocuments;
using NetEvolve.Schemas.Xsd.Tests.Unit.Wildcards.Negation;
using NetEvolve.Schemas.Xsd.Wildcards.Core;
using NetEvolve.Schemas.Xsd.Wildcards.Negation;

public class DefaultAttributeRulesTests
{
    private const string Tns = "urn:test";
    private static readonly DocumentLocation Where = new("file:///a.xsd", 4, 2);
    private static readonly DocumentLocation TypeWhere = new("file:///a.xsd", 9, 3);

    private static readonly SimpleTypeDefinition StringType = SimpleTypeDefinition.FromDatatype(
        new XPathQName("http://www.w3.org/2001/XMLSchema", "string"),
        Where,
        new SimpleTypes.Definitions.FakeDatatype("string", DatatypeVariety.Atomic)
    );

    private static readonly AttributeUse[] NoUses = [];

    private readonly ValidationIssueCollector _collector = new(ValidationLimits.Default);

    private XsdIssueReporter Reporter => new(_collector);

    private AttributeDeclaration Declare(string name) =>
        AttributeDeclaration.Global(name, Tns, Where, StringType, default, Reporter)!;

    private AttributeUse Use(string name, AttributeDeclaration? declaration = null) =>
        AttributeUse.Create(Where, AttributeUseKind.Optional, declaration ?? Declare(name), default, Reporter)!;

    private AttributeGroupDefinition Group(
        string name,
        AttributeUse[] uses,
        IWildcard? wildcard = null,
        params AttributeGroupReference[] references
    ) => AttributeGroupDefinition.Create(new XPathQName(Tns, name), Where, uses, references, wildcard);

    private static ComplexTypeHeader Header() =>
        new(new XPathQName(Tns, "t"), TypeWhere, false, XsdDerivationMethod.None, XsdDerivationMethod.None);

    private static SchemaSyntaxNode Schema(string attributes)
    {
        var (document, _) = SchemaFixture.Parse(
            "<xs:schema xmlns:xs=\"http://www.w3.org/2001/XMLSchema\" xmlns:t=\"urn:test\" targetNamespace=\"urn:test\" "
                + attributes
                + "/>"
        );
        return document.Root!;
    }

    private static SchemaNamespaceScope Scope => new(Tns, false);

    private static string[] Names(IReadOnlyList<AttributeUse> uses)
    {
        var names = new string[uses.Count];
        for (var i = 0; i < names.Length; i++)
        {
            names[i] = uses[i].Name.LocalName;
        }

        return names;
    }

    private static Wildcard Core(string ns, string target = "urn:t")
    {
        _ = Wildcard.TryCreate(ns, "lax", target, Where, out var wildcard);
        return wildcard!;
    }

    private async Task AssertSingleError(DiagnosticCode code, int line)
    {
        var issues = _collector.ToResult().Issues;
        await Assert.That(issues).Count().IsEqualTo(1);
        await Assert.That(issues[0].Code).IsEqualTo(code);
        await Assert.That(issues[0].Location.Line).IsEqualTo(line);
    }

    [Test]
    [Arguments(null, true)]
    [Arguments("true", true)]
    [Arguments("1", true)]
    [Arguments("false", false)]
    [Arguments("0", false)]
    public async Task Applies_DefaultsToTrue(string? value, bool expected) =>
        await Assert.That(DefaultAttributeRules.Applies(value)).IsEqualTo(expected);

    [Test]
    public async Task Find_WithoutTheAttribute_ReturnsNothingAndReportsNothing()
    {
        var group = DefaultAttributeRules.Find(Schema(string.Empty), Scope, new FakeComponentRegistry([]), Reporter);

        await Assert.That(group).IsNull();
        await Assert.That(_collector.ToResult().Issues).IsEmpty();
    }

    [Test]
    public async Task Find_NamedGroup_IsReturnedResolved()
    {
        var registry = new FakeComponentRegistry([]);
        var inner = Group("inner", [Use("b")]);
        var outer = Group("outer", [Use("a")], null, new AttributeGroupReference(new XPathQName(Tns, "inner"), Where));
        registry.Add(inner);
        registry.Add(outer);

        var group = DefaultAttributeRules.Find(Schema("defaultAttributes=\"t:outer\""), Scope, registry, Reporter);

        await Assert.That(group).IsSameReferenceAs(outer);
        await Assert.That(Names(group!.Uses)).IsEquivalentTo(["a", "b"]);
        await Assert.That(_collector.ToResult().Issues).IsEmpty();
    }

    [Test]
    public async Task Find_UndeclaredGroup_IsASchemaError()
    {
        var group = DefaultAttributeRules.Find(
            Schema("defaultAttributes=\"t:missing\""),
            Scope,
            new FakeComponentRegistry([]),
            Reporter
        );

        await Assert.That(group).IsNull();
        await AssertSingleError(XsdRuleCodes.SrcResolve, 1);
    }

    [Test]
    public async Task Find_UnresolvablePrefix_IsASchemaError()
    {
        var group = DefaultAttributeRules.Find(
            Schema("defaultAttributes=\"nope:g\""),
            Scope,
            new FakeComponentRegistry([]),
            Reporter
        );

        await Assert.That(group).IsNull();
        await AssertSingleError(XsdRuleCodes.SrcResolve, 1);
    }

    [Test]
    public async Task TryApply_WithoutAGroup_KeepsTheTypeAsItIs()
    {
        var own = new[] { Use("a") };
        var wildcard = Core("##any");

        var ok = DefaultAttributeRules.TryApply(
            null,
            true,
            own,
            wildcard,
            TypeWhere,
            Reporter,
            out var uses,
            out var result
        );

        await Assert.That(ok).IsTrue();
        await Assert.That(uses).IsSameReferenceAs(own);
        await Assert.That(result).IsSameReferenceAs(wildcard);
    }

    [Test]
    public async Task TryApply_DefaultAttributesApplyFalse_RemovesTheDefaultGroup()
    {
        var own = new[] { Use("a") };
        var group = Group("g", [Use("d")], Core("##any"));
        group.Resolve(new FakeComponentRegistry([]), Reporter);

        var ok = DefaultAttributeRules.TryApply(
            group,
            false,
            own,
            null,
            TypeWhere,
            Reporter,
            out var uses,
            out var result
        );

        await Assert.That(ok).IsTrue();
        await Assert.That(uses).IsSameReferenceAs(own);
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task TryApply_AddsTheUsesOfTheGroupAfterTheOwnUses()
    {
        var group = Group("g", [Use("d1"), Use("d2")]);
        group.Resolve(new FakeComponentRegistry([]), Reporter);

        var ok = DefaultAttributeRules.TryApply(
            group,
            true,
            [Use("a"), Use("b")],
            null,
            TypeWhere,
            Reporter,
            out var uses,
            out var result
        );

        await Assert.That(ok).IsTrue();
        await Assert.That(Names(uses)).IsEquivalentTo(["a", "b", "d1", "d2"]);
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task TryApply_WildcardOfTheGroupOnly_BecomesTheWildcardOfTheType()
    {
        var wildcard = Core("##other");
        var group = Group("g", NoUses, wildcard);
        group.Resolve(new FakeComponentRegistry([]), Reporter);

        var ok = DefaultAttributeRules.TryApply(group, true, NoUses, null, TypeWhere, Reporter, out _, out var result);

        await Assert.That(ok).IsTrue();
        await Assert.That(result).IsSameReferenceAs(wildcard);
    }

    [Test]
    public async Task TryApply_WildcardOfTheTypeOnly_IsKept()
    {
        var wildcard = Core("##other");
        var group = Group("g", [Use("d")]);
        group.Resolve(new FakeComponentRegistry([]), Reporter);

        var ok = DefaultAttributeRules.TryApply(
            group,
            true,
            NoUses,
            wildcard,
            TypeWhere,
            Reporter,
            out _,
            out var result
        );

        await Assert.That(ok).IsTrue();
        await Assert.That(result).IsSameReferenceAs(wildcard);
    }

    [Test]
    public async Task TryApply_BothWildcards_AreIntersected()
    {
        var group = Group("g", NoUses, Core("urn:a urn:b"));
        group.Resolve(new FakeComponentRegistry([]), Reporter);

        var ok = DefaultAttributeRules.TryApply(
            group,
            true,
            NoUses,
            Core("urn:b urn:c"),
            TypeWhere,
            Reporter,
            out _,
            out var result
        );

        await Assert.That(ok).IsTrue();
        await Assert.That(result!.Allows(new XPathQName("urn:b", "x"))).IsTrue();
        await Assert.That(result.Allows(new XPathQName("urn:a", "x"))).IsFalse();
        await Assert.That(result.Allows(new XPathQName("urn:c", "x"))).IsFalse();
    }

    [Test]
    public async Task TryApply_NegatedWildcards_AreIntersected()
    {
        var excluded = NegatedWildcard.Create(NameConstraintTests.Spec("not a"), XsdProcessContents.Lax, Where);
        var group = Group(
            "g",
            NoUses,
            NegatedWildcard.Create(NameConstraintTests.Spec("not b;q c#x"), XsdProcessContents.Lax, Where)
        );
        group.Resolve(new FakeComponentRegistry([]), Reporter);

        var ok = DefaultAttributeRules.TryApply(
            group,
            true,
            NoUses,
            excluded,
            TypeWhere,
            Reporter,
            out _,
            out var result
        );

        await Assert.That(ok).IsTrue();
        await Assert.That(NegatedWildcard.ConstraintOf(result!)).IsEqualTo(NameConstraintTests.Spec("not a b;q c#x"));
    }

    [Test]
    public async Task TryApply_WildcardsWithoutExpressibleIntersection_ReportAndKeepTheOwn()
    {
        var own = Core("##other", "urn:t");
        var group = Group("g", NoUses, Core("##other", "urn:o"));
        group.Resolve(new FakeComponentRegistry([]), Reporter);

        var ok = DefaultAttributeRules.TryApply(group, true, NoUses, own, TypeWhere, Reporter, out _, out var result);

        await Assert.That(ok).IsFalse();
        await Assert.That(result).IsSameReferenceAs(own);
        await AssertSingleError(XsdRuleCodes.CosAwIntersect, 9);
    }

    [Test]
    public async Task ComplexType_WithoutAnyContent_GetsTheDefaultAttributeGroup()
    {
        var group = Group("g", [Use("d")]);
        group.Resolve(new FakeComponentRegistry([]), Reporter);
        _ = DefaultAttributeRules.TryApply(
            group,
            true,
            [Use("a")],
            null,
            TypeWhere,
            Reporter,
            out var uses,
            out var wildcard
        );

        var type = ComplexTypeDefinition.Implicit(Header(), false, null, uses, wildcard, Reporter);

        await Assert.That(Names(type.AttributeUses)).IsEquivalentTo(["a", "d"]);
    }

    [Test]
    public async Task ComplexType_WithDefaultAttributesApplyFalse_HasOnlyItsOwnAttributes()
    {
        var group = Group("g", [Use("d")]);
        group.Resolve(new FakeComponentRegistry([]), Reporter);
        _ = DefaultAttributeRules.TryApply(
            group,
            DefaultAttributeRules.Applies("false"),
            [Use("a")],
            null,
            TypeWhere,
            Reporter,
            out var uses,
            out var wildcard
        );

        var type = ComplexTypeDefinition.Implicit(Header(), false, null, uses, wildcard, Reporter);

        await Assert.That(Names(type.AttributeUses)).IsEquivalentTo(["a"]);
    }

    [Test]
    public async Task ComplexType_Derivations_AllGetTheDefaultAttributeGroup()
    {
        var group = Group("g", [Use("d")], Core("##other"));
        group.Resolve(new FakeComponentRegistry([]), Reporter);
        _ = DefaultAttributeRules.TryApply(
            group,
            true,
            [Use("a")],
            null,
            TypeWhere,
            Reporter,
            out var uses,
            out var wildcard
        );

        var restriction = ComplexTypeDefinition.FromComplexContent(
            Header(),
            XsdDerivationMethod.Restriction,
            ComplexTypeDefinition.AnyType,
            false,
            null,
            uses,
            wildcard,
            Reporter
        );
        var extension = ComplexTypeDefinition.FromComplexContent(
            Header(),
            XsdDerivationMethod.Extension,
            restriction!,
            false,
            null,
            uses,
            wildcard,
            Reporter
        );
        var simple = ComplexTypeDefinition.FromSimpleContent(
            Header(),
            XsdDerivationMethod.Extension,
            StringType,
            null,
            uses,
            wildcard,
            Reporter
        );

        await Assert.That(Names(restriction!.AttributeUses)).IsEquivalentTo(["a", "d"]);
        await Assert.That(restriction.AttributeWildcard).IsNotNull();
        await Assert.That(Names(extension!.AttributeUses)).IsEquivalentTo(["a", "d"]);
        await Assert.That(Names(simple!.AttributeUses)).IsEquivalentTo(["a", "d"]);
        await Assert.That(_collector.ToResult().Issues).IsEmpty();
    }

    [Test]
    public async Task ComplexType_OwnAttributeWithTheNameOfADefaultOne_IsASchemaError_UnlessItIsTheSameDeclaration()
    {
        var shared = Declare("a");
        var group = Group("g", [Use("a", shared), Use("d")]);
        group.Resolve(new FakeComponentRegistry([]), Reporter);

        _ = DefaultAttributeRules.TryApply(
            group,
            true,
            [Use("a", shared)],
            null,
            TypeWhere,
            Reporter,
            out var same,
            out _
        );
        var sameType = ComplexTypeDefinition.Implicit(Header(), false, null, same, null, Reporter);
        var before = _collector.ToResult().Issues.Count;
        _ = DefaultAttributeRules.TryApply(group, true, [Use("a")], null, TypeWhere, Reporter, out var other, out _);
        _ = ComplexTypeDefinition.Implicit(Header(), false, null, other, null, Reporter);

        await Assert.That(Names(sameType.AttributeUses)).IsEquivalentTo(["a", "d"]);
        await Assert.That(before).IsEqualTo(0);
        var issues = _collector.ToResult().Issues;
        await Assert.That(issues).Count().IsEqualTo(1);
        await Assert.That(issues[0].Code).IsEqualTo(XsdRuleCodes.CtPropsCorrect);
    }

    [Test]
    public async Task Operations_WithNullArgument_Throw()
    {
        var schema = Schema(string.Empty);
        var registry = new FakeComponentRegistry([]);

        await Assert
            .That(() => DefaultAttributeRules.Find(null!, Scope, registry, Reporter))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => DefaultAttributeRules.Find(schema, Scope, null!, Reporter))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => DefaultAttributeRules.Find(schema, Scope, registry, null!))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => DefaultAttributeRules.TryApply(null, true, null!, null, TypeWhere, Reporter, out _, out _))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => DefaultAttributeRules.TryApply(null, true, NoUses, null, TypeWhere, null!, out _, out _))
            .Throws<ArgumentNullException>();
    }
}
